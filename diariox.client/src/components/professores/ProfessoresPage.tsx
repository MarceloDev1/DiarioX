import { useCallback, useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { useCrudData } from '../../hooks/useCrudData';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import StatusPill from '../ui/StatusPill';
import ConfirmDialog from '../ui/ConfirmDialog';

type View = 'list' | 'form';
type FormTab = 'pessoais' | 'contratuais' | 'habilitacao';

interface Disciplina {
    id: number;
    nome: string;
}

interface EscolaVinculada {
    id: number;
    nome: string;
}

interface Professor {
    id: number;
    nome: string;
    cpf: string;
    dataNascimento: string;
    email: string;
    telefone: string;
    matricula: string | null;
    dataAdmissao: string | null;
    situacao: string;
    escolas: EscolaVinculada[];
    disciplinas: Disciplina[];
    usuarioId: number | null;
    usuarioEmail: string | null;
    createdAt: string;
    updatedAt: string | null;
}

interface EscolaOption {
    id: number;
    nome: string;
    status: string;
}

interface DisciplinaOption {
    id: number;
    nome: string;
}

interface ProfessorFormState {
    nome: string;
    cpf: string;
    dataNascimento: string;
    email: string;
    telefone: string;
    matricula: string;
    dataAdmissao: string;
    situacao: string;
    escolaIds: string[];
    disciplinaIds: string[];
}

interface ProfessorFieldErrors {
    nome: boolean;
    cpf: boolean;
    dataNascimento: boolean;
    email: boolean;
    telefone: boolean;
}

const formTabs: { id: FormTab; label: string }[] = [
    { id: 'pessoais', label: ' Dados Pessoais' },
    { id: 'contratuais', label: ' Dados Contratuais' },
    { id: 'habilitacao', label: ' Habilitação Pedagógica' },
];

const situacoes = [
    { value: 'ATIVO', label: 'Ativo' },
    { value: 'INATIVO', label: 'Inativo' },
    { value: 'AFASTADO', label: 'Afastado' },
    { value: 'LICENCIADO', label: 'Licenciado' },
];

const emptyForm: ProfessorFormState = {
    nome: '',
    cpf: '',
    dataNascimento: '',
    email: '',
    telefone: '',
    matricula: '',
    dataAdmissao: '',
    situacao: 'ATIVO',
    escolaIds: [],
    disciplinaIds: [],
};

const emptyFieldErrors: ProfessorFieldErrors = {
    nome: false,
    cpf: false,
    dataNascimento: false,
    email: false,
    telefone: false,
};

function ProfessoresPage() {
    const { confirm, confirmDialog } = useConfirm();
    const { items: professores, isLoading, isSaving, error, load, save, remove } = useCrudData<Professor>('/api/professores');

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<ProfessorFormState>(emptyForm);
    const [activeTab, setActiveTab] = useState<FormTab>('pessoais');
    const [fieldErrors, setFieldErrors] = useState<ProfessorFieldErrors>(emptyFieldErrors);
    const [localError, setLocalError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [situacaoUpdatingId, setSituacaoUpdatingId] = useState<number | null>(null);
    const [professorToDelete, setProfessorToDelete] = useState<Professor | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    const [escolas, setEscolas] = useState<EscolaOption[]>([]);
    const [disciplinas, setDisciplinas] = useState<DisciplinaOption[]>([]);

    useEffect(() => {
        let cancelled = false;

        async function loadOptions() {
            try {
                const [escolasRes, disciplinasRes] = await Promise.all([
                    apiFetch('/api/escolas'),
                    apiFetch('/api/disciplinas'),
                ]);
                if (cancelled) return;

                if (escolasRes.ok) {
                    const escolasData = (await escolasRes.json()) as EscolaOption[];
                    setEscolas(escolasData.filter(escola => escola.status === 'ATIVO'));
                }

                if (disciplinasRes.ok) {
                    const disciplinasData = (await disciplinasRes.json()) as DisciplinaOption[];
                    setDisciplinas(disciplinasData);
                }
            } catch {
                if (!cancelled) setLocalError('Falha ao carregar opções do formulário.');
            }
        }

        void load();
        void loadOptions();
        return () => { cancelled = true; };
    }, []);

    const formatCpf = (cpf: string): string => {
        const numbers = cpf.replace(/\D/g, '');
        if (numbers.length !== 11) return numbers;
        return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9)}`;
    };

    const formatTelefone = (telefone: string): string => {
        const numbers = telefone.replace(/\D/g, '');
        if (numbers.length !== 11) return numbers;
        return `(${numbers.slice(0, 2)}) ${numbers.slice(2, 7)}-${numbers.slice(7)}`;
    };

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value, type } = event.currentTarget;

        if (type === 'checkbox') {
            const checkbox = event.currentTarget as HTMLInputElement;
            const field = name as 'escolaIds' | 'disciplinaIds';
            setForm(current => {
                const ids = checkbox.checked
                    ? [...current[field], value]
                    : current[field].filter(id => id !== value);
                return { ...current, [field]: ids };
            });
        } else {
            setForm(current => ({ ...current, [name]: value }));
        }

        setFieldErrors(current => ({ ...current, [name]: false }));
        setLocalError(null);
    };

    const validateForm = (): boolean => {
        const cpfNumbers = form.cpf.replace(/\D/g, '');
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        const nextErrors: ProfessorFieldErrors = {
            nome: !form.nome.trim(),
            cpf: cpfNumbers.length !== 11,
            dataNascimento: !form.dataNascimento,
            email: !emailRegex.test(form.email),
            telefone: !form.telefone.trim(),
        };

        setFieldErrors(nextErrors);
        const hasErrors = Object.values(nextErrors).some(error => error);

        if (hasErrors) {
            setLocalError('Por favor, preencha todos os campos obrigatórios.');
            setActiveTab('pessoais');
            return false;
        }

        return true;
    };

    const handleFormSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!validateForm()) return;

        const body = {
            nome: form.nome,
            cpf: form.cpf.replace(/\D/g, ''),
            dataNascimento: form.dataNascimento,
            email: form.email,
            telefone: form.telefone,
            matricula: form.matricula.trim() || null,
            dataAdmissao: form.dataAdmissao || null,
            situacao: form.situacao,
            escolaIds: form.escolaIds.map(id => parseInt(id)),
            disciplinaIds: form.disciplinaIds.map(id => parseInt(id)),
        };

        const result = await save(editingId, body);
        if (result) {
            setSuccessMessage(editingId ? 'Professor atualizado com sucesso!' : 'Professor cadastrado com sucesso!');
            setView('list');
            setForm(emptyForm);
            setEditingId(null);
            setTimeout(() => setSuccessMessage(null), 3000);
        }
    };

    const handleEditClick = (professor: Professor) => {
        setEditingId(professor.id);
        setForm({
            nome: professor.nome,
            cpf: professor.cpf,
            dataNascimento: professor.dataNascimento.slice(0, 10),
            email: professor.email,
            telefone: professor.telefone,
            matricula: professor.matricula ?? '',
            dataAdmissao: professor.dataAdmissao?.slice(0, 10) ?? '',
            situacao: professor.situacao,
            escolaIds: professor.escolas.map(e => e.id.toString()),
            disciplinaIds: professor.disciplinas.map(d => d.id.toString()),
        });
        setActiveTab('pessoais');
        setView('form');
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
    };

    const handleConfirmDelete = async () => {
        if (!professorToDelete) return;

        setIsDeleting(true);
        const success = await remove(professorToDelete.id);
        setIsDeleting(false);
        setProfessorToDelete(null);

        if (success) {
            setSuccessMessage('Professor removido com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
        }
    };

    const handleCancelDelete = useCallback(() => setProfessorToDelete(null), []);

    const handleToggleSituacao = async (professor: Professor) => {
        const inativar = professor.situacao !== 'INATIVO';
        const confirmed = await confirm(inativar
            ? {
                title: 'Inativar professor',
                variant: 'warning',
                confirmLabel: 'Inativar',
                message: (
                    <>
                        <p>
                            Deseja inativar <strong>{professor.nome}</strong>
                            {professor.matricula && <> (matrícula {professor.matricula})</>}?
                        </p>
                        <p>O cadastro, as habilitações e as escolas vinculadas são mantidos, e você pode reativá-lo a qualquer momento.</p>
                    </>
                ),
            }
            : {
                title: 'Ativar professor',
                variant: 'success',
                confirmLabel: 'Ativar',
                message: (
                    <p>
                        Deseja ativar <strong>{professor.nome}</strong>
                        {professor.matricula && <> (matrícula {professor.matricula})</>}?
                    </p>
                ),
            });
        if (!confirmed) return;

        setSuccessMessage(null);
        setLocalError(null);
        setSituacaoUpdatingId(professor.id);
        try {
            const response = await apiFetch(`/api/professores/${professor.id}/situacao`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ situacao: inativar ? 'INATIVO' : 'ATIVO' }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccessMessage(inativar ? 'Professor inativado com sucesso!' : 'Professor ativado com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
            await load();
        } catch (e) {
            setLocalError(e instanceof Error ? e.message : 'Falha ao alterar a situação do professor.');
        } finally {
            setSituacaoUpdatingId(null);
        }
    };

    const handleNewProfessor = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFieldErrors(emptyFieldErrors);
        setLocalError(null);
        setActiveTab('pessoais');
        setView('form');
    };

    const handleBackToList = () => {
        setView('list');
        setForm(emptyForm);
        setEditingId(null);
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1> Professores</h1>
            </div>

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={localError} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {view === 'list' ? (
                <div className="list-view">
                    <div className="list-header">
                        <button className="btn btn-primary" onClick={handleNewProfessor}>
                            ➕ Novo Professor
                        </button>
                    </div>

                    {isLoading ? (
                        <div className="loading">Carregando professores...</div>
                    ) : professores.length === 0 ? (
                        <EmptyState emptyMessage="Nenhum professor cadastrado. Clique em 'Novo Professor' para começar." />
                    ) : (
                        <div className="table-container">
                            <table className="data-table">
                                <thead>
                                    <tr>
                                        <th>Nome</th>
                                        <th>CPF</th>
                                        <th>Matrícula</th>
                                        <th>Email</th>
                                        <th>Disciplinas</th>
                                        <th>Situação</th>
                                        <th>Ações</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {professores.map(professor => (
                                        <tr key={professor.id}>
                                            <td>{professor.nome}</td>
                                            <td className="nowrap-cell">{professor.cpf}</td>
                                            <td>{professor.matricula}</td>
                                            <td>{professor.email}</td>
                                            <td>
                                                <small>
                                                    {professor.disciplinas.map(d => d.nome).join(', ')}
                                                </small>
                                            </td>
                                            <td>
                                                <StatusPill status={professor.situacao as 'ATIVO' | 'INATIVO' | 'BLOQUEADO'} />
                                            </td>
                                            <td>
                                                <div className="action-group vertical">
                                                    <button type="button" className="table-action-button" onClick={() => handleEditClick(professor)}>Editar</button>
                                                    <button
                                                        type="button"
                                                        className="table-action-button"
                                                        onClick={() => handleToggleSituacao(professor)}
                                                        disabled={situacaoUpdatingId === professor.id}
                                                    >
                                                        {professor.situacao === 'INATIVO' ? 'Ativar' : 'Inativar'}
                                                    </button>
                                                    <button type="button" className="table-action-button danger" onClick={() => setProfessorToDelete(professor)}>Excluir</button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            ) : (
                <div className="form-view">
                    <div className="form-header">
                        <h2>{editingId ? 'Editar Professor' : 'Novo Professor'}</h2>
                        <button className="btn btn-secondary" onClick={handleBackToList}>
                            ← Voltar
                        </button>
                    </div>

                    <form onSubmit={handleFormSubmit} className="form-grid">
                        <div className="form-tabs" role="tablist">
                            {formTabs.map(tab => {
                                const hasErrors = tab.id === 'pessoais' && Object.values(fieldErrors).some(Boolean);
                                return (
                                    <button
                                        key={tab.id}
                                        type="button"
                                        role="tab"
                                        id={`professor-tab-${tab.id}`}
                                        aria-selected={activeTab === tab.id}
                                        aria-controls={`professor-panel-${tab.id}`}
                                        className={`form-tab${activeTab === tab.id ? ' active' : ''}${hasErrors ? ' has-error' : ''}`}
                                        onClick={() => setActiveTab(tab.id)}
                                    >
                                        {tab.label}
                                    </button>
                                );
                            })}
                        </div>

                        {/* Dados Pessoais */}
                        <fieldset
                            className="form-section"
                            role="tabpanel"
                            id="professor-panel-pessoais"
                            aria-labelledby="professor-tab-pessoais"
                            hidden={activeTab !== 'pessoais'}
                        >

                            <div className="form-group">
                                <label htmlFor="nome">
                                    Nome <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="nome"
                                    name="nome"
                                    value={form.nome}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.nome ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.nome && <span className="field-error">Nome é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="cpf">
                                    CPF <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="cpf"
                                    name="cpf"
                                    value={formatCpf(form.cpf)}
                                    onChange={e => setForm(current => ({ ...current, cpf: e.target.value }))}
                                    placeholder="000.000.000-00"
                                    className={fieldErrors.cpf ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={14}
                                />
                                {fieldErrors.cpf && <span className="field-error">CPF inválido (11 dígitos)</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="dataNascimento">
                                    Data de Nascimento <span className="required">*</span>
                                </label>
                                <input
                                    type="date"
                                    id="dataNascimento"
                                    name="dataNascimento"
                                    value={form.dataNascimento}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.dataNascimento ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.dataNascimento && (
                                    <span className="field-error">Data de nascimento é obrigatória</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="email">
                                    Email <span className="required">*</span>
                                </label>
                                <input
                                    type="email"
                                    id="email"
                                    name="email"
                                    value={form.email}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.email ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.email && (
                                    <span className="field-error">Email inválido (ex: usuario@email.com)</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="telefone">
                                    Telefone <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="telefone"
                                    name="telefone"
                                    value={formatTelefone(form.telefone)}
                                    onChange={e => setForm(current => ({ ...current, telefone: e.target.value }))}
                                    placeholder="(00) 00000-0000"
                                    className={fieldErrors.telefone ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={15}
                                />
                                {fieldErrors.telefone && (
                                    <span className="field-error">Telefone é obrigatório</span>
                                )}
                            </div>
                        </fieldset>

                        {/* Dados Contratuais */}
                        <fieldset
                            className="form-section"
                            role="tabpanel"
                            id="professor-panel-contratuais"
                            aria-labelledby="professor-tab-contratuais"
                            hidden={activeTab !== 'contratuais'}
                        >
                            <p className="form-tab-hint">Opcional: estes dados podem ser preenchidos depois.</p>

                            <div className="form-group">
                                <label htmlFor="matricula">Matrícula</label>
                                <input
                                    type="text"
                                    id="matricula"
                                    name="matricula"
                                    value={form.matricula}
                                    onChange={handleFieldChange}
                                    disabled={isSaving}
                                />
                            </div>

                            <div className="form-group">
                                <label htmlFor="dataAdmissao">Data de Admissão</label>
                                <input
                                    type="date"
                                    id="dataAdmissao"
                                    name="dataAdmissao"
                                    value={form.dataAdmissao}
                                    onChange={handleFieldChange}
                                    disabled={isSaving}
                                />
                            </div>

                            <div className="form-group">
                                <label htmlFor="situacao">Situação</label>
                                <select
                                    id="situacao"
                                    name="situacao"
                                    value={form.situacao}
                                    onChange={handleFieldChange}
                                    disabled={isSaving}
                                >
                                    {situacoes.map(situacao => (
                                        <option key={situacao.value} value={situacao.value}>
                                            {situacao.label}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-group form-field-full">
                                <label>Escolas</label>
                                <div className="checkbox-group">
                                    {escolas.length === 0 ? (
                                        <p className="no-options">Nenhuma escola disponível</p>
                                    ) : (
                                        escolas.map(escola => (
                                            <label key={escola.id} className="checkbox-label">
                                                <input
                                                    type="checkbox"
                                                    name="escolaIds"
                                                    value={escola.id.toString()}
                                                    checked={form.escolaIds.includes(escola.id.toString())}
                                                    onChange={handleFieldChange}
                                                    disabled={isSaving}
                                                />
                                                <span>{escola.nome}</span>
                                            </label>
                                        ))
                                    )}
                                </div>
                            </div>
                        </fieldset>

                        {/* Habilitação Pedagógica */}
                        <fieldset
                            className="form-section"
                            role="tabpanel"
                            id="professor-panel-habilitacao"
                            aria-labelledby="professor-tab-habilitacao"
                            hidden={activeTab !== 'habilitacao'}
                        >
                            <p className="form-tab-hint">Opcional: as disciplinas podem ser vinculadas depois.</p>

                            <div className="form-group form-field-full">
                                <label>Disciplinas</label>
                                <div className="checkbox-group">
                                    {disciplinas.length === 0 ? (
                                        <p className="no-options">Nenhuma disciplina disponível</p>
                                    ) : (
                                        disciplinas.map(disciplina => (
                                            <label key={disciplina.id} className="checkbox-label">
                                                <input
                                                    type="checkbox"
                                                    name="disciplinaIds"
                                                    value={disciplina.id.toString()}
                                                    checked={form.disciplinaIds.includes(disciplina.id.toString())}
                                                    onChange={handleFieldChange}
                                                    disabled={isSaving}
                                                />
                                                <span>{disciplina.nome}</span>
                                            </label>
                                        ))
                                    )}
                                </div>
                            </div>
                        </fieldset>

                        <div className="form-actions">
                            <button type="submit" className="btn btn-primary" disabled={isSaving}>
                                {isSaving ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}
                            </button>
                            <button type="button" className="btn btn-secondary" onClick={handleBackToList} disabled={isSaving}>
                                Cancelar
                            </button>
                        </div>
                    </form>
                </div>
            )}

            <ConfirmDialog
                open={professorToDelete !== null}
                title="Excluir professor"
                variant="danger"
                confirmLabel="Excluir"
                isLoading={isDeleting}
                onConfirm={handleConfirmDelete}
                onCancel={handleCancelDelete}
            >
                <p>
                    Tem certeza que deseja excluir <strong>{professorToDelete?.nome}</strong>
                    {professorToDelete?.matricula && <> (matrícula {professorToDelete.matricula})</>}?
                </p>
                <p>As habilitações e escolas vinculadas também serão removidas. Esta ação não pode ser desfeita.</p>
            </ConfirmDialog>
            {confirmDialog}
        </div>
    );
}

export default ProfessoresPage;
