import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router';
import type { IconType } from 'react-icons';
import {
    FiAlertCircle, FiAlertTriangle, FiBarChart2, FiBookmark, FiCalendar, FiCheck, FiCheckSquare, FiClock,
    FiFlag, FiInfo, FiUserCheck, FiUsers,
} from 'react-icons/fi';
import { MdClass, MdPeople } from 'react-icons/md';
import { apiFetch, readApiError } from '../../utils/api';
import { usePermissoes } from '../../hooks/usePermissoes';
import { permissaoDaPagina } from '../../utils/permissoes';
import { caminhoDaPagina } from '../../utils/rotas';
import FeedbackMessage from '../ui/FeedbackMessage';
import Cartao from './Cartao';
import {
    formatarData, formatarInteiro, formatarPercentual, formatarVariacao, partesDaData,
    type Dashboard, type TipoAlerta, type TipoEvento,
} from './tipos';
import './Home.css';

type Cor = 'roxo' | 'azul' | 'verde' | 'ambar';

const atalhos: { pagina: string; titulo: string; descricao: string; icone: IconType; cor: Cor }[] = [
    { pagina: 'chamada', titulo: 'Chamada diária', descricao: 'Registre presenças por turma', icone: FiCheckSquare, cor: 'roxo' },
    { pagina: 'alunos', titulo: 'Alunos e enturmação', descricao: 'Gerencie matrículas e turmas', icone: MdPeople, cor: 'azul' },
    { pagina: 'alocacao-professor', titulo: 'Alocação de professores', descricao: 'Defina quem leciona cada disciplina', icone: FiUserCheck, cor: 'verde' },
    { pagina: 'relatorios', titulo: 'Relatórios e gráficos', descricao: 'Análises da rede e das escolas', icone: FiBarChart2, cor: 'ambar' },
];

const iconeDoAlerta: Record<TipoAlerta, IconType> = {
    atencao: FiAlertTriangle,
    aviso: FiAlertCircle,
    info: FiInfo,
};

const iconeDoEvento: Record<TipoEvento, IconType> = {
    'inicio-periodo': FiBookmark,
    'fim-periodo': FiClock,
    'inicio-ano': FiCalendar,
    'fim-ano': FiFlag,
};

interface IndicadorProps {
    icone: IconType;
    cor: Cor;
    valor: string;
    rotulo: string;
    selo?: { texto: string; tom: 'positivo' | 'negativo' | 'neutro'; titulo?: string };
}

function Indicador({ icone: Icone, cor, valor, rotulo, selo }: IndicadorProps) {
    return (
        <div className="home-kpi">
            <div className="home-kpi-topo">
                <span className={`home-icone home-icone-${cor}`}><Icone aria-hidden="true" /></span>
                {selo && <span className={`home-selo home-selo-${selo.tom}`} title={selo.titulo}>{selo.texto}</span>}
            </div>
            <strong className="home-kpi-valor">{valor}</strong>
            <span className="home-kpi-rotulo">{rotulo}</span>
        </div>
    );
}

const CHAVE_ESCOLA_GESTAO = 'diariox.home.gestao.escola';

function lerEscolaSalva(): number | null {
    try { return Number(localStorage.getItem(CHAVE_ESCOLA_GESTAO)) || null; } catch { return null; }
}

function salvarEscola(id: number | null) {
    try {
        if (id === null) localStorage.removeItem(CHAVE_ESCOLA_GESTAO);
        else localStorage.setItem(CHAVE_ESCOLA_GESTAO, String(id));
    } catch { /* sem localStorage */ }
}

