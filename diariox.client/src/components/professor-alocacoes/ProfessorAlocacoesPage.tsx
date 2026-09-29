import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { apiFetch } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import '../MainContent.css';
import { usePermissoes } from '../../hooks/usePermissoes';

interface Professor {
    id: number;
    nome: string;
    disciplinas: { id: number; nome: string }[];
}

interface AlocacaoListItem {
    id: number;
    professorNome: string;
    escolaNome: string;
    modalidadeNome: string;
    etapaNome: string;
    turmaNome: string;
    disciplinaNome: string;
    turno: string;
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
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();
    const [view, setView] = useState<'list' | 'form'>('list');
    const [alocacoes, setAlocacoes] = useState<AlocacaoListItem[]>([]);
    const [listLoading, setListLoading] = useState(true);
    const [deletingId, setDeletingId] = useState<number | null>(null);
    const [filterProfessor, setFilterProfessor] = useState('');
    const [filterEscola, setFilterEscola] = useState('');
    const [filterTurma, setFilterTurma] = useState('');
    const [applied, setApplied] = useState({ professor: '', escola: '', turma: '' });
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

    const loadAlocacoes = useCallback(async () => {
        setListLoading(true);
        try {
            const response = await apiFetch('/api/professor-alocacoes');
            if (!response.ok) throw new Error('Falha ao carregar professores alocados.');
            setAlocacoes(await response.json() as AlocacaoListItem[]);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao carregar professores alocados.');
        } finally {
            setListLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadAlocacoes();
    }, [loadAlocacoes]);

    const handleConsultar = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setApplied({
            professor: filterProfessor.trim().toLowerCase(),
            escola: filterEscola.trim().toLowerCase(),
            turma: filterTurma.trim().toLowerCase(),
        });
    };

    const handleLimparFiltros = () => {
        setFilterProfessor('');
        setFilterEscola('');
        setFilterTurma('');
        setApplied({ professor: '', escola: '', turma: '' });
    };

    const alocacoesFiltradas = alocacoes.filter(item =>
        item.professorNome.toLowerCase().includes(applied.professor)
        && item.escolaNome.toLowerCase().includes(applied.escola)
        && item.turmaNome.toLowerCase().includes(applied.turma));

    const handleAlocar = () => {
        setSuccess(null);
        setError(null);
        void loadDisponiveis('');
        setView('form');
    };

    const handleVoltar = () => {
        setError(null);
        setView('list');
        void loadDisponiveis('');
    };

