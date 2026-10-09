import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { apiFetch, readApiError } from '../../utils/api';
import { lerSelecaoDiario, turmaPedida } from '../../utils/selecaoDiario';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import LancarNotas from './LancarNotas';
import MediasNotas from './MediasNotas';
import { hojeIso } from '../chamada/tipos';
import type { NotaPeriodo, NotaTurma } from './tipos';
import './Notas.css';

type Aba = 'lancar' | 'medias';

const abas: { id: Aba; label: string }[] = [
    { id: 'lancar', label: 'Lançar notas' },
    { id: 'medias', label: 'Médias do ano' },
];

/** Período em andamento; fora dos períodos, o último que já começou (ou o primeiro). */
function periodoAtual(periodos: NotaPeriodo[]): number | null {
    if (periodos.length === 0) return null;
    const hoje = hojeIso();
    const vigente = periodos.find(p => p.dataInicio <= hoje && hoje <= p.dataTermino);
    const iniciados = periodos.filter(p => p.dataInicio <= hoje);
    return (vigente ?? iniciados[iniciados.length - 1] ?? periodos[0]).id;
}

function NotasPage() {
    const [searchParams] = useSearchParams();
    // Atalho da home do professor: turma, disciplina e período já escolhidos.
    const [pedido] = useState(() => lerSelecaoDiario(searchParams));
    const [turmas, setTurmas] = useState<NotaTurma[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [turmaId, setTurmaId] = useState<number | null>(null);
    const [disciplinaId, setDisciplinaId] = useState<number | null>(null);
    const [periodoId, setPeriodoId] = useState<number | null>(null);
    const [aba, setAba] = useState<Aba>('lancar');

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/notas/turmas');
                if (!response.ok) throw new Error(await readApiError(response));
                const carregadas = (await response.json()) as NotaTurma[];
                if (cancelled) return;

                setTurmas(carregadas);
                const pedida = turmaPedida(carregadas, pedido);
                if (pedida) {
                    const { turma: alvo } = pedida;
                    setTurmaId(alvo.turmaId);
                    setDisciplinaId(pedida.disciplinaId);
                    setPeriodoId(alvo.periodos.some(p => p.id === pedido.periodoId) ? pedido.periodoId : periodoAtual(alvo.periodos));
                } else if (carregadas.length === 1) {
                    // Professor com uma única turma/disciplina já cai direto nela.
                    setTurmaId(carregadas[0].turmaId);
                    setPeriodoId(periodoAtual(carregadas[0].periodos));
                    if (carregadas[0].disciplinas.length === 1) setDisciplinaId(carregadas[0].disciplinas[0].id);
                }
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as turmas.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [pedido]);

    const turma = turmas.find(t => t.turmaId === turmaId) ?? null;
    const disciplina = turma?.disciplinas.find(d => d.id === disciplinaId) ?? null;

    const handleTurmaChange = (value: string) => {
        const proxima = turmas.find(t => t.turmaId === Number(value)) ?? null;
        setTurmaId(proxima?.turmaId ?? null);
        setDisciplinaId(proxima?.disciplinas.length === 1 ? proxima.disciplinas[0].id : null);
        setPeriodoId(proxima ? periodoAtual(proxima.periodos) : null);
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Notas</h1>
            </div>

            <FeedbackMessage message={error} type="error" />

            {isLoading ? (
                <div className="loading">Carregando turmas...</div>
            ) : turmas.length === 0 ? (
                <EmptyState
                    emptyMessage="Nenhuma turma disponível para lançamento de notas."
                    emptySubMessage="Professores só veem as turmas e disciplinas em que estão alocados; as turmas precisam de um ano letivo com períodos avaliativos."
                />
            ) : (
                <>
                    <div className="chamada-filtros">
                        <div className="form-group">
                            <label htmlFor="notas-turma">Turma</label>
                            <select id="notas-turma" value={turmaId ?? ''} onChange={e => handleTurmaChange(e.target.value)}>
                                <option value="">Selecione...</option>
                                {turmas.map(t => (
                                    <option key={t.turmaId} value={t.turmaId}>
                                        {t.turmaNome} — {t.anoReferencia} ({t.escolaNome}){t.ativa ? '' : ' · inativa'}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="form-group">
                            <label htmlFor="notas-disciplina">Disciplina</label>
                            <select
                                id="notas-disciplina"
                                value={disciplinaId ?? ''}
                                onChange={e => setDisciplinaId(e.target.value ? Number(e.target.value) : null)}
                                disabled={!turma}
                            >
                                <option value="">Selecione...</option>
                                {turma?.disciplinas.map(d => <option key={d.id} value={d.id}>{d.nome}</option>)}
                            </select>
                        </div>
                    </div>

                    {!turma || !disciplina || periodoId === null ? (
                        <EmptyState emptyMessage="Selecione a turma e a disciplina para lançar ou consultar as notas." />
                    ) : (
                        <div className="form-grid">
                            <div className="form-tabs" role="tablist" aria-label="Notas">
                                {abas.map(a => (
                                    <button
                                        key={a.id}
                                        type="button"
                                        role="tab"
                                        aria-selected={aba === a.id}
                                        className={`form-tab${aba === a.id ? ' active' : ''}`}
                                        onClick={() => setAba(a.id)}
                                    >
                                        {a.label}
                                    </button>
                                ))}
                            </div>

                            <div className="chamada-painel" role="tabpanel">
                                {aba === 'lancar' && (
                                    <LancarNotas
                                        key={`${turma.turmaId}-${disciplina.id}`}
                                        turma={turma}
                                        disciplinaId={disciplina.id}
                                        periodoId={periodoId}
                                        onPeriodoChange={setPeriodoId}
                                    />
                                )}
                                {aba === 'medias' && (
                                    <MediasNotas key={`${turma.turmaId}-${disciplina.id}`} turma={turma} disciplinaId={disciplina.id} />
                                )}
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

export default NotasPage;
