import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { apiFetch } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import MultiSelectDropdown from '../ui/MultiSelectDropdown';
import '../MainContent.css';
import { usePermissoes } from '../../hooks/usePermissoes';

interface Professor {
    id: number;
    nome: string;
}

interface AlocacaoListItem {
    id: number;
    professorNome: string;
    escolaNome: string;
    modalidadeNome: string;
    etapaNome: string;
    turmaNome: string;
    turmaIdentificador: string;
    disciplinaNome: string;
    turno: string;
}

interface Disponibilidade {
    turmaId: number;
    turmaNome: string;
    anoLetivoId: number;
    anoReferencia: number;
    turno: string;
    escolaId: number;
    escolaNome: string;
    modalidadeEnsinoId: number;
    modalidadeNome: string;
    etapaEnsinoId: number;
    etapaNome: string;
    turmaIdentificador: string;
    disciplinas: { disciplinaId: number; disciplinaNome: string; alocacaoId: number | null; professorAtualNome: string | null }[];
}

interface GradeItem {
    turmaId: number;
    turmaNome: string;
    turmaIdentificador: string;
    escolaNome: string;
    modalidadeNome: string;
    etapaNome: string;
    turno: string;
    disciplinaId: number;
    disciplinaNome: string;
    substituirAlocacaoId: number | null;
    professorAtualNome: string | null;
}

interface SelectOption {
    value: string;
    label: string;
}

