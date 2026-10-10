import { useEffect, useState } from 'react';
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

/** Painel geral da home (gestão, secretaria): indicadores, atalhos, alertas, últimas chamadas e agenda, conforme o perfil. */
function PainelGeral() {
    const { can } = usePermissoes();
    const [painel, setPainel] = useState<Dashboard | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/dashboard');
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Dashboard;
                if (!cancelled) setPainel(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar o painel.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

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

export default PainelGeral;
