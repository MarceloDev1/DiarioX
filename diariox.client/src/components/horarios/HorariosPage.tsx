import { useCallback, useEffect, useMemo, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import { usePermissoes } from '../../hooks/usePermissoes';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import './Horarios.css';

interface TurmaResumo {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    anoReferencia: number;
    tipoFrequencia: string;
    tempos: number;
}

interface Tempo {
    diaSemana: number;
    ordem: number;
    disciplinaId: number;
}

interface GradeTurma {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    anoReferencia: number;
    tipoFrequencia: string;
    maxTempos: number;
    disciplinas: { id: number; nome: string; professores: string[] }[];
    tempos: Tempo[];
}

const DIAS = [
    { id: 1, nome: 'Segunda' },
    { id: 2, nome: 'Terça' },
    { id: 3, nome: 'Quarta' },
    { id: 4, nome: 'Quinta' },
    { id: 5, nome: 'Sexta' },
    { id: 6, nome: 'Sábado' },
];

const TEMPOS_PADRAO = 5;
const SABADO = 6;

/** "diaSemana-ordem" → disciplina. */
type Celulas = Record<string, number>;

const chave = (dia: number, ordem: number) => `${dia}-${ordem}`;

function paraCelulas(tempos: Tempo[]): Celulas {
    return Object.fromEntries(tempos.map(t => [chave(t.diaSemana, t.ordem), t.disciplinaId]));
}

/**
 * Grade semanal de cada turma: em cada dia e tempo de aula, a disciplina. É dela que o painel do professor
 * tira as aulas do dia, as aulas previstas no calendário e as pendências de registro.
 */
function HorariosPage() {
    const { can } = usePermissoes();
    const podeEditar = can('horarios.editar');

    const [turmas, setTurmas] = useState<TurmaResumo[]>([]);
    const [turmaId, setTurmaId] = useState<number | null>(null);
    const [grade, setGrade] = useState<GradeTurma | null>(null);
    const [celulas, setCelulas] = useState<Celulas>({});
    const [quantidadeTempos, setQuantidadeTempos] = useState(TEMPOS_PADRAO);
    const [comSabado, setComSabado] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/horarios/turmas');
                if (!response.ok) throw new Error(await readApiError(response));
                const carregadas = (await response.json()) as TurmaResumo[];
                if (!cancelled) setTurmas(carregadas);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as turmas.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    const aplicarGrade = useCallback((carregada: GradeTurma) => {
        setGrade(carregada);
        setCelulas(paraCelulas(carregada.tempos));
        setQuantidadeTempos(Math.max(TEMPOS_PADRAO, ...carregada.tempos.map(t => t.ordem)));
        setComSabado(carregada.tempos.some(t => t.diaSemana === SABADO));
    }, []);

    useEffect(() => {
        if (turmaId === null) return;
        let cancelled = false;

        async function load() {
            setError(null);
            setSuccess(null);
            try {
                const response = await apiFetch(`/api/horarios/turmas/${turmaId}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const carregada = (await response.json()) as GradeTurma;
                if (!cancelled) aplicarGrade(carregada);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar a grade.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turmaId, aplicarGrade]);

    const dias = comSabado ? DIAS : DIAS.filter(d => d.id !== SABADO);
    const alterada = useMemo(() => {
        if (!grade) return false;
        const original = paraCelulas(grade.tempos);
        const chaves = new Set([...Object.keys(original), ...Object.keys(celulas)]);
        return [...chaves].some(k => original[k] !== celulas[k]);
    }, [grade, celulas]);

    const aulasPorDisciplina = useMemo(() => {
        const contagem = new Map<number, number>();
        for (const disciplinaId of Object.values(celulas)) contagem.set(disciplinaId, (contagem.get(disciplinaId) ?? 0) + 1);
        return contagem;
    }, [celulas]);

    const handleCelula = (dia: number, ordem: number, valor: string) => {
        setSuccess(null);
        setCelulas(atual => {
            const proxima = { ...atual };
            if (valor) proxima[chave(dia, ordem)] = Number(valor);
            else delete proxima[chave(dia, ordem)];
            return proxima;
        });
    };

    const handleRemoverTempo = () => {
        const ultimo = quantidadeTempos;
        setCelulas(atual => Object.fromEntries(Object.entries(atual).filter(([k]) => Number(k.split('-')[1]) !== ultimo)));
        setQuantidadeTempos(ultimo - 1);
    };

    const handleSabado = (marcado: boolean) => {
        setComSabado(marcado);
        if (!marcado) setCelulas(atual => Object.fromEntries(Object.entries(atual).filter(([k]) => !k.startsWith(`${SABADO}-`))));
    };

    const handleSalvar = async () => {
        if (!grade) return;
        setIsSaving(true);
        setError(null);
        setSuccess(null);
        try {
            const tempos = Object.entries(celulas).map(([k, disciplinaId]) => {
                const [diaSemana, ordem] = k.split('-').map(Number);
                return { diaSemana, ordem, disciplinaId };
            });
            const response = await apiFetch(`/api/horarios/turmas/${grade.turmaId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ tempos }),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            const salva = (await response.json()) as GradeTurma;
            aplicarGrade(salva);
            setTurmas(atual => atual.map(t => t.turmaId === salva.turmaId ? { ...t, tempos: salva.tempos.length } : t));
            setSuccess('Grade de horários salva.');
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar a grade.');
        } finally {
            setIsSaving(false);
        }
    };

    const ultimaLinhaOcupada = Object.keys(celulas).some(k => Number(k.split('-')[1]) === quantidadeTempos);

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Grade de Horários</h1>
            </div>

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={success} type="success" />

            {isLoading ? (
                <div className="loading">Carregando turmas...</div>
            ) : turmas.length === 0 ? (
                <EmptyState emptyMessage="Nenhuma turma ativa disponível." />
            ) : (
                <>
                    <div className="chamada-filtros">
                        <div className="form-group">
                            <label htmlFor="horario-turma">Turma</label>
                            <select
                                id="horario-turma"
                                value={turmaId ?? ''}
                                onChange={e => setTurmaId(e.target.value ? Number(e.target.value) : null)}
                                disabled={isSaving}
                            >
                                <option value="">Selecione...</option>
                                {turmas.map(t => (
                                    <option key={t.turmaId} value={t.turmaId}>
                                        {t.turmaNome} — {t.anoReferencia} ({t.escolaNome}){t.tempos === 0 ? ' · sem grade' : ''}
                                    </option>
                                ))}
                            </select>
                        </div>
                        {grade && (
                            <div className="form-group">
                                <label className="horarios-checkbox">
                                    <input type="checkbox" checked={comSabado} onChange={e => handleSabado(e.target.checked)} disabled={!podeEditar} />
                                    Aulas aos sábados
                                </label>
                                <span className="field-hint">
                                    O sábado só tem aula quando o Calendário Letivo o marca como Dia Letivo Especial.
                                </span>
                            </div>
                        )}
                    </div>

                    {!grade || grade.turmaId !== turmaId ? (
                        <EmptyState emptyMessage={turmaId ? 'Carregando a grade...' : 'Selecione a turma para montar a grade semanal.'} />
                    ) : (
                        <>
                            {grade.tipoFrequencia === 'DIARIA' && (
                                <p className="field-hint horarios-aviso">
                                    Turma de frequência diária (Anos Iniciais): o painel do professor considera aula em todo dia letivo,
                                    com ou sem grade. A grade aqui é opcional e serve de referência.
                                </p>
                            )}

                            <div className="table-container">
                                <table className="data-table horarios-grade">
                                    <thead>
                                        <tr>
                                            <th scope="col">Tempo</th>
                                            {dias.map(d => <th key={d.id} scope="col">{d.nome}</th>)}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {Array.from({ length: quantidadeTempos }, (_, i) => i + 1).map(ordem => (
                                            <tr key={ordem}>
                                                <th scope="row">{ordem}º</th>
                                                {dias.map(d => (
                                                    <td key={d.id}>
                                                        <select
                                                            aria-label={`${d.nome}, ${ordem}º tempo`}
                                                            value={celulas[chave(d.id, ordem)] ?? ''}
                                                            onChange={e => handleCelula(d.id, ordem, e.target.value)}
                                                            disabled={!podeEditar || isSaving}
                                                            className={celulas[chave(d.id, ordem)] ? 'horarios-celula-ocupada' : ''}
                                                        >
                                                            <option value="">—</option>
                                                            {grade.disciplinas.map(disc => <option key={disc.id} value={disc.id}>{disc.nome}</option>)}
                                                        </select>
                                                    </td>
                                                ))}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>

                            {podeEditar && (
                                <div className="horarios-linhas">
                                    <button
                                        type="button"
                                        className="btn btn-secondary btn-sm"
                                        onClick={() => setQuantidadeTempos(q => q + 1)}
                                        disabled={quantidadeTempos >= grade.maxTempos || isSaving}
                                    >
                                        + Tempo
                                    </button>
                                    <button
                                        type="button"
                                        className="btn btn-secondary btn-sm"
                                        onClick={handleRemoverTempo}
                                        disabled={quantidadeTempos <= 1 || isSaving}
                                        title={ultimaLinhaOcupada ? 'Remove o último tempo e as disciplinas dele' : undefined}
                                    >
                                        − Tempo
                                    </button>
                                </div>
                            )}

                            <section className="horarios-resumo">
                                <h2>Aulas por semana</h2>
                                {grade.disciplinas.length === 0 ? (
                                    <p className="field-hint">Nenhuma disciplina ativa vinculada à etapa de ensino da turma.</p>
                                ) : (
                                    <ul>
                                        {grade.disciplinas.map(d => (
                                            <li key={d.id}>
                                                <strong>{d.nome}</strong>: {aulasPorDisciplina.get(d.id) ?? 0}
                                                <span className="field-hint">
                                                    {d.professores.length > 0 ? d.professores.join(', ') : 'sem professor alocado'}
                                                </span>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </section>

                            {podeEditar && (
                                <div className="form-actions">
                                    <button type="button" className="btn btn-primary" onClick={() => void handleSalvar()} disabled={!alterada || isSaving}>
                                        {isSaving ? 'Salvando...' : 'Salvar grade'}
                                    </button>
                                    <button type="button" className="btn btn-secondary" onClick={() => aplicarGrade(grade)} disabled={!alterada || isSaving}>
                                        Descartar alterações
                                    </button>
                                </div>
                            )}
                        </>
                    )}
                </>
            )}
        </div>
    );
}

export default HorariosPage;
