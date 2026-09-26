import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import LancarChamada from './LancarChamada';
import HistoricoChamadas from './HistoricoChamadas';
import FrequenciaChamada from './FrequenciaChamada';
import { hojeIso, type ChamadaTurma } from './tipos';

type Aba = 'lancar' | 'historico' | 'frequencia';

const abas: { id: Aba; label: string }[] = [
    { id: 'lancar', label: '✅ Lançar chamada' },
    { id: 'historico', label: '🗓️ Histórico' },
    { id: 'frequencia', label: '📊 Frequência' },
];

function ChamadaPage() {
    const [turmas, setTurmas] = useState<ChamadaTurma[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [turmaId, setTurmaId] = useState<number | null>(null);
    const [disciplinaId, setDisciplinaId] = useState<number | null>(null);
    const [aba, setAba] = useState<Aba>('lancar');
    // Data da chamada aberta na aba "Lançar"; o histórico também abre chamadas antigas por aqui.
    const [data, setData] = useState(hojeIso);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/chamadas/turmas');
                if (!response.ok) throw new Error(await readApiError(response));
                const carregadas = (await response.json()) as ChamadaTurma[];
                if (cancelled) return;

                setTurmas(carregadas);
                // Professor com uma única turma/disciplina já cai direto nela.
                if (carregadas.length === 1) {
                    setTurmaId(carregadas[0].turmaId);
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
    }, []);

    const turma = turmas.find(t => t.turmaId === turmaId) ?? null;
    const disciplina = turma?.disciplinas.find(d => d.id === disciplinaId) ?? null;

    const handleTurmaChange = (value: string) => {
        const proxima = turmas.find(t => t.turmaId === Number(value)) ?? null;
        setTurmaId(proxima?.turmaId ?? null);
        setDisciplinaId(proxima?.disciplinas.length === 1 ? proxima.disciplinas[0].id : null);
        setData(hojeIso());
    };

    const handleAbrirChamada = (dataChamada: string) => {
        setData(dataChamada);
        setAba('lancar');
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Chamada</h1>
            </div>

            <FeedbackMessage message={error} type="error" />

            {isLoading ? (
                <div className="loading">Carregando turmas...</div>
            ) : turmas.length === 0 ? (
                <EmptyState
                    emptyMessage="Nenhuma turma disponível para chamada."
                    emptySubMessage="Professores só veem as turmas e disciplinas em que estão alocados (Alocação de Professor)."
                />
            ) : (
                <>
                    <div className="chamada-filtros">
                        <div className="form-group">
                            <label htmlFor="chamada-turma">Turma</label>
                            <select id="chamada-turma" value={turmaId ?? ''} onChange={e => handleTurmaChange(e.target.value)}>
                                <option value="">Selecione...</option>
                                {turmas.map(t => (
                                    <option key={t.turmaId} value={t.turmaId}>
                                        {t.turmaNome} — {t.anoReferencia} ({t.escolaNome})
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="form-group">
                            <label htmlFor="chamada-disciplina">Disciplina</label>
                            <select
                                id="chamada-disciplina"
                                value={disciplinaId ?? ''}
                                onChange={e => setDisciplinaId(e.target.value ? Number(e.target.value) : null)}
                                disabled={!turma}
                            >
                                <option value="">Selecione...</option>
                                {turma?.disciplinas.map(d => <option key={d.id} value={d.id}>{d.nome}</option>)}
                            </select>
                        </div>
                    </div>

                    {!turma || !disciplina ? (
                        <EmptyState emptyMessage="Selecione a turma e a disciplina para lançar ou consultar a chamada." />
                    ) : (
                        <div className="form-grid">
                            <div className="form-tabs" role="tablist" aria-label="Chamada">
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
                                    <LancarChamada
                                        key={`${turma.turmaId}-${disciplina.id}`}
                                        turma={turma}
                                        disciplinaId={disciplina.id}
                                        data={data}
                                        onDataChange={setData}
                                    />
                                )}
                                {aba === 'historico' && (
                                    <HistoricoChamadas
                                        key={`${turma.turmaId}-${disciplina.id}`}
                                        turmaId={turma.turmaId}
                                        disciplinaId={disciplina.id}
                                        onAbrir={handleAbrirChamada}
                                    />
                                )}
                                {aba === 'frequencia' && (
                                    <FrequenciaChamada key={`${turma.turmaId}-${disciplina.id}`} turma={turma} disciplinaId={disciplina.id} />
                                )}
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

export default ChamadaPage;
