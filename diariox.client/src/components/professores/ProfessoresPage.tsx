import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import StatusPill from '../ui/StatusPill';

type View = 'list' | 'form';

interface Disciplina {
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
    matricula: string;
    dataAdmissao: string;
    situacao: string;
    escolaId: number;
    escolaNome: string;
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
    escolaId: string;
    disciplinaIds: string[];
}

interface ProfessorFieldErrors {
    nome: boolean;
    cpf: boolean;
    dataNascimento: boolean;
    email: boolean;
    telefone: boolean;
    matricula: boolean;
    dataAdmissao: boolean;
    situacao: boolean;
    escolaId: boolean;
    disciplinaIds: boolean;
}

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
    escolaId: '',
    disciplinaIds: [],
};

const emptyFieldErrors: ProfessorFieldErrors = {
    nome: false,
    cpf: false,
    dataNascimento: false,
    email: false,
    telefone: false,
    matricula: false,
    dataAdmissao: false,
    situacao: false,
    escolaId: false,
    disciplinaIds: false,
};

function ProfessoresPage() {
    const { items: professores, isLoading, isSaving, error, load, save, remove } = useCrudData<Professor>('/api/professores');

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<ProfessorFormState>(emptyForm);
    const [fieldErrors, setFieldErrors] = useState<ProfessorFieldErrors>(emptyFieldErrors);
    const [localError, setLocalError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);

    const [escolas, setEscolas] = useState<EscolaOption[]>([]);
    const [disciplinas, setDisciplinas] = useState<DisciplinaOption[]>([]);

    useEffect(() => {
        void load();
        void loadOptions();
    }, []);

    const loadOptions = async () => {
        try {
            const [escolasRes, disciplinasRes] = await Promise.all([
                fetch('/api/escolas'),
                fetch('/api/disciplinas'),
            ]);

            if (escolasRes.ok) {
                const escolasData = (await escolasRes.json()) as EscolaOption[];
                setEscolas(escolasData.filter(escola => escola.status === 'ATIVO'));
            }

            if (disciplinasRes.ok) {
                const disciplinasData = (await disciplinasRes.json()) as DisciplinaOption[];
                setDisciplinas(disciplinasData);
            }
        } catch {
            setLocalError('Falha ao carregar opções do formulário.');
        }
    };

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
            const disciplinaId = checkbox.value;
            setForm(current => {
                const disciplinaIds = checkbox.checked
                    ? [...current.disciplinaIds, disciplinaId]
                    : current.disciplinaIds.filter(id => id !== disciplinaId);
                return { ...current, disciplinaIds };
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
            matricula: !form.matricula.trim(),
            dataAdmissao: !form.dataAdmissao,
            situacao: !form.situacao,
            escolaId: !form.escolaId,
            disciplinaIds: form.disciplinaIds.length === 0,
        };

        setFieldErrors(nextErrors);
        const hasErrors = Object.values(nextErrors).some(error => error);

        if (hasErrors) {
            setLocalError('Por favor, preencha todos os campos obrigatórios.');
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
            matricula: form.matricula,
            dataAdmissao: form.dataAdmissao,
            situacao: form.situacao,
            escolaId: parseInt(form.escolaId),
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
            dataNascimento: professor.dataNascimento,
            email: professor.email,
            telefone: professor.telefone,
            matricula: professor.matricula,
            dataAdmissao: professor.dataAdmissao,
            situacao: professor.situacao,
            escolaId: professor.escolaId.toString(),
            disciplinaIds: professor.disciplinas.map(d => d.id.toString()),
        });
        setView('form');
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
    };

    const handleDeleteClick = async (id: number) => {
        if (window.confirm('Tem certeza que deseja remover este professor?')) {
            const success = await remove(id);
            if (success) {
                setSuccessMessage('Professor removido com sucesso!');
                setTimeout(() => setSuccessMessage(null), 3000);
            }
        }
    };

    const handleNewProfessor = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFieldErrors(emptyFieldErrors);
        setLocalError(null);
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
                <h1>👨‍🏫 Professores</h1>
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
                                            <td>{professor.cpf}</td>
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
                                            <td className="actions-cell">
                                                <button
                                                    className="btn btn-sm btn-info"
                                                    onClick={() => handleEditClick(professor)}
                                                >
                                                    ✏️ Editar
                                                </button>
                                                <button
                                                    className="btn btn-sm btn-danger"
                                                    onClick={() => handleDeleteClick(professor.id)}
                                                >
                                                    🗑️ Deletar
                                                </button>
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
                        {/* Dados Pessoais */}
                        <fieldset className="form-section">
                            <legend>📋 Dados Pessoais</legend>

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
                        <fieldset className="form-section">
                            <legend>📄 Dados Contratuais</legend>

                            <div className="form-group">
                                <label htmlFor="matricula">
                                    Matrícula <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="matricula"
                                    name="matricula"
                                    value={form.matricula}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.matricula ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.matricula && <span className="field-error">Matrícula é obrigatória</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="dataAdmissao">
                                    Data de Admissão <span className="required">*</span>
                                </label>
                                <input
                                    type="date"
                                    id="dataAdmissao"
                                    name="dataAdmissao"
                                    value={form.dataAdmissao}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.dataAdmissao ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.dataAdmissao && (
                                    <span className="field-error">Data de admissão é obrigatória</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="situacao">
                                    Situação <span className="required">*</span>
                                </label>
                                <select
                                    id="situacao"
                                    name="situacao"
                                    value={form.situacao}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.situacao ? 'input-error' : ''}
                                    disabled={isSaving}
                                >
                                    <option value="">Selecione...</option>
                                    {situacoes.map(situacao => (
                                        <option key={situacao.value} value={situacao.value}>
                                            {situacao.label}
                                        </option>
                                    ))}
                                </select>
                                {fieldErrors.situacao && <span className="field-error">Situação é obrigatória</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="escolaId">
                                    Escola <span className="required">*</span>
                                </label>
                                <select
                                    id="escolaId"
                                    name="escolaId"
                                    value={form.escolaId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.escolaId ? 'input-error' : ''}
                                    disabled={isSaving}
                                >
                                    <option value="">Selecione...</option>
                                    {escolas.map(escola => (
                                        <option key={escola.id} value={escola.id}>
                                            {escola.nome}
                                        </option>
                                    ))}
                                </select>
                                {fieldErrors.escolaId && <span className="field-error">Escola é obrigatória</span>}
                            </div>
                        </fieldset>

                        {/* Habilitação Pedagógica */}
                        <fieldset className="form-section">
                            <legend>🎓 Habilitação Pedagógica</legend>

                            <div className="form-group">
                                <label>
                                    Disciplinas <span className="required">*</span>
                                </label>
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
                                {fieldErrors.disciplinaIds && (
                                    <span className="field-error">Selecione pelo menos uma disciplina</span>
                                )}
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
        </div>
    );
}

export default ProfessoresPage;
