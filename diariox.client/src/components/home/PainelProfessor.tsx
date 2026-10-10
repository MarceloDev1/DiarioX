import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { FiAlertTriangle, FiCheckCircle, FiChevronLeft, FiChevronRight, FiClock } from 'react-icons/fi';
import { apiFetch, readApiError } from '../../utils/api';
import { usePermissoes } from '../../hooks/usePermissoes';
import FeedbackMessage from '../ui/FeedbackMessage';
import Cartao from './Cartao';
import { formatarData, formatarPercentual } from './tipos';
import {
    formatarTempos, linkChamada, linkConteudo, linkNotas, nomeDoMes,
    type AulasDoDia, type DiaPendencia, type DiarioPainel, type GraficoRegistros,
    type PainelProfessor as Dados, type PendenciasMes, type PeriodoNotas,
} from './tiposProfessor';
import './PainelProfessor.css';

interface PainelProfessorProps {
    painel: Dados;
    atualizando: boolean;
    onEscolaChange: (escolaId: number | null) => void;
}

/** Home do professor: aulas do dia, pendências do mês, diários, gráficos do ano e pendências de notas. */
function PainelProfessor({ painel, atualizando, onEscolaChange }: PainelProfessorProps) {
    const { can } = usePermissoes();
    const acessos = {
        chamada: can('chamada.visualizar'),
        conteudo: can('conteudo-ministrado.visualizar'),
        notas: can('notas.visualizar'),
    };
    const semLotacao = painel.semLotacao;

    return (
        <div className={`home painel-professor${atualizando ? ' painel-professor-atualizando' : ''}`}>
            <div className="painel-professor-topo">
                <div className="form-group painel-professor-escola">
                    <label htmlFor="painel-escola">Escola</label>
                    <select
                        id="painel-escola"
                        value={painel.escolaId ?? ''}
                        onChange={e => onEscolaChange(e.target.value ? Number(e.target.value) : null)}
                        disabled={painel.escolas.length <= 1}
                    >
                        {painel.escolas.length !== 1 && <option value="">Todas as minhas escolas</option>}
                        {painel.escolas.map(e => <option key={e.id} value={e.id}>{e.nome}</option>)}
                    </select>
                </div>
                <span className="painel-professor-ano">Ano letivo {painel.anoReferencia}</span>
            </div>

            <div className="painel-professor-linha">
                <Cartao titulo="Minhas aulas do dia" className="painel-cartao-aulas">
                    <AulasDoDiaLista hoje={painel.hoje} acessos={acessos} />
                </Cartao>

                <Cartao titulo="Minhas pendências" className="painel-cartao-pendencias">
                    {semLotacao || !painel.pendencias
                        ? <p className="home-vazio">{semLotacao}</p>
                        : (
                            <CalendarioPendencias
                                key={painel.escolaId ?? 'todas'}
                                inicial={painel.pendencias}
                                escolaId={painel.escolaId}
                                inicioAno={painel.anoLetivoInicio}
                                acessos={acessos}
                            />
                        )}
                </Cartao>

                <Cartao titulo="Meus diários" className="painel-cartao-diarios">
                    {semLotacao
                        ? <p className="home-vazio">{semLotacao}</p>
                        : <ListaDiarios diarios={painel.diarios} acessos={acessos} />}
                </Cartao>
            </div>

            <div className="painel-professor-graficos">
                <Cartao titulo="Registros de aula no ano">
                    {semLotacao || !painel.registrosAula
                        ? <p className="home-vazio">{semLotacao}</p>
                        : (
                            <GraficoPizza
                                dados={painel.registrosAula}
                                rotulos={['Aulas registradas', 'Pendentes', 'A realizar']}
                                legenda="Aulas registradas ÷ aulas previstas no calendário do ano letivo."
                            />
                        )}
                </Cartao>
                <Cartao titulo="Registros de frequência no ano">
                    {semLotacao || !painel.registrosFrequencia
                        ? <p className="home-vazio">{semLotacao}</p>
                        : (
                            <GraficoPizza
                                dados={painel.registrosFrequencia}
                                rotulos={['Chamadas realizadas', 'Pendentes', '']}
                                legenda="Chamadas realizadas ÷ dias letivos decorridos com aula."
                            />
                        )}
                </Cartao>
            </div>

            {(semLotacao || painel.notas) && (
                <Cartao titulo="Turmas com pendência de avaliação e notas">
                    {semLotacao || !painel.notas
                        ? <p className="home-vazio">{semLotacao}</p>
                        : <PendenciasNotas periodos={painel.notas} podeAbrir={acessos.notas} />}
                </Cartao>
            )}
        </div>
    );
}

interface Acessos {
    chamada: boolean;
    conteudo: boolean;
    notas: boolean;
}

