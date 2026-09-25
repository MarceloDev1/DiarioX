import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

type View = 'list' | 'form';

interface Turma {
    id: number;
    anoLetivoId: number;
    anoReferencia: number;
    escolaId: number;
    escolaNome: string;
    modalidadeEnsinoId: number;
    modalidadeEnsinoNome: string;
    etapaEnsinoId: number;
    etapaEnsinoNome: string;
    nomeIdentificador: string;
    nomeCompleto: string;
    turno: string;
    turnoDescricao: string;
    vagasOfertadas: number;
    status: string;
}

interface AnoLetivoOption {
    id: number;
    anoReferencia: number;
}

interface EscolaOption {
    id: number;
    nome: string;
    status: string;
}

interface ModalidadeOption {
    id: number;
    nome: string;
    status: string;
}

interface EtapaOption {
    id: number;
    modalidadeEnsinoId: number;
    nome: string;
}

interface TurmaFormState {
    anoLetivoId: string;
    escolaId: string;
    modalidadeEnsinoId: string;
    etapaEnsinoId: string;
    nomeIdentificador: string;
    turno: string;
    vagasOfertadas: string;
}

interface TurmaFieldErrors {
    anoLetivoId: boolean;
    escolaId: boolean;
    modalidadeEnsinoId: boolean;
    etapaEnsinoId: boolean;
    nomeIdentificador: boolean;
    turno: boolean;
    vagasOfertadas: boolean;
}

const turnos = [
    { value: 'MANHA', label: 'Manhã' },
    { value: 'TARDE', label: 'Tarde' },
    { value: 'NOITE', label: 'Noite' },
    { value: 'INTEGRAL', label: 'Integral' },
];

const emptyForm: TurmaFormState = {
    anoLetivoId: '',
    escolaId: '',
    modalidadeEnsinoId: '',
    etapaEnsinoId: '',
    nomeIdentificador: '',
    turno: '',
    vagasOfertadas: '',
};

const emptyFieldErrors: TurmaFieldErrors = {
    anoLetivoId: false,
    escolaId: false,
    modalidadeEnsinoId: false,
    etapaEnsinoId: false,
    nomeIdentificador: false,
    turno: false,
    vagasOfertadas: false,
};

const emptyFilters = {
    anoLetivoId: '',
    escolaId: '',
    modalidadeEnsinoId: '',
    etapaEnsinoId: '',
    turno: '',
    status: '',
};

