import { useEffect, useState } from 'react';
import { apiFetch } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import '../MainContent.css';

interface Professor {
    id: number;
    nome: string;
    escolaId: number;
    disciplinas: { id: number; nome: string }[];
}

interface Disponibilidade {
    turmaId: number;
    turmaNome: string;
    anoLetivoId: number;
    anoReferencia: number;
    turno: string;
    disciplinas: { disciplinaId: number; disciplinaNome: string; alocacaoId: number | null; professorAtualNome: string | null }[];
}

interface GradeItem {
    turmaId: number;
    turmaNome: string;
    anoReferencia: number;
    turno: string;
    disciplinaId: number;
    disciplinaNome: string;
    substituirAlocacaoId: number | null;
    professorAtualNome: string | null;
}

const turnoLabels: Record<string, string> = { MANHA: 'Manhã', TARDE: 'Tarde', NOITE: 'Noite', INTEGRAL: 'Integral' };

function ProfessorAlocacoesPage() {
    const [professores, setProfessores] = useState<Professor[]>([]);
    const [disponiveis, setDisponiveis] = useState<Disponibilidade[]>([]);
    const [professorId, setProfessorId] = useState('');
    const [selecoes, setSelecoes] = useState<Record<number, number[]>>({});
    const [grade, setGrade] = useState<GradeItem[]>([]);
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        void apiFetch('/api/professores')
            .then(response => response.ok ? response.json() : Promise.reject(new Error('Falha ao carregar professores.')))
            .then(data => setProfessores(data as Professor[]))
            .catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar professores.'));
    }, []);

    const loadDisponiveis = async (id: string) => {
        setProfessorId(id);
        setGrade([]);
        setSelecoes({});
        setSuccess(null);
        setError(null);
        if (!id) {
            setDisponiveis([]);
            return;
        }

        setLoading(true);
        try {
            const response = await apiFetch(`/api/professor-alocacoes/professor/${id}/disponiveis`);
            if (!response.ok) throw new Error('Falha ao carregar turmas disponíveis.');
            setDisponiveis(await response.json() as Disponibilidade[]);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao carregar turmas disponíveis.');
        } finally {
            setLoading(false);
        }
    };

    const toggleDisciplina = (turmaId: number, disciplinaId: number) => {
        setSelecoes(current => {
            const atual = current[turmaId] ?? [];
            const next = atual.includes(disciplinaId) ? atual.filter(id => id !== disciplinaId) : [...atual, disciplinaId];
            return { ...current, [turmaId]: next };
        });
    };

    const addToGrade = () => {
        const itens = disponiveis.flatMap(turma => (selecoes[turma.turmaId] ?? []).map(disciplinaId => {
            const disciplina = turma.disciplinas.find(item => item.disciplinaId === disciplinaId);
            return disciplina ? {
                turmaId: turma.turmaId,
                turmaNome: turma.turmaNome,
                anoReferencia: turma.anoReferencia,
                turno: turma.turno,
                disciplinaId: disciplina.disciplinaId,
                disciplinaNome: disciplina.disciplinaNome,
                substituirAlocacaoId: disciplina.alocacaoId,
                professorAtualNome: disciplina.professorAtualNome,
            } : null;
        }).filter((item): item is GradeItem => item !== null));

        setGrade(current => [...current, ...itens.filter(item => !current.some(existing => existing.turmaId === item.turmaId && existing.disciplinaId === item.disciplinaId))]);
        setSelecoes({});
    };

    const removeFromGrade = (item: GradeItem) =>
        setGrade(current => current.filter(existing => !(existing.turmaId === item.turmaId && existing.disciplinaId === item.disciplinaId)));

    const confirmAllocation = async () => {
        if (!professorId || grade.length === 0) return;
        const substituicoes = grade.filter(item => item.substituirAlocacaoId !== null);
        if (substituicoes.length > 0) {
            const nomes = substituicoes.map(item => `${item.disciplinaNome} em ${item.turmaNome}`).join(', ');
            if (!window.confirm(`Esta disciplina já possui professor: ${nomes}. Deseja substituí-lo?`)) return;
        }

        setSaving(true);
        setError(null);
        setSuccess(null);
        try {
            const response = await apiFetch('/api/professor-alocacoes', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    professorId: Number(professorId),
                    itens: grade.map(item => ({ turmaId: item.turmaId, disciplinaId: item.disciplinaId, substituirAlocacaoId: item.substituirAlocacaoId })),
                }),
            });
            const data = await response.json() as { message?: string };
            if (!response.ok) throw new Error(data.message ?? 'Falha ao confirmar alocação.');
            setSuccess(data.message ?? 'Enturmação realizada com sucesso!');
            setGrade([]);
            await loadDisponiveis(professorId);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao confirmar alocação.');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Alocação de Professor</h2>
                        <p>Vincule professores habilitados às turmas e disciplinas do ano letivo.</p>
                    </div>
                </div>
                <FeedbackMessage message={error} />
                <FeedbackMessage message={success} type="success" />
                <div className="cadastro-form escola-form">
                    <div className="form-field">
                        <label htmlFor="alocacao-professor">Professor</label>
                        <select id="alocacao-professor" value={professorId} onChange={event => void loadDisponiveis(event.target.value)}>
                            <option value="">Selecione um professor</option>
                            {professores.map(professor => <option key={professor.id} value={professor.id}>{professor.nome}</option>)}
                        </select>
                    </div>
                </div>
            </section>

            {professorId && (
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>Turmas e Disciplinas disponíveis</h2>
                            <p>Somente disciplinas habilitadas para o professor são exibidas.</p>
                        </div>
                        <button className="btn btn-primary" type="button" onClick={addToGrade} disabled={loading || Object.values(selecoes).every(items => items.length === 0)}>Adicionar à Grade do Professor</button>
                    </div>
                    {loading ? <div className="loading">Carregando turmas...</div> : disponiveis.length === 0 ? (
                        <EmptyState emptyMessage="Nenhuma turma disponível para as habilitações cadastradas deste professor." />
                    ) : (
                        <div className="allocation-grid">
                            {disponiveis.map(turma => (
                                <fieldset className="allocation-group" key={turma.turmaId}>
                                    <legend>{turma.turmaNome} · {turma.anoReferencia} · {turnoLabels[turma.turno] ?? turma.turno}</legend>
                                    {turma.disciplinas.map(disciplina => (
                                        <label className="allocation-option" key={disciplina.disciplinaId}>
                                            <input type="checkbox" checked={(selecoes[turma.turmaId] ?? []).includes(disciplina.disciplinaId)} onChange={() => toggleDisciplina(turma.turmaId, disciplina.disciplinaId)} />
                                            <span>{disciplina.disciplinaNome}</span>
                                            {disciplina.professorAtualNome && <small>Professor atual: {disciplina.professorAtualNome}</small>}
                                        </label>
                                    ))}
                                </fieldset>
                            ))}
                        </div>
                    )}
                </section>
            )}

            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Tabela de conferência</h2>
                        <p>Revise os vínculos antes de confirmar a alocação.</p>
                    </div>
                    <button className="btn btn-primary" type="button" onClick={() => void confirmAllocation()} disabled={saving || !professorId || grade.length === 0}>Confirmar Alocação</button>
                </div>
                {grade.length === 0 ? <EmptyState emptyMessage="Nenhuma alocação adicionada à grade." /> : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead><tr><th>Turma</th><th>Ano</th><th>Turno</th><th>Disciplina</th><th>Ação</th></tr></thead>
                            <tbody>{grade.map(item => <tr key={`${item.turmaId}-${item.disciplinaId}`}><td>{item.turmaNome}</td><td>{item.anoReferencia}</td><td>{turnoLabels[item.turno] ?? item.turno}</td><td>{item.disciplinaNome}</td><td><button className="table-action-button danger" type="button" onClick={() => removeFromGrade(item)}>Remover</button></td></tr>)}</tbody>
                        </table>
                    </div>
                )}
            </section>
        </div>
    );
}

export default ProfessorAlocacoesPage;