function AulasDoDiaLista({ hoje, acessos }: { hoje: AulasDoDia; acessos: Acessos }) {
    return (
        <>
            <p className="painel-data">
                {formatarData(hoje.data)}
                {hoje.evento && <span className="home-selo home-selo-neutro">{hoje.evento}</span>}
            </p>
            {hoje.aulas.length === 0 ? (
                <p className="home-vazio">{hoje.mensagem}</p>
            ) : (
                <ul className="painel-lista">
                    {hoje.aulas.map(aula => (
                        <li key={`${aula.turmaId}-${aula.disciplinaId ?? 'diaria'}`} className="painel-item">
                            <div className="painel-item-texto">
                                <strong>{aula.turmaNome}</strong>
                                <span>
                                    {aula.disciplinaNome}
                                    {aula.tempos.length > 0 && ` · ${formatarTempos(aula.tempos)}`}
                                </span>
                                <span className="painel-item-status">
                                    <Status ok={aula.frequenciaRegistrada} rotulo="Frequência" />
                                    <Status ok={aula.aulaRegistrada} rotulo="Aula" />
                                </span>
                            </div>
                            <div className="painel-item-acoes">
                                {acessos.chamada && (
                                    <Link className="painel-botao" to={linkChamada(aula.turmaId, aula.disciplinaId, hoje.data)}>Frequência</Link>
                                )}
                                {acessos.conteudo && (
                                    <Link className="painel-botao" to={linkConteudo(aula.turmaId, aula.disciplinaId, hoje.data)}>Aula</Link>
                                )}
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </>
    );
}

function Status({ ok, rotulo }: { ok: boolean; rotulo: string }) {
    return (
        <span className={`home-selo ${ok ? 'home-selo-positivo' : 'home-selo-neutro'}`}>
            {ok ? <FiCheckCircle aria-hidden="true" /> : <FiClock aria-hidden="true" />} {rotulo}{ok ? '' : ' pendente'}
        </span>
    );
}

const DIAS_SEMANA = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];

function mesDe(iso: string): { ano: number; mes: number } {
    const [ano, mes] = iso.split('-').map(Number);
    return { ano, mes };
}

/** RN03: mini-calendário do mês com os dias letivos passados sem registro em vermelho. */
function CalendarioPendencias({ inicial, escolaId, inicioAno, acessos }: {
    inicial: PendenciasMes;
    escolaId: number | null;
    inicioAno: string | null;
    acessos: Acessos;
}) {
    const [mes, setMes] = useState(inicial);
    const [alvo, setAlvo] = useState({ ano: inicial.ano, mes: inicial.mes });
    const [selecionado, setSelecionado] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [carregando, setCarregando] = useState(false);

    useEffect(() => {
        if (alvo.ano === mes.ano && alvo.mes === mes.mes) return;
        let cancelled = false;

        async function load() {
            setCarregando(true);
            setError(null);
            try {
                const escola = escolaId ? `&escolaId=${escolaId}` : '';
                const response = await apiFetch(`/api/dashboard/professor/pendencias?ano=${alvo.ano}&mes=${alvo.mes}${escola}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const dados = (await response.json()) as PendenciasMes;
                if (!cancelled) {
                    setMes(dados);
                    setSelecionado(null);
                }
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as pendências.');
            } finally {
                if (!cancelled) setCarregando(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [alvo, mes, escolaId]);

    const navegar = (delta: number) => {
        const data = new Date(alvo.ano, alvo.mes - 1 + delta, 1);
        setAlvo({ ano: data.getFullYear(), mes: data.getMonth() + 1 });
    };

    // Navega dos meses do ano letivo até o mês atual.
    const primeiro = inicioAno ? mesDe(inicioAno) : { ano: inicial.ano, mes: 1 };
    const podeVoltar = alvo.ano * 12 + alvo.mes > primeiro.ano * 12 + primeiro.mes;
    const podeAvancar = alvo.ano * 12 + alvo.mes < inicial.ano * 12 + inicial.mes;

    const vazios = new Date(mes.ano, mes.mes - 1, 1).getDay();
    const pendentes = mes.dias.filter(d => d.situacao === 'pendente').length;
    const dia = mes.dias.find(d => d.data === selecionado) ?? null;

    return (
        <div className="painel-calendario">
            <div className="painel-calendario-nav">
                <button type="button" onClick={() => navegar(-1)} disabled={!podeVoltar || carregando} aria-label="Mês anterior">
                    <FiChevronLeft aria-hidden="true" />
                </button>
                <strong>{nomeDoMes(alvo.mes)} de {alvo.ano}</strong>
                <button type="button" onClick={() => navegar(1)} disabled={!podeAvancar || carregando} aria-label="Próximo mês">
                    <FiChevronRight aria-hidden="true" />
                </button>
            </div>

            <FeedbackMessage message={error} type="error" />

            <div className={`painel-calendario-grade${carregando ? ' painel-calendario-carregando' : ''}`}>
                {DIAS_SEMANA.map((d, i) => <span key={i} className="painel-calendario-semana" aria-hidden="true">{d}</span>)}
                {Array.from({ length: vazios }, (_, i) => <span key={`v${i}`} />)}
                {mes.dias.map(d => (
                    <DiaCalendario
                        key={d.data}
                        dia={d}
                        selecionado={d.data === selecionado}
                        onSelecionar={() => setSelecionado(d.data === selecionado ? null : d.data)}
                    />
                ))}
            </div>

            <p className="painel-calendario-resumo">
                {pendentes === 0
                    ? 'Nenhum dia com pendência neste mês.'
                    : `${pendentes} ${pendentes === 1 ? 'dia' : 'dias'} com pendência. Clique para regularizar.`}
            </p>

            {dia && dia.pendencias.length > 0 && (
                <ul className="painel-lista painel-lista-compacta">
                    {dia.pendencias.map(p => (
                        <li key={`${p.turmaId}-${p.disciplinaId ?? 'diaria'}`} className="painel-item">
                            <div className="painel-item-texto">
                                <strong>{p.turmaNome}</strong>
                                <span>{p.disciplinaNome} · {formatarData(dia.data)}</span>
                            </div>
                            <div className="painel-item-acoes">
                                {p.semFrequencia && acessos.chamada && (
                                    <Link className="painel-botao painel-botao-alerta" to={linkChamada(p.turmaId, p.disciplinaId, dia.data)}>
                                        Frequência
                                    </Link>
                                )}
                                {p.semAula && acessos.conteudo && (
                                    <Link className="painel-botao painel-botao-alerta" to={linkConteudo(p.turmaId, p.disciplinaId, dia.data)}>
                                        Aula
                                    </Link>
                                )}
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

const DESCRICAO_SITUACAO: Record<DiaPendencia['situacao'], string> = {
    'fora-do-ano': 'Fora do ano letivo',
    'nao-letivo': 'Dia não letivo',
    'sem-aula': 'Sem aulas na sua grade',
    'em-dia': 'Registros em dia',
    'pendente': 'Frequência ou aula sem registro',
    'hoje': 'Hoje',
    'futuro': 'Dia futuro',
};

function DiaCalendario({ dia, selecionado, onSelecionar }: { dia: DiaPendencia; selecionado: boolean; onSelecionar: () => void }) {
    const numero = Number(dia.data.slice(8, 10));
    const titulo = [formatarData(dia.data), DESCRICAO_SITUACAO[dia.situacao], dia.evento].filter(Boolean).join(' · ');
    const classe = `painel-dia painel-dia-${dia.situacao}${selecionado ? ' painel-dia-selecionado' : ''}`;

    if (dia.situacao !== 'pendente') {
        return <span className={classe} title={titulo}>{numero}</span>;
    }

    return (
        <button type="button" className={classe} title={titulo} aria-pressed={selecionado} onClick={onSelecionar}>
            {numero}
        </button>
    );
}

function ListaDiarios({ diarios, acessos }: { diarios: DiarioPainel[]; acessos: Acessos }) {
    return (
        <ul className="painel-lista painel-diarios">
            {diarios.map(diario => (
                <li key={diario.turmaId} className="painel-diario">
                    <div className="painel-diario-cabecalho">
                        <strong>{diario.turmaNome}</strong>
                        <span>{diario.escolaNome} · {diario.etapaNome}</span>
                        {diario.semGrade && (
                            <span className="home-selo home-selo-negativo" title="Sem grade de horários não há aulas previstas para o painel.">
                                <FiAlertTriangle aria-hidden="true" /> Sem grade de horários
                            </span>
                        )}
                    </div>
                    {diario.diaria && acessos.chamada && (
                        <div className="painel-diario-linha">
                            <span>Frequência diária da turma</span>
                            <div className="painel-item-acoes">
                                <Link className="painel-botao" to={linkChamada(diario.turmaId, null)}>Frequência</Link>
                            </div>
                        </div>
                    )}
                    {diario.disciplinas.map(d => (
                        <div key={d.id} className="painel-diario-linha">
                            <span>
                                {d.nome}
                                {!diario.diaria && <small> · {d.aulasSemanais} {d.aulasSemanais === 1 ? 'aula' : 'aulas'}/semana</small>}
                            </span>
                            <div className="painel-item-acoes">
                                {!diario.diaria && acessos.chamada && (
                                    <Link className="painel-botao" to={linkChamada(diario.turmaId, d.id)}>Frequência</Link>
                                )}
                                {acessos.conteudo && <Link className="painel-botao" to={linkConteudo(diario.turmaId, d.id)}>Aula</Link>}
                                {acessos.notas && <Link className="painel-botao" to={linkNotas(diario.turmaId, d.id)}>Avaliação</Link>}
                            </div>
                        </div>
                    ))}
                </li>
            ))}
        </ul>
    );
}

const CORES_PIZZA = ['#16a34a', '#dc2626', '#cbd5e1'];

/** RN02: rosca com registrados, pendentes e (no gráfico de aulas) a realizar. */
function GraficoPizza({ dados, rotulos, legenda }: { dados: GraficoRegistros; rotulos: [string, string, string]; legenda: string }) {
    const valores = [dados.registrados, dados.pendentes, dados.aFazer];
    const total = valores.reduce((a, b) => a + b, 0);
    const raio = 15.915; // circunferência 100: o traço de cada fatia é o próprio percentual.
    let acumulado = 0;

    return (
        <div className="painel-grafico">
            <svg viewBox="0 0 42 42" className="painel-grafico-rosca" role="img"
                aria-label={`${rotulos[0]}: ${dados.percentual === null ? 'sem aulas previstas' : formatarPercentual(dados.percentual)}`}>
                <circle cx="21" cy="21" r={raio} fill="none" stroke="#eef1f6" strokeWidth="6" />
                {total > 0 && valores.map((valor, i) => {
                    if (valor === 0) return null;
                    const fatia = (valor * 100) / total;
                    const deslocamento = 25 - acumulado;
                    acumulado += fatia;
                    return (
                        <circle key={i} cx="21" cy="21" r={raio} fill="none" stroke={CORES_PIZZA[i]} strokeWidth="6"
                            strokeDasharray={`${fatia} ${100 - fatia}`} strokeDashoffset={deslocamento} />
                    );
                })}
                <text x="21" y="22.5" textAnchor="middle" className="painel-grafico-valor">
                    {dados.percentual === null ? '—' : formatarPercentual(dados.percentual)}
                </text>
            </svg>
            <div className="painel-grafico-legenda">
                <ul>
                    {valores.map((valor, i) => rotulos[i] && (
                        <li key={i}>
                            <span className="painel-grafico-cor" style={{ background: CORES_PIZZA[i] }} aria-hidden="true" />
                            {rotulos[i]}: <strong>{valor}</strong>
                        </li>
                    ))}
                </ul>
                <small>{legenda}</small>
            </div>
        </div>
    );
}

/** RN01: só o período atual e os anteriores, conforme o ano letivo avança. */
function PendenciasNotas({ periodos, podeAbrir }: { periodos: PeriodoNotas[]; podeAbrir: boolean }) {
    if (periodos.length === 0) {
        return <p className="home-vazio">Nenhum período avaliativo iniciado no ano letivo.</p>;
    }

    return (
        <div className="painel-periodos">
            {periodos.map(periodo => (
                <section key={periodo.id} className={`painel-periodo${periodo.pendencias.length > 0 ? ' painel-periodo-pendente' : ''}`}>
                    <header>
                        <strong>{periodo.nome}</strong>
                        {periodo.atual && <span className="home-selo home-selo-neutro">Atual</span>}
                        <span className="painel-periodo-datas">
                            {formatarData(periodo.dataInicio)} a {formatarData(periodo.dataTermino)}
                            {periodo.prazoLancamentoNotas && ` · prazo ${formatarData(periodo.prazoLancamentoNotas)}`}
                        </span>
                        {periodo.prazoEncerrado && <span className="home-selo home-selo-negativo">Prazo encerrado</span>}
                    </header>
                    {periodo.pendencias.length === 0 ? (
                        <p className="home-vazio"><FiCheckCircle aria-hidden="true" /> Nenhuma pendência no período.</p>
                    ) : (
                        <ul className="painel-lista painel-lista-compacta">
                            {periodo.pendencias.map(p => (
                                <li key={`${p.turmaId}-${p.disciplinaId}`} className="painel-item">
                                    <div className="painel-item-texto">
                                        <strong>{p.turmaNome} · {p.disciplinaNome}</strong>
                                        <span>{p.descricao}</span>
                                    </div>
                                    {podeAbrir && (
                                        <div className="painel-item-acoes">
                                            <Link className="painel-botao" to={linkNotas(p.turmaId, p.disciplinaId, periodo.id)}>Notas</Link>
                                        </div>
                                    )}
                                </li>
                            ))}
                        </ul>
                    )}
                </section>
            ))}
        </div>
    );
}

export default PainelProfessor;