function TurmasPage() {
    const { items: turmas, isLoading, isSaving, error, load, save, remove } = useCrudData<Turma>('/api/turmas');

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<TurmaFormState>(emptyForm);
    const [fieldErrors, setFieldErrors] = useState<TurmaFieldErrors>(emptyFieldErrors);
    const [localError, setLocalError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [statusUpdatingId, setStatusUpdatingId] = useState<number | null>(null);

    const [anosLetivos, setAnosLetivos] = useState<AnoLetivoOption[]>([]);
    const [escolas, setEscolas] = useState<EscolaOption[]>([]);
    const [modalidades, setModalidades] = useState<ModalidadeOption[]>([]);
    const [etapas, setEtapas] = useState<EtapaOption[]>([]);

    const [filters, setFilters] = useState(emptyFilters);
    const [appliedFilters, setAppliedFilters] = useState(emptyFilters);

    const etapasFiltradas = useMemo(() => {
        if (!form.modalidadeEnsinoId) return [];
        const modalidadeId = parseInt(form.modalidadeEnsinoId);
        return etapas.filter(etapa => etapa.modalidadeEnsinoId === modalidadeId);
    }, [etapas, form.modalidadeEnsinoId]);

    const etapasFiltroOptions = useMemo(() => {
        if (!filters.modalidadeEnsinoId) return etapas;
        const modalidadeId = parseInt(filters.modalidadeEnsinoId);
        return etapas.filter(etapa => etapa.modalidadeEnsinoId === modalidadeId);
    }, [etapas, filters.modalidadeEnsinoId]);

    useEffect(() => {
        let cancelled = false;

        async function loadOptions() {
            try {
                const [anosRes, escolasRes, modalidadesRes, etapasRes] = await Promise.all([
                    fetch('/api/anosletivos'),
                    fetch('/api/escolas'),
                    fetch('/api/modalidadesensino'),
                    fetch('/api/etapasensino'),
                ]);
                if (cancelled) return;

                if (anosRes.ok) {
                    const anos = (await anosRes.json()) as AnoLetivoOption[];
                    setAnosLetivos(anos);
                    setForm(current => {
                        if (current.anoLetivoId) return current;

                        const anoAtual = new Date().getFullYear();
                        const defaultAno = anos.find(a => a.anoReferencia === anoAtual) ?? anos[0];
                        if (!defaultAno) return current;

                        return {
                            ...current,
                            anoLetivoId: String(defaultAno.id),
                        };
                    });
                }

                if (escolasRes.ok) {
                    const escolasData = (await escolasRes.json()) as EscolaOption[];
                    setEscolas(escolasData.filter(escola => escola.status === 'ATIVO'));
                }

                if (modalidadesRes.ok) {
                    const modalidadesData = (await modalidadesRes.json()) as ModalidadeOption[];
                    setModalidades(modalidadesData.filter(modalidade => modalidade.status === 'ATIVO'));
                }

                if (etapasRes.ok) {
                    setEtapas((await etapasRes.json()) as EtapaOption[]);
                }
            } catch {
                if (!cancelled) setLocalError('Falha ao carregar opções do formulário.');
            }
        }

        void load();
        void loadOptions();
        return () => { cancelled = true; };
    }, []);

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;

        setForm(current => {
            const next = { ...current, [name]: value };

            if (name === 'modalidadeEnsinoId') {
                const modalidadeId = value ? parseInt(value) : 0;
                const etapaValida = current.etapaEnsinoId
                    ? etapas.some(etapa => etapa.id === parseInt(current.etapaEnsinoId) && etapa.modalidadeEnsinoId === modalidadeId)
                    : true;

                if (!etapaValida) {
                    next.etapaEnsinoId = '';
                }
            }

            return next;
        });

        setFieldErrors(current => ({ ...current, [name]: false }));
        setLocalError(null);
    };

    const handleFilterChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;

        setFilters(current => {
            const next = { ...current, [name]: value };

            if (name === 'modalidadeEnsinoId') {
                const modalidadeId = value ? parseInt(value) : 0;
                const etapaValida = current.etapaEnsinoId
                    ? etapas.some(etapa => etapa.id === parseInt(current.etapaEnsinoId) && etapa.modalidadeEnsinoId === modalidadeId)
                    : true;

                if (!etapaValida) {
                    next.etapaEnsinoId = '';
                }
            }

            return next;
        });
    };

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedFilters(filters);
    };

    const handleLimparFiltros = () => {
        setFilters(emptyFilters);
        setAppliedFilters(emptyFilters);
    };

    const validateForm = () => {
        const nextErrors: TurmaFieldErrors = {
            anoLetivoId: !form.anoLetivoId,
            escolaId: !form.escolaId,
            modalidadeEnsinoId: !form.modalidadeEnsinoId,
            etapaEnsinoId: !form.etapaEnsinoId,
            nomeIdentificador: !form.nomeIdentificador.trim(),
            turno: !form.turno,
            vagasOfertadas: !form.vagasOfertadas || parseInt(form.vagasOfertadas) <= 0,
        };

        setFieldErrors(nextErrors);

        if (Object.values(nextErrors).some(Boolean)) {
            setLocalError('Por favor, preencha todos os campos obrigatórios.');
            return false;
        }

        return true;
    };

    const buildPayload = () => {
        return {
            anoLetivoId: parseInt(form.anoLetivoId),
            escolaId: parseInt(form.escolaId),
            modalidadeEnsinoId: parseInt(form.modalidadeEnsinoId),
            etapaEnsinoId: parseInt(form.etapaEnsinoId),
            nomeIdentificador: form.nomeIdentificador.trim(),
            turno: form.turno,
            vagasOfertadas: parseInt(form.vagasOfertadas),
        };
    };

    const filteredTurmas = turmas.filter(turma => {
        const matchAno = !appliedFilters.anoLetivoId || turma.anoLetivoId === parseInt(appliedFilters.anoLetivoId);
        const matchEscola = !appliedFilters.escolaId || turma.escolaId === parseInt(appliedFilters.escolaId);
        const matchModalidade = !appliedFilters.modalidadeEnsinoId || turma.modalidadeEnsinoId === parseInt(appliedFilters.modalidadeEnsinoId);
        const matchEtapa = !appliedFilters.etapaEnsinoId || turma.etapaEnsinoId === parseInt(appliedFilters.etapaEnsinoId);
        const matchTurno = !appliedFilters.turno || turma.turno === appliedFilters.turno;
        const matchStatus = !appliedFilters.status || turma.status === appliedFilters.status;
        return matchAno && matchEscola && matchModalidade && matchEtapa && matchTurno && matchStatus;
    });

    const handleSave = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setSuccessMessage(null);

        if (!validateForm()) return;

        const saved = await save(editingId, buildPayload());
        if (saved) {
            setLocalError(null);
            setFieldErrors(emptyFieldErrors);
            setSuccessMessage(editingId !== null ? 'Turma atualizada com sucesso!' : 'Turma cadastrada com sucesso!');
            setEditingId(null);
            setForm(emptyForm);
            setView('list');
            await load();
        }
    };

    const handleSaveAndAddAnother = async () => {
        setSuccessMessage(null);

        if (!validateForm()) return;

        const saved = await save(null, buildPayload());
        if (saved) {
            setLocalError(null);
            setFieldErrors(emptyFieldErrors);
            setSuccessMessage('Turma cadastrada com sucesso!');

            setForm(current => ({
                ...current,
                nomeIdentificador: '',
                vagasOfertadas: '',
            }));

            await load();
        }
    };

    const handleNova = () => {
        setSuccessMessage(null);
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
        setEditingId(null);
        setForm(current => ({
            ...emptyForm,
            anoLetivoId: current.anoLetivoId,
        }));
        setView('form');
    };

    const handleEdit = (turma: Turma) => {
        setSuccessMessage(null);
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
        setEditingId(turma.id);
        setForm({
            anoLetivoId: String(turma.anoLetivoId),
            escolaId: String(turma.escolaId),
            modalidadeEnsinoId: String(turma.modalidadeEnsinoId),
            etapaEnsinoId: String(turma.etapaEnsinoId),
            nomeIdentificador: turma.nomeIdentificador,
            turno: turma.turno,
            vagasOfertadas: String(turma.vagasOfertadas),
        });
        setView('form');
    };

    const handleCancel = () => {
        setSuccessMessage(null);
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
        setEditingId(null);
        setForm(current => ({
            ...emptyForm,
            anoLetivoId: current.anoLetivoId,
        }));
        setView('list');
    };

    const handleClear = () => {
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
        setForm(current => ({
            ...emptyForm,
            anoLetivoId: current.anoLetivoId,
            escolaId: current.escolaId,
            modalidadeEnsinoId: current.modalidadeEnsinoId,
            etapaEnsinoId: current.etapaEnsinoId,
            turno: current.turno,
        }));
    };

    const handleDelete = async (id: number) => {
        if (!window.confirm('Tem certeza que deseja excluir esta turma?')) return;

        setSuccessMessage(null);
        await remove(id);
    };

    const handleToggleStatus = async (turma: Turma) => {
        const inativar = turma.status === 'ATIVO';
        const confirmMessage = inativar
            ? 'Tem certeza que deseja inativar esta turma? Turmas inativas não recebem novos alunos nem alocações de professores.'
            : 'Tem certeza que deseja ativar esta turma?';
        if (!window.confirm(confirmMessage)) return;

        setSuccessMessage(null);
        setLocalError(null);
        setStatusUpdatingId(turma.id);
        try {
            const response = await fetch(`/api/turmas/${turma.id}/status`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status: inativar ? 'INATIVO' : 'ATIVO' }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccessMessage(inativar ? 'Turma inativada com sucesso!' : 'Turma ativada com sucesso!');
            await load();
        } catch (e) {
            setLocalError(e instanceof Error ? e.message : 'Falha ao alterar o status da turma.');
        } finally {
            setStatusUpdatingId(null);
        }
    };

    const mergedError = localError ?? error;

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId !== null ? 'Editar Turma' : 'Cadastro de Turma'}</h2>
                            <p>{editingId !== null ? 'Altere os dados e salve para atualizar.' : 'Preencha os campos obrigatórios para cadastrar uma nova turma.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={successMessage} type="success" />
                    <FeedbackMessage message={mergedError} />

                    <form className="cadastro-form escola-form" onSubmit={handleSave}>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="turma-ano-letivo">Ano Letivo</label>
                                <select
                                    id="turma-ano-letivo"
                                    name="anoLetivoId"
                                    value={form.anoLetivoId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.anoLetivoId ? 'input-error' : ''}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {anosLetivos.map(ano => (
                                        <option key={ano.id} value={String(ano.id)}>
                                            {ano.anoReferencia}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-escola">Escola</label>
                                <select
                                    id="turma-escola"
                                    name="escolaId"
                                    value={form.escolaId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.escolaId ? 'input-error' : ''}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {escolas.map(escola => (
                                        <option key={escola.id} value={String(escola.id)}>
                                            {escola.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-modalidade">Modalidade de Ensino</label>
                                <select
                                    id="turma-modalidade"
                                    name="modalidadeEnsinoId"
                                    value={form.modalidadeEnsinoId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.modalidadeEnsinoId ? 'input-error' : ''}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {modalidades.map(modalidade => (
                                        <option key={modalidade.id} value={String(modalidade.id)}>
                                            {modalidade.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-etapa">Ano de Ensino / Etapa</label>
                                <select
                                    id="turma-etapa"
                                    name="etapaEnsinoId"
                                    value={form.etapaEnsinoId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.etapaEnsinoId ? 'input-error' : ''}
                                    required
                                    disabled={!form.modalidadeEnsinoId}
                                >
                                    <option value="">Selecione...</option>
                                    {etapasFiltradas.map(etapa => (
                                        <option key={etapa.id} value={String(etapa.id)}>
                                            {etapa.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-nome-identificador">Nome/Identificador da Turma</label>
                                <input
                                    id="turma-nome-identificador"
                                    name="nomeIdentificador"
                                    value={form.nomeIdentificador}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Ex: Turma A"
                                    className={fieldErrors.nomeIdentificador ? 'input-error' : ''}
                                    required
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-turno">Turno</label>
                                <select
                                    id="turma-turno"
                                    name="turno"
                                    value={form.turno}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.turno ? 'input-error' : ''}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {turnos.map(turno => (
                                        <option key={turno.value} value={turno.value}>
                                            {turno.label}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="turma-vagas">Vagas Ofertadas</label>
                                <input
                                    id="turma-vagas"
                                    name="vagasOfertadas"
                                    value={form.vagasOfertadas}
                                    onChange={handleFieldChange}
                                    type="number"
                                    min={1}
                                    step={1}
                                    placeholder="Ex: 35"
                                    className={fieldErrors.vagasOfertadas ? 'input-error' : ''}
                                    required
                                />
                            </div>
                        </div>

                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Turma' : 'Salvar'}
                            </button>
                            {editingId === null && (
                                <button type="button" onClick={handleSaveAndAddAnother} disabled={isSaving}>
                                    Salvar e Adicionar Outra
                                </button>
                            )}
                            <button type="button" onClick={handleClear}>
                                Limpar
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        );
    }

    return (
        <div className="school-page">
            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Turmas</h2>
                        <p>Consulte as turmas cadastradas e adicione novas turmas.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNova}>
                        + Nova Turma
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-ano">Ano Letivo</label>
                        <select
                            id="filtro-turma-ano"
                            name="anoLetivoId"
                            value={filters.anoLetivoId}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todos</option>
                            {anosLetivos.map(ano => (
                                <option key={ano.id} value={String(ano.id)}>
                                    {ano.anoReferencia}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-escola">Escola</label>
                        <select
                            id="filtro-turma-escola"
                            name="escolaId"
                            value={filters.escolaId}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todas</option>
                            {escolas.map(escola => (
                                <option key={escola.id} value={String(escola.id)}>
                                    {escola.nome}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-modalidade">Modalidade</label>
                        <select
                            id="filtro-turma-modalidade"
                            name="modalidadeEnsinoId"
                            value={filters.modalidadeEnsinoId}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todas</option>
                            {modalidades.map(modalidade => (
                                <option key={modalidade.id} value={String(modalidade.id)}>
                                    {modalidade.nome}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-etapa">Etapa</label>
                        <select
                            id="filtro-turma-etapa"
                            name="etapaEnsinoId"
                            value={filters.etapaEnsinoId}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todas</option>
                            {etapasFiltroOptions.map(etapa => (
                                <option key={etapa.id} value={String(etapa.id)}>
                                    {etapa.nome}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-turno">Turno</label>
                        <select
                            id="filtro-turma-turno"
                            name="turno"
                            value={filters.turno}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todos</option>
                            {turnos.map(turno => (
                                <option key={turno.value} value={turno.value}>
                                    {turno.label}
                                </option>
                            ))}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-turma-status">Status</label>
                        <select
                            id="filtro-turma-status"
                            name="status"
                            value={filters.status}
                            onChange={handleFilterChange}
                            className="filter-input"
                        >
                            <option value="">Todos</option>
                            <option value="ATIVO">Ativo</option>
                            <option value="INATIVO">Inativo</option>
                        </select>
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={successMessage} type="success" />
                <FeedbackMessage message={mergedError} />

                {isLoading || filteredTurmas.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando turmas..."
                        emptyMessage="Nenhuma turma encontrada."
                        emptySubMessage={turmas.length === 0 ? 'Clique em Nova Turma para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nome Completo</th>
                                    <th>Ano Letivo</th>
                                    <th>Escola</th>
                                    <th>Modalidade</th>
                                    <th>Etapa</th>
                                    <th>Turno</th>
                                    <th>Vagas</th>
                                    <th>Status</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredTurmas.map(turma => (
                                    <tr key={turma.id}>
                                        <td>{turma.nomeCompleto}</td>
                                        <td>{turma.anoReferencia}</td>
                                        <td>{turma.escolaNome}</td>
                                        <td>{turma.modalidadeEnsinoNome}</td>
                                        <td>{turma.etapaEnsinoNome}</td>
                                        <td>{turma.turnoDescricao}</td>
                                        <td>{turma.vagasOfertadas}</td>
                                        <td>
                                            <span className={`status-pill ${turma.status === 'ATIVO' ? 'status-active' : 'status-inactive'}`}>
                                                {turma.status}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="action-group vertical">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(turma)}>Editar</button>
                                                <button
                                                    type="button"
                                                    className="table-action-button"
                                                    onClick={() => handleToggleStatus(turma)}
                                                    disabled={statusUpdatingId === turma.id}
                                                >
                                                    {turma.status === 'ATIVO' ? 'Inativar' : 'Ativar'}
                                                </button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(turma.id)}>Excluir</button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </div>
    );
}

export default TurmasPage;
