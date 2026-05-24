import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import './MainContent.css';

type EscolaStatus = 'ATIVO' | 'INATIVO';

interface EscolaFormState {
    codigoInep: string;
    nome: string;
    cnpj: string;
    telefone: string;
    emailInstitucional: string;
    municipio: string;
    enderecoCompleto: string;
    status: EscolaStatus;
}

interface Escola extends EscolaFormState {
    id: number;
}

const emptyEscolaForm: EscolaFormState = {
    codigoInep: '',
    nome: '',
    cnpj: '',
    telefone: '',
    emailInstitucional: '',
    municipio: '',
    enderecoCompleto: '',
    status: 'ATIVO',
};

interface MainContentProps {
    page: string;
}

function MainContent({ page }: MainContentProps) {
    const [escolas, setEscolas] = useState<Escola[]>([]);
    const [form, setForm] = useState<EscolaFormState>(emptyEscolaForm);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    useEffect(() => {
        if (page !== 'escolas') {
            return;
        }

        void loadEscolas();
    }, [page]);

    const loadEscolas = async () => {
        setIsLoading(true);
        setErrorMessage(null);

        try {
            const response = await fetch('/api/escolas');
            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            const data = (await response.json()) as Escola[];
            setEscolas(data);
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao carregar escolas.';
            setErrorMessage(message);
        } finally {
            setIsLoading(false);
        }
    };

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;

        setForm((currentForm) => ({
            ...currentForm,
            [name]: value,
        }));
    };

    const clearForm = () => {
        setForm(emptyEscolaForm);
        setEditingId(null);
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        setIsSaving(true);
        setErrorMessage(null);

        try {
            const isEditing = editingId !== null;
            const endpoint = isEditing ? `/api/escolas/${editingId}` : '/api/escolas';
            const method = isEditing ? 'PUT' : 'POST';

            const response = await fetch(endpoint, {
                method,
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(form),
            });

            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            const escola = (await response.json()) as Escola;

            if (isEditing) {
                setEscolas((currentEscolas) =>
                    currentEscolas.map((item) => (item.id === escola.id ? escola : item))
                );
            } else {
                setEscolas((currentEscolas) => [...currentEscolas, escola]);
            }

            clearForm();
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao salvar escola.';
            setErrorMessage(message);
        } finally {
            setIsSaving(false);
        }
    };

    const handleEdit = (escola: Escola) => {
        setEditingId(escola.id);
        setForm({
            codigoInep: escola.codigoInep,
            nome: escola.nome,
            cnpj: escola.cnpj,
            telefone: escola.telefone,
            emailInstitucional: escola.emailInstitucional,
            municipio: escola.municipio,
            enderecoCompleto: escola.enderecoCompleto,
            status: escola.status,
        });
    };

    const handleDelete = async (id: number) => {
        setErrorMessage(null);

        try {
            const response = await fetch(`/api/escolas/${id}`, {
                method: 'DELETE',
            });

            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            setEscolas((currentEscolas) => currentEscolas.filter((escola) => escola.id !== id));

            if (editingId === id) {
                clearForm();
            }
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao excluir escola.';
            setErrorMessage(message);
        }
    };

    const readApiError = async (response: Response): Promise<string> => {
        try {
            const payload = (await response.json()) as { message?: string };
            return payload.message ?? `Erro ${response.status}`;
        } catch {
            return `Erro ${response.status}`;
        }
    };

    switch (page) {
        case 'escolas':
            return (
                <div className="school-page">
                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>{editingId ? 'Editar Escola' : 'Cadastro de Escolas'}</h2>
                                <p>Cadastre, edite e remova escolas usando os dados do modelo principal.</p>
                            </div>
                            {editingId !== null ? (
                                <button type="button" className="secondary-button" onClick={clearForm}>
                                    Cancelar edição
                                </button>
                            ) : null}
                        </div>

                        {errorMessage ? <div className="feedback-message feedback-error">{errorMessage}</div> : null}

                        <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                            <div className="form-grid">
                                <input
                                    name="codigoInep"
                                    value={form.codigoInep}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Código INEP"
                                    required
                                />
                                <input
                                    name="nome"
                                    value={form.nome}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Nome da escola"
                                    required
                                />
                                <input
                                    name="cnpj"
                                    value={form.cnpj}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="CNPJ"
                                    required
                                />
                                <input
                                    name="telefone"
                                    value={form.telefone}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Telefone"
                                    required
                                />
                                <input
                                    name="emailInstitucional"
                                    value={form.emailInstitucional}
                                    onChange={handleFieldChange}
                                    type="email"
                                    placeholder="E-mail institucional"
                                    required
                                />
                                <input
                                    name="municipio"
                                    value={form.municipio}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Município"
                                    required
                                />
                                <input
                                    name="enderecoCompleto"
                                    value={form.enderecoCompleto}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Endereço completo"
                                    required
                                />
                                <select name="status" value={form.status} onChange={handleFieldChange}>
                                    <option value="ATIVO">Ativa</option>
                                    <option value="INATIVO">Inativa</option>
                                </select>
                            </div>

                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Escola' : 'Salvar Escola'}
                            </button>
                        </form>
                    </div>

                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>Escolas cadastradas</h2>
                                <p>Lista sincronizada com o backend para visualização, edição e remoção.</p>
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="empty-state">
                                <strong>Carregando escolas...</strong>
                            </div>
                        ) : escolas.length > 0 ? (
                            <div className="table-responsive">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Nome</th>
                                            <th>INEP</th>
                                            <th>Município</th>
                                            <th>Status</th>
                                            <th>Ações</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {escolas.map((escola) => (
                                            <tr key={escola.id}>
                                                <td>{escola.nome}</td>
                                                <td>{escola.codigoInep}</td>
                                                <td>{escola.municipio}</td>
                                                <td>
                                                    <span
                                                        className={`status-pill ${escola.status === 'ATIVO' ? 'status-active' : 'status-inactive'}`}
                                                    >
                                                        {escola.status}
                                                    </span>
                                                </td>
                                                <td>
                                                    <div className="action-group">
                                                        <button
                                                            type="button"
                                                            className="table-action-button"
                                                            onClick={() => handleEdit(escola)}
                                                        >
                                                            Editar
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="table-action-button danger"
                                                            onClick={() => handleDelete(escola.id)}
                                                        >
                                                            Excluir
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <div className="empty-state">
                                <strong>Nenhuma escola cadastrada ainda.</strong>
                                <span>Use o formulário acima para criar o primeiro registro.</span>
                            </div>
                        )}
                    </div>
                </div>
            );
        default:
            return (
                <div className="content-card">
                    <h2>Bem-vindo(a) ao Diário de Classe</h2>
                    <p>Selecione a opção de Escolas no menu lateral para gerenciar os registros.</p>
                </div>
            );
    }
}

export default MainContent;