/** Painel geral da home (gestão, secretaria): indicadores, atalhos, alertas, gráficos e alunos em risco. */
function PainelGeral() {
    const { can } = usePermissoes();
    const [painel, setPainel] = useState<Dashboard | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [escolaId, setEscolaId] = useState<number | null>(lerEscolaSalva);

    const carregarPainel = useCallback(async (escola: number | null) => {
        try {
            const qs = escola ? `?escolaId=${escola}` : '';
            const response = await apiFetch(`/api/dashboard${qs}`);
            if (!response.ok) throw new Error(await readApiError(response));
            setPainel(await response.json() as Dashboard);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao carregar o painel.');
        }
    }, []);

    useEffect(() => { void carregarPainel(escolaId); }, [escolaId, carregarPainel]);

    const handleEscolaChange = (id: number | null) => {
        salvarEscola(id);
        setEscolaId(id);
    };

    const podeAbrir = (pagina: string) => !permissaoDaPagina[pagina] || can(permissaoDaPagina[pagina]);
    const atalhosVisiveis = atalhos.filter(a => podeAbrir(a.pagina));

    if (!painel) {
        return error
            ? <FeedbackMessage message={error} type="error" />
            : <div className="loading">Carregando painel...</div>;
    }

    const { alunos, turmas, frequencia, professores } = painel;
    const temIndicadores = alunos || turmas || frequencia || professores;

    return (
        <div className="home">
            {/* RN01: Filtro de escola para usuários da rede */}
            {painel.escolas && (
                <div className="home-filtro-escola">
                    <label htmlFor="filtro-escola">Escola:</label>
                    <select
                        id="filtro-escola"
                        value={painel.escolaIdSelecionada ?? ''}
                        onChange={e => handleEscolaChange(e.target.value ? Number(e.target.value) : null)}
                    >
                        <option value="">Todas as escolas</option>
                        {painel.escolas.map(e => (
                            <option key={e.id} value={e.id}>{e.nome}</option>
                        ))}
                    </select>
                </div>
            )}

            {temIndicadores && (
                <div className="home-kpis">
                    {alunos && (
                        <Indicador
                            icone={FiUsers}
                            cor="roxo"
                            valor={formatarInteiro(alunos.ativos)}
                            rotulo="Alunos ativos"
                            selo={alunos.variacaoPercentual === null ? undefined : {
                                texto: formatarVariacao(alunos.variacaoPercentual),
                                tom: alunos.variacaoPercentual < 0 ? 'negativo' : 'positivo',
                                titulo: 'Em relação a 30 dias atrás',
                            }}
                        />
                    )}
                    {turmas && (
                        <Indicador
                            icone={MdClass}
                            cor="azul"
                            valor={formatarInteiro(turmas.ativas)}
                            rotulo="Turmas ativas"
                            selo={turmas.escolas > 1 ? { texto: `${turmas.escolas} escolas`, tom: 'neutro' } : undefined}
                        />
                    )}
                    {frequencia && (
                        <Indicador
                            icone={FiCheck}
                            cor="verde"
                            valor={frequencia.percentual === null ? '—' : formatarPercentual(frequencia.percentual)}
                            rotulo="Frequência média"
                            selo={frequencia.percentual === null ? undefined : frequencia.percentual >= frequencia.meta
                                ? { texto: 'Meta OK', tom: 'positivo', titulo: `Últimos ${frequencia.dias} dias · meta de ${frequencia.meta}%` }
                                : { texto: 'Abaixo da meta', tom: 'negativo', titulo: `Últimos ${frequencia.dias} dias · meta de ${frequencia.meta}%` }}
                        />
                    )}
                    {professores && (
                        <Indicador
                            icone={FiUserCheck}
                            cor="ambar"
                            valor={formatarInteiro(professores.alocados)}
                            rotulo="Professores alocados"
                            selo={professores.ativos > 0
                                ? { texto: `de ${formatarInteiro(professores.ativos)} ativos`, tom: 'neutro' }
                                : undefined}
                        />
                    )}
                </div>
            )}

            {/* RN03: Gráficos de frequência e diários */}
            {(painel.graficoFrequencia || painel.graficoDiarios) && (
                <div className="home-graficos">
                    {painel.graficoFrequencia && painel.graficoFrequencia.length > 0 && (
                        <Cartao titulo="Evolução da frequência" className="home-cartao-grafico">
                            <p className="home-grafico-subtitulo">Presença diária nos últimos 30 dias (%)</p>
                            <GraficoFrequencia dados={painel.graficoFrequencia} meta={frequencia?.meta ?? 75} />
                        </Cartao>
                    )}
                    {painel.graficoDiarios && (
                        <Cartao titulo="Lançamentos de diários" className="home-cartao-grafico">
                            <p className="home-grafico-subtitulo">
                                Chamadas com conteúdo ministrado registrado
                            </p>
                            <GraficoDiarios dados={painel.graficoDiarios} />
                        </Cartao>
                    )}
                </div>
            )}

            <div className="home-grade">
                <div className="home-coluna">
                    {atalhosVisiveis.length > 0 && (
                        <section>
                            <h2 className="home-titulo-secao">Acesso rápido</h2>
                            <div className="home-atalhos">
                                {atalhosVisiveis.map(({ pagina, titulo, descricao, icone: Icone, cor }) => (
                                    <Link key={pagina} to={caminhoDaPagina(pagina)} className="home-atalho">
                                        <span className={`home-icone home-icone-${cor}`}><Icone aria-hidden="true" /></span>
                                        <span>
                                            <strong>{titulo}</strong>
                                            <span>{descricao}</span>
                                        </span>
                                    </Link>
                                ))}
                            </div>
                        </section>
                    )}

                    {painel.ultimasChamadas && (
                        <Cartao
                            titulo="Últimas chamadas realizadas"
                            acao={<Link to={caminhoDaPagina('chamada')} className="home-link">Ver todas</Link>}
                        >
                            {painel.ultimasChamadas.length === 0 ? (
                                <p className="home-vazio">Nenhuma chamada registrada ainda.</p>
                            ) : (
                                <div className="table-container">
                                    <table className="home-tabela">
                                        <thead>
                                            <tr>
                                                <th>Turma</th>
                                                <th>Disciplina</th>
                                                <th>Registrada por</th>
                                                <th>Data</th>
                                                <th>Presença</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {painel.ultimasChamadas.map(c => {
                                                const percentual = c.alunos === 0 ? null : (c.presentes * 100) / c.alunos;
                                                const baixa = percentual !== null && frequencia !== null && percentual < frequencia.meta;
                                                return (
                                                    <tr key={c.id}>
                                                        <td className="home-tabela-destaque">{c.turma}</td>
                                                        <td>{c.disciplina}</td>
                                                        <td>{c.registradoPor ?? '—'}</td>
                                                        <td className="nowrap-cell">{formatarData(c.data)}</td>
                                                        <td>
                                                            <span
                                                                className={`home-selo ${baixa ? 'home-selo-negativo' : 'home-selo-positivo'}`}
                                                                title={`${c.presentes} de ${c.alunos} alunos presentes`}
                                                            >
                                                                {c.presentes}/{c.alunos}
                                                                {percentual !== null && ` · ${formatarPercentual(percentual)}`}
                                                            </span>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </Cartao>
                    )}

                    {/* RN02: Alunos em risco de infrequência */}
                    {painel.alunosEmRisco && painel.alunosEmRisco.length > 0 && (
                        <Cartao
                            titulo="Alunos em risco de infrequência"
                            acao={<span className="home-selo home-selo-negativo">{painel.alunosEmRisco.length} aluno{painel.alunosEmRisco.length !== 1 ? 's' : ''}</span>}
                            className="home-cartao-risco"
                        >
                            <p className="home-risco-descricao">
                                Alunos com mais de 20% de faltas no período
                                {painel.alunosEmRisco[0]?.periodoAvaliativo ? ` (${painel.alunosEmRisco[0].periodoAvaliativo})` : ''}.
                            </p>
                            <div className="table-container">
                                <table className="home-tabela">
                                    <thead>
                                        <tr>
                                            <th>Aluno</th>
                                            <th>Turma</th>
                                            {painel.escolaIdSelecionada === null && painel.escolas && <th>Escola</th>}
                                            <th>Frequência</th>
                                            <th>Faltas</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {painel.alunosEmRisco.map(a => (
                                            <tr key={`${a.alunoId}-${a.turma}`}>
                                                <td className="home-tabela-destaque">{a.nome}</td>
                                                <td>{a.turma}</td>
                                                {painel.escolaIdSelecionada === null && painel.escolas && <td>{a.escola}</td>}
                                                <td>
                                                    <span className={`home-selo ${a.frequenciaPercentual < 75 ? 'home-selo-negativo' : 'home-selo-aviso'}`}>
                                                        {formatarPercentual(a.frequenciaPercentual)}
                                                    </span>
                                                </td>
                                                <td>{a.totalFaltas} de {a.totalAulas}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </Cartao>
                    )}
                </div>

                <div className="home-coluna">
                    <Cartao titulo="Alertas e lembretes" className="home-cartao-alertas">
                        {painel.alertas.length === 0 ? (
                            <p className="home-vazio">Nenhum alerta no momento. Tudo em dia!</p>
                        ) : (
                            <ul className="home-alertas">
                                {painel.alertas.map(alerta => {
                                    const Icone = iconeDoAlerta[alerta.tipo];
                                    const conteudo = (
                                        <>
                                            <Icone aria-hidden="true" />
                                            <span>{alerta.mensagem}</span>
                                        </>
                                    );
                                    return (
                                        <li key={alerta.mensagem}>
                                            {alerta.pagina && podeAbrir(alerta.pagina) ? (
                                                <Link to={caminhoDaPagina(alerta.pagina)} className={`home-alerta home-alerta-${alerta.tipo}`}>
                                                    {conteudo}
                                                </Link>
                                            ) : (
                                                <div className={`home-alerta home-alerta-${alerta.tipo}`}>{conteudo}</div>
                                            )}
                                        </li>
                                    );
                                })}
                            </ul>
                        )}
                    </Cartao>

                    <Cartao titulo="Agenda acadêmica" className="home-cartao-agenda">
                        {painel.agenda.length === 0 ? (
                            <p className="home-vazio">Nenhum marco do calendário letivo nos próximos 90 dias.</p>
                        ) : (
                            <ul className="home-agenda">
                                {painel.agenda.map(evento => {
                                    const { mes, dia } = partesDaData(evento.data);
                                    const Icone = iconeDoEvento[evento.tipo];
                                    return (
                                        <li key={`${evento.data}-${evento.titulo}`}>
                                            <span className="home-agenda-data" aria-label={formatarData(evento.data)}>
                                                <span>{mes}</span>
                                                <strong>{dia}</strong>
                                            </span>
                                            <span className="home-agenda-texto">
                                                <strong>{evento.titulo}</strong>
                                                <span><Icone aria-hidden="true" /> {evento.descricao}</span>
                                            </span>
                                        </li>
                                    );
                                })}
                            </ul>
                        )}
                        {podeAbrir('anos-letivos') && (
                            <Link to={caminhoDaPagina('anos-letivos')} className="home-botao-secundario">
                                Ver anos letivos e períodos
                            </Link>
                        )}
                    </Cartao>
                </div>
            </div>
        </div>
    );
}

/* ─── RN03: Gráficos ──────────────────────────────────────── */

import type { FrequenciaDia as FrequenciaDiaType, DashboardDiarios } from './tipos';

const COR_FREQUENCIA = '#2a78d6';
const COR_META = '#dc2626';
const COR_COM_CONTEUDO = '#16a34a';
const COR_SEM_CONTEUDO = '#e5e7eb';

function GraficoFrequencia({ dados, meta }: { dados: FrequenciaDiaType[]; meta: number }) {
    const largura = 100;
    const altura = 40;
    const margemE = 8;
    const margemD = 2;
    const margemT = 4;
    const margemB = 8;
    const larg = largura - margemE - margemD;
    const alt = altura - margemT - margemB;

    if (dados.length === 0) return <p className="home-vazio">Sem dados de frequência no período.</p>;

    const min = Math.min(...dados.map(d => d.percentual), meta) - 5;
    const max = Math.max(...dados.map(d => d.percentual), 100);
    const range = max - min || 1;

    const pontos = dados.map((d, i) => {
        const x = margemE + (i / Math.max(dados.length - 1, 1)) * larg;
        const y = margemT + alt - ((d.percentual - min) / range) * alt;
        return { x, y, ...d };
    });

    const linha = pontos.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(2)},${p.y.toFixed(2)}`).join(' ');
    const areaFim = `L${pontos[pontos.length - 1].x.toFixed(2)},${(margemT + alt).toFixed(2)} L${pontos[0].x.toFixed(2)},${(margemT + alt).toFixed(2)} Z`;

    const yMeta = margemT + alt - ((meta - min) / range) * alt;

    const labelStep = Math.max(1, Math.floor(dados.length / 6));

    return (
        <svg viewBox={`0 0 ${largura} ${altura}`} className="home-svg-grafico" aria-label="Gráfico de evolução da frequência">
            {/* Área sob a linha */}
            <path d={`${linha} ${areaFim}`} fill={COR_FREQUENCIA} fillOpacity="0.08" />
            {/* Linha da meta */}
            <line x1={margemE} y1={yMeta} x2={largura - margemD} y2={yMeta} stroke={COR_META} strokeWidth="0.3" strokeDasharray="1,0.6" />
            <text x={largura - margemD - 0.5} y={yMeta - 0.8} fill={COR_META} fontSize="2.2" textAnchor="end">meta {meta}%</text>
            {/* Linha de frequência */}
            <path d={linha} fill="none" stroke={COR_FREQUENCIA} strokeWidth="0.5" strokeLinejoin="round" />
            {/* Pontos */}
            {pontos.map((p, i) => (
                <circle key={i} cx={p.x} cy={p.y} r="0.6" fill={COR_FREQUENCIA}>
                    <title>{p.data}: {formatarPercentual(p.percentual)} ({p.presencas}/{p.aulas})</title>
                </circle>
            ))}
            {/* Labels do eixo X */}
            {pontos.filter((_, i) => i % labelStep === 0 || i === pontos.length - 1).map((p, i) => (
                <text key={i} x={p.x} y={margemT + alt + 3.5} fill="#5b6475" fontSize="2" textAnchor="middle">{p.data}</text>
            ))}
        </svg>
    );
}

function GraficoDiarios({ dados }: { dados: DashboardDiarios }) {
    const total = dados.totalChamadas;
    if (total === 0) return <p className="home-vazio">Sem chamadas registradas no período.</p>;

    const pct = dados.percentual;
    const raio = 36;
    const circunferencia = 2 * Math.PI * raio;
    const preenchido = (pct / 100) * circunferencia;

    return (
        <div className="home-diarios-grafico">
            <svg viewBox="0 0 100 100" className="home-svg-donut" aria-label={`${formatarPercentual(pct)} com conteúdo`}>
                <circle cx="50" cy="50" r={raio} fill="none" stroke={COR_SEM_CONTEUDO} strokeWidth="8" />
                <circle
                    cx="50" cy="50" r={raio} fill="none"
                    stroke={COR_COM_CONTEUDO} strokeWidth="8" strokeLinecap="round"
                    strokeDasharray={`${preenchido} ${circunferencia}`}
                    transform="rotate(-90 50 50)"
                />
                <text x="50" y="47" textAnchor="middle" fill="var(--home-titulo)" fontSize="12" fontWeight="700">
                    {formatarPercentual(pct)}
                </text>
                <text x="50" y="57" textAnchor="middle" fill="#5b6475" fontSize="5">
                    com conteúdo
                </text>
            </svg>
            <div className="home-diarios-legenda">
                <span><span className="home-legenda-cor" style={{ background: COR_COM_CONTEUDO }} /> Com conteúdo: {dados.chamadasComConteudo}</span>
                <span><span className="home-legenda-cor" style={{ background: COR_SEM_CONTEUDO }} /> Sem conteúdo: {dados.chamadasSemConteudo}</span>
                <span className="home-diarios-total">Total de chamadas: {total}</span>
            </div>
        </div>
    );
}

export default PainelGeral;