    const handleDelete = async (item: AlocacaoListItem) => {
        const confirmed = await confirm({
            title: 'Desalocar professor',
            variant: 'danger',
            confirmLabel: 'Desalocar',
            message: (
                <>
                    <p>
                        Deseja desalocar <strong>{item.professorNome}</strong> de{' '}
                        <strong>{item.disciplinaNome}</strong> ({item.turmaNome}, {item.escolaNome})?
                    </p>
                    <p>O cadastro do professor é mantido e ele poderá ser alocado novamente.</p>
                </>
            ),
        });
        if (!confirmed) return;

        setError(null);
        setSuccess(null);
        setDeletingId(item.id);
        try {
            const response = await apiFetch(`/api/professor-alocacoes/${item.id}`, { method: 'DELETE' });
            const data = await response.json() as { message?: string };
            if (!response.ok) throw new Error(data.message ?? 'Falha ao desalocar professor.');
            setSuccess(data.message ?? 'Professor desalocado com sucesso!');
            await loadAlocacoes();
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao desalocar professor.');
        } finally {
            setDeletingId(null);
        }
    };

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
            const confirmed = await confirm({
                title: substituicoes.length === 1 ? 'Substituir professor' : 'Substituir professores',
                variant: 'warning',
                confirmLabel: 'Substituir',
                message: (
                    <>
                        <p>{substituicoes.length === 1 ? 'Esta vaga já possui professor alocado:' : 'Estas vagas já possuem professor alocado:'}</p>
                        <ul className="confirm-dialog-list">
                            {substituicoes.map(item => (
                                <li key={`${item.turmaId}-${item.disciplinaId}`}>
                                    <strong>{item.disciplinaNome}</strong> em {item.turmaNome}
                                    {item.professorAtualNome && <> — atualmente com {item.professorAtualNome}</>}
                                </li>
                            ))}
                        </ul>
                        <p>Ao confirmar, a alocação atual será encerrada e substituída.</p>
                    </>
                ),
            });
            if (!confirmed) return;
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
            setGrade([]);
            await loadDisponiveis('');
            await loadAlocacoes();
            setSuccess(data.message ?? 'Enturmação realizada com sucesso!');
            setView('list');
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao confirmar alocação.');
        } finally {
            setSaving(false);
        }
    };

    if (view === 'list') {
        return (
            <div className="school-page">
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>Professores Alocados</h2>
                            <p>Consulte, edite e remova professores alocados.</p>
                        </div>
                        {can('alocacao-professor.criar') && (
                            <button type="button" className="primary-button" onClick={handleAlocar}>
                                + Alocar Professor
                            </button>
                        )}
                    </div>

                    <form className="filter-bar" onSubmit={handleConsultar}>
                        <div className="filter-field">
                            <label htmlFor="filtro-professor">Professor</label>
                            <input id="filtro-professor" type="text" className="filter-input" value={filterProfessor} onChange={e => setFilterProfessor(e.target.value)} />
                        </div>
                        <div className="filter-field">
                            <label htmlFor="filtro-escola">Escola</label>
                            <input id="filtro-escola" type="text" className="filter-input" value={filterEscola} onChange={e => setFilterEscola(e.target.value)} />
                        </div>
                        <div className="filter-field">
                            <label htmlFor="filtro-turma">Turma</label>
                            <input id="filtro-turma" type="text" className="filter-input" value={filterTurma} onChange={e => setFilterTurma(e.target.value)} />
                        </div>
                        <button type="submit" className="filter-button">Consultar</button>
                        <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                    </form>

                    <FeedbackMessage message={error} />
                    <FeedbackMessage message={success} type="success" />

                    {listLoading || alocacoesFiltradas.length === 0 ? (
                        <EmptyState
                            loading={listLoading}
                            loadingMessage="Carregando professores alocados..."
                            emptyMessage="Nenhum professor alocado encontrado."
                            emptySubMessage={alocacoes.length === 0 ? 'Clique em Alocar Professor para começar.' : 'Tente ajustar os filtros.'}
                        />
                    ) : (
                        <div className="table-responsive">
                            <table className="data-table">
                                <thead>
                                    <tr>
                                        <th>Professor</th>
                                        <th>Escola</th>
                                        <th>Modalidade</th>
                                        <th>Etapa</th>
                                        <th>Turma</th>
                                        <th>Disciplina</th>
                                        <th>Turno</th>
                                        <th>Ações</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {alocacoesFiltradas.map(item => (
                                        <tr key={item.id}>
                                            <td>{item.professorNome}</td>
                                            <td>{item.escolaNome}</td>
                                            <td>{item.modalidadeNome}</td>
                                            <td>{item.etapaNome}</td>
                                            <td>{item.turmaNome}</td>
                                            <td>{item.disciplinaNome}</td>
                                            <td>{turnoLabels[item.turno] ?? item.turno}</td>
                                            <td>
                                                {can('alocacao-professor.excluir') && (
                                                    <button
                                                        type="button"
                                                        className="table-action-button danger"
                                                        disabled={deletingId === item.id}
                                                        onClick={() => void handleDelete(item)}
                                                    >
                                                        Desalocar
                                                    </button>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </section>
                {confirmDialog}
            </div>
        );
    }

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Alocação de Professor</h2>
                        <p>Vincule professores habilitados às turmas e disciplinas do ano letivo.</p>
                    </div>
                    <button type="button" className="btn btn-secondary" onClick={handleVoltar}>← Voltar</button>
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
                        {can('alocacao-professor.criar') && <button className="btn btn-primary" type="button" onClick={addToGrade} disabled={loading || Object.values(selecoes).every(items => items.length === 0)}>Adicionar à Grade do Professor</button>}
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
                    {can('alocacao-professor.criar') && <button className="btn btn-primary" type="button" onClick={() => void confirmAllocation()} disabled={saving || !professorId || grade.length === 0}>Confirmar Alocação</button>}
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
            {confirmDialog}
        </div>
    );
}

export default ProfessorAlocacoesPage;