const uniqueOptions = (items: Disponibilidade[], value: (item: Disponibilidade) => number, label: (item: Disponibilidade) => string): SelectOption[] => {
    const map = new Map<string, string>();
    items.forEach(item => map.set(value(item).toString(), label(item)));
    return [...map.entries()]
        .map(([optionValue, optionLabel]) => ({ value: optionValue, label: optionLabel }))
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'));
};

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
    const [escolaId, setEscolaId] = useState('');
    const [modalidadeId, setModalidadeId] = useState('');
    const [etapaId, setEtapaId] = useState('');
    const [turmaIds, setTurmaIds] = useState<string[]>([]);
    const [disciplinaIds, setDisciplinaIds] = useState<string[]>([]);
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
        && item.turmaIdentificador.toLowerCase().includes(applied.turma));

    const resetForm = () => {
        setProfessorId('');
        setDisponiveis([]);
        setEscolaId('');
        setModalidadeId('');
        setEtapaId('');
        setTurmaIds([]);
        setDisciplinaIds([]);
        setGrade([]);
    };

    const handleAlocar = () => {
        setSuccess(null);
        setError(null);
        resetForm();
        setView('form');
    };

    const handleCancelar = async () => {
        if (professorId || grade.length > 0) {
            const confirmed = await confirm({
                title: 'Cancelar alocação',
                variant: 'warning',
                confirmLabel: 'Cancelar alocação',
                message: (
                    <>
                        <p>Deseja cancelar a alocação de professor?</p>
                        <p>{grade.length > 0
                            ? 'As alocações da lista de conferência serão descartadas.'
                            : 'Os dados informados serão descartados.'}</p>
                    </>
                ),
            });
            if (!confirmed) return;
        }

        setError(null);
        resetForm();
        setView('list');
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

    const handleProfessorChange = async (id: string) => {
        setProfessorId(id);
        setEscolaId('');
        setModalidadeId('');
        setEtapaId('');
        setTurmaIds([]);
        setDisciplinaIds([]);
        setDisponiveis([]);
        setSuccess(null);
        setError(null);
        if (!id) return;

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

    const handleEscolaChange = (value: string) => {
        setEscolaId(value);
        setModalidadeId('');
        setEtapaId('');
        setTurmaIds([]);
        setDisciplinaIds([]);
    };

    const handleModalidadeChange = (value: string) => {
        setModalidadeId(value);
        setEtapaId('');
        setTurmaIds([]);
        setDisciplinaIds([]);
    };

    const handleEtapaChange = (value: string) => {
        setEtapaId(value);
        setTurmaIds([]);
        setDisciplinaIds([]);
    };

    const handleTurmasChange = (ids: string[]) => {
        setTurmaIds(ids);
        // Mantém só as disciplinas que ainda são oferecidas em alguma das turmas selecionadas.
        const ofertadas = new Set(
            naEtapa
                .filter(item => ids.includes(item.turmaId.toString()))
                .flatMap(item => item.disciplinas.map(disciplina => disciplina.disciplinaId.toString())));
        setDisciplinaIds(current => current.filter(id => ofertadas.has(id)));
    };

    const professorSelecionado = professores.find(professor => professor.id.toString() === professorId);
    const escolaOptions = uniqueOptions(disponiveis, item => item.escolaId, item => item.escolaNome);
    const noEscola = disponiveis.filter(item => item.escolaId.toString() === escolaId);
    const modalidadeOptions = uniqueOptions(noEscola, item => item.modalidadeEnsinoId, item => item.modalidadeNome);
    const naModalidade = noEscola.filter(item => item.modalidadeEnsinoId.toString() === modalidadeId);
    const etapaOptions = uniqueOptions(naModalidade, item => item.etapaEnsinoId, item => item.etapaNome);
    const naEtapa = naModalidade.filter(item => item.etapaEnsinoId.toString() === etapaId);
    const turmaOptions = uniqueOptions(naEtapa, item => item.turmaId, item => `${item.turmaIdentificador} · ${item.anoReferencia} · ${turnoLabels[item.turno] ?? item.turno}`);
    const turmasSelecionadas = naEtapa.filter(item => turmaIds.includes(item.turmaId.toString()));
    const disciplinaOptions: SelectOption[] = [...new Map(
        turmasSelecionadas.flatMap(item => item.disciplinas).map(disciplina => [disciplina.disciplinaId.toString(), disciplina.disciplinaNome] as const),
    ).entries()]
        .map(([value, label]) => ({ value, label }))
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR'));

    const addToGrade = () => {
        const novos: GradeItem[] = [];
        let ignorados = 0;

        turmasSelecionadas.forEach(turma => {
            disciplinaIds.forEach(disciplinaId => {
                const disciplina = turma.disciplinas.find(item => item.disciplinaId.toString() === disciplinaId);
                const jaNaLista = grade.some(item => item.turmaId === turma.turmaId && item.disciplinaId === Number(disciplinaId));
                const jaDoProfessor = disciplina?.professorAtualNome !== null && disciplina?.professorAtualNome === professorSelecionado?.nome;

                if (!disciplina || jaNaLista || jaDoProfessor) {
                    ignorados += 1;
                    return;
                }

                novos.push({
                    turmaId: turma.turmaId,
                    turmaNome: turma.turmaNome,
                    turmaIdentificador: turma.turmaIdentificador,
                    escolaNome: turma.escolaNome,
                    modalidadeNome: turma.modalidadeNome,
                    etapaNome: turma.etapaNome,
                    turno: turma.turno,
                    disciplinaId: disciplina.disciplinaId,
                    disciplinaNome: disciplina.disciplinaNome,
                    substituirAlocacaoId: disciplina.alocacaoId,
                    professorAtualNome: disciplina.professorAtualNome,
                });
            });
        });

        setGrade(current => [...current, ...novos]);
        setDisciplinaIds([]);
        setError(ignorados > 0
            ? `${ignorados} combinação(ões) de turma e disciplina não foram adicionadas: já estão na lista, já são deste professor ou a disciplina não é ofertada na turma.`
            : null);
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
            resetForm();
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
                                            <td>{item.turmaIdentificador}</td>
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
                </div>
                <FeedbackMessage message={error} />
                <div className="cadastro-form escola-form">
                    <div className="form-field">
                        <label htmlFor="alocacao-professor">Professor</label>
                        <select
                            id="alocacao-professor"
                            value={professorId}
                            onChange={event => void handleProfessorChange(event.target.value)}
                            disabled={saving || grade.length > 0}
                            title={grade.length > 0 ? 'Remova as alocações da lista de conferência para trocar o professor.' : undefined}
                        >
                            <option value="">Selecione um professor</option>
                            {professores.map(professor => <option key={professor.id} value={professor.id}>{professor.nome}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="alocacao-escola">Escola</label>
                        <select id="alocacao-escola" value={escolaId} onChange={event => handleEscolaChange(event.target.value)} disabled={!professorId || loading || saving}>
                            <option value="">Selecione uma escola</option>
                            {escolaOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="alocacao-modalidade">Modalidade</label>
                        <select id="alocacao-modalidade" value={modalidadeId} onChange={event => handleModalidadeChange(event.target.value)} disabled={!escolaId || saving}>
                            <option value="">Selecione uma modalidade</option>
                            {modalidadeOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="alocacao-etapa">Etapa</label>
                        <select id="alocacao-etapa" value={etapaId} onChange={event => handleEtapaChange(event.target.value)} disabled={!modalidadeId || saving}>
                            <option value="">Selecione uma etapa</option>
                            {etapaOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="alocacao-turma">Turma</label>
                        <MultiSelectDropdown
                            id="alocacao-turma"
                            options={turmaOptions}
                            selected={turmaIds}
                            onChange={handleTurmasChange}
                            placeholder="Selecione as turmas"
                            searchPlaceholder="Pesquisar turmas..."
                            emptyMessage="Nenhuma turma disponível"
                            disabled={!etapaId || saving}
                        />
                    </div>

                    <div className="form-field">
                        <label htmlFor="alocacao-disciplina">Disciplina</label>
                        <MultiSelectDropdown
                            id="alocacao-disciplina"
                            options={disciplinaOptions}
                            selected={disciplinaIds}
                            onChange={setDisciplinaIds}
                            placeholder="Selecione as disciplinas"
                            searchPlaceholder="Pesquisar disciplinas..."
                            emptyMessage="Nenhuma disciplina disponível"
                            disabled={turmaIds.length === 0 || saving}
                        />
                    </div>

                    {professorId && !loading && disponiveis.length === 0 && (
                        <EmptyState emptyMessage="Nenhuma turma disponível para as habilitações e escolas cadastradas deste professor." />
                    )}

                    <div>
                        <button type="button" onClick={addToGrade} disabled={disciplinaIds.length === 0 || saving}>Adicionar Alocação</button>
                    </div>
                </div>
            </section>

            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Tabela de conferência</h2>
                        <p>Revise os vínculos antes de confirmar a alocação.</p>
                    </div>
                    <div className="form-actions">
                        <button className="btn btn-secondary" type="button" onClick={() => void handleCancelar()} disabled={saving}>Cancelar</button>
                        {can('alocacao-professor.criar') && <button className="btn btn-primary" type="button" onClick={() => void confirmAllocation()} disabled={saving || !professorId || grade.length === 0}>Confirmar Alocação</button>}
                    </div>
                </div>
                {grade.length === 0 ? <EmptyState emptyMessage="Nenhuma alocação adicionada à lista." /> : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr><th>Professor</th><th>Escola</th><th>Modalidade</th><th>Etapa</th><th>Turma</th><th>Disciplina</th><th>Turno</th><th>Ação</th></tr>
                            </thead>
                            <tbody>
                                {grade.map(item => (
                                    <tr key={`${item.turmaId}-${item.disciplinaId}`}>
                                        <td>{professorSelecionado?.nome}</td>
                                        <td>{item.escolaNome}</td>
                                        <td>{item.modalidadeNome}</td>
                                        <td>{item.etapaNome}</td>
                                        <td>{item.turmaIdentificador}</td>
                                        <td>
                                            {item.disciplinaNome}
                                            {item.professorAtualNome && <small> — substitui {item.professorAtualNome}</small>}
                                        </td>
                                        <td>{turnoLabels[item.turno] ?? item.turno}</td>
                                        <td>
                                            <button className="table-action-button danger" type="button" onClick={() => removeFromGrade(item)} disabled={saving}>
                                                Remover
                                            </button>
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

export default ProfessorAlocacoesPage;
