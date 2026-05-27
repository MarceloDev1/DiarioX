import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import './MainContent.css';

type EscolaStatus = 'ATIVO' | 'INATIVO';
type UsuarioStatus = 'ATIVO' | 'INATIVO' | 'BLOQUEADO';

interface EscolaFormState {
    codigoInep: string;
    nome: string;
    cnpj: string;
    telefone: string;
    emailInstitucional: string;
    municipio: string;
    enderecoCompleto: string;
    status: EscolaStatus;
    cpfDiretor: string;
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
    cpfDiretor: '',
};

interface Perfil {
    id: number;
    nome: string;
}

interface UsuarioFormState {
    email: string;
    cpf: string;
    dataNascimento: string;
    senha: string;
    status: UsuarioStatus;
    perfilId: number | null;
}

interface Usuario {
    id: number;
    email: string;
    cpf: string;
    dataNascimento: string | null;
    status: UsuarioStatus;
    ultimoAcesso: string | null;
    createdAt: string;
    perfilId: number | null;
    perfilNome: string | null;
}

const emptyUsuarioForm: UsuarioFormState = {
    email: '',
    cpf: '',
    dataNascimento: '',
    senha: '',
    status: 'ATIVO',
    perfilId: null,
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

    const [usuarios, setUsuarios] = useState<Usuario[]>([]);
    const [usuarioForm, setUsuarioForm] = useState<UsuarioFormState>(emptyUsuarioForm);
    const [usuarioEditingId, setUsuarioEditingId] = useState<number | null>(null);
    const [usuariosLoading, setUsuariosLoading] = useState(false);
    const [usuariosSaving, setUsuariosSaving] = useState(false);
    const [usuariosError, setUsuariosError] = useState<string | null>(null);

    const [perfis, setPerfis] = useState<Perfil[]>([]);

    useEffect(() => {
        if (page !== 'escolas') {
            return;
        }

        void loadEscolas();
    }, [page]);

    useEffect(() => {
        if (page !== 'usuarios') {
            return;
        }

        void loadUsuarios();
        void loadPerfis();
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

    const loadPerfis = async () => {
        try {
            const response = await fetch('/api/perfis');
            if (!response.ok) return;
            const data = (await response.json()) as Perfil[];
            setPerfis(data);
        } catch {
            // silently ignore — perfis are optional context
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
            cpfDiretor: escola.cpfDiretor ?? '',
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

    const loadUsuarios = async () => {
        setUsuariosLoading(true);
        setUsuariosError(null);

        try {
            const response = await fetch('/api/users');
            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            const data = (await response.json()) as Usuario[];
            setUsuarios(data);
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao carregar usuários.';
            setUsuariosError(message);
        } finally {
            setUsuariosLoading(false);
        }
    };

    const handleUsuarioFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;
        if (name === 'perfilId') {
            setUsuarioForm((current) => ({ ...current, perfilId: value ? Number(value) : null }));
        } else {
            setUsuarioForm((current) => ({ ...current, [name]: value }));
        }
    };

    const clearUsuarioForm = () => {
        setUsuarioForm(emptyUsuarioForm);
        setUsuarioEditingId(null);
    };

    const handleUsuarioSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setUsuariosSaving(true);
        setUsuariosError(null);

        try {
            const isEditing = usuarioEditingId !== null;
            const endpoint = isEditing ? `/api/users/${usuarioEditingId}` : '/api/users';
            const method = isEditing ? 'PUT' : 'POST';

            const body = {
                email: usuarioForm.email,
                cpf: usuarioForm.cpf,
                dataNascimento: usuarioForm.dataNascimento || null,
                senha: usuarioForm.senha || null,
                status: usuarioForm.status,
                perfilId: usuarioForm.perfilId,
            };

            const response = await fetch(endpoint, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            const usuario = (await response.json()) as Usuario;

            if (isEditing) {
                setUsuarios((current) => current.map((u) => (u.id === usuario.id ? usuario : u)));
            } else {
                setUsuarios((current) => [...current, usuario]);
            }

            clearUsuarioForm();
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao salvar usuário.';
            setUsuariosError(message);
        } finally {
            setUsuariosSaving(false);
        }
    };

    const handleUsuarioEdit = (usuario: Usuario) => {
        setUsuarioEditingId(usuario.id);
        setUsuarioForm({
            email: usuario.email,
            cpf: usuario.cpf,
            dataNascimento: usuario.dataNascimento ? usuario.dataNascimento.split('T')[0] : '',
            senha: '',
            status: usuario.status,
            perfilId: usuario.perfilId,
        });
    };

    const handleUsuarioDelete = async (id: number) => {
        setUsuariosError(null);

        try {
            const response = await fetch(`/api/users/${id}`, { method: 'DELETE' });

            if (!response.ok) {
                throw new Error(await readApiError(response));
            }

            setUsuarios((current) => current.filter((u) => u.id !== id));

            if (usuarioEditingId === id) {
                clearUsuarioForm();
            }
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Falha ao excluir usuário.';
            setUsuariosError(message);
        }
    };

    const formatCpf = (cpf: string): string => {
        const digits = cpf.replace(/\D/g, '');
        if (digits.length !== 11) return cpf;
        return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
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
                                <input
                                    name="cpfDiretor"
                                    value={form.cpfDiretor}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="CPF do Diretor (somente números)"
                                    maxLength={14}
                                />
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
                                            <th>Diretor (CPF)</th>
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
                                                <td>{escola.cpfDiretor ? formatCpf(escola.cpfDiretor) : '—'}</td>
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
        case 'usuarios':
            return (
                <div className="school-page">
                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>{usuarioEditingId ? 'Editar Usuário' : 'Cadastro de Usuários'}</h2>
                                <p>Cadastre, edite e remova usuários do sistema.</p>
                            </div>
                            {usuarioEditingId !== null ? (
                                <button type="button" className="secondary-button" onClick={clearUsuarioForm}>
                                    Cancelar edição
                                </button>
                            ) : null}
                        </div>

                        {usuariosError ? <div className="feedback-message feedback-error">{usuariosError}</div> : null}

                        <form className="cadastro-form escola-form" onSubmit={handleUsuarioSubmit}>
                            <div className="form-grid">
                                <input
                                    name="email"
                                    value={usuarioForm.email}
                                    onChange={handleUsuarioFieldChange}
                                    type="email"
                                    placeholder="E-mail"
                                    required
                                />
                                <input
                                    name="cpf"
                                    value={usuarioForm.cpf}
                                    onChange={handleUsuarioFieldChange}
                                    type="text"
                                    placeholder="CPF (somente números)"
                                    maxLength={14}
                                    required
                                />
                                <input
                                    name="dataNascimento"
                                    value={usuarioForm.dataNascimento}
                                    onChange={handleUsuarioFieldChange}
                                    type="date"
                                    placeholder="Data de nascimento"
                                />
                                <input
                                    name="senha"
                                    value={usuarioForm.senha}
                                    onChange={handleUsuarioFieldChange}
                                    type="password"
                                    placeholder={
                                        usuarioEditingId
                                            ? 'Nova senha (deixe em branco para manter)'
                                            : 'Senha (opcional — use primeiro acesso)'
                                    }
                                />
                                <select name="status" value={usuarioForm.status} onChange={handleUsuarioFieldChange}>
                                    <option value="ATIVO">Ativo</option>
                                    <option value="INATIVO">Inativo</option>
                                    <option value="BLOQUEADO">Bloqueado</option>
                                </select>
                                <select
                                    name="perfilId"
                                    value={usuarioForm.perfilId ?? ''}
                                    onChange={handleUsuarioFieldChange}
                                >
                                    <option value="">Selecione um perfil</option>
                                    {perfis.map((perfil) => (
                                        <option key={perfil.id} value={perfil.id}>
                                            {perfil.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <button type="submit" disabled={usuariosSaving}>
                                {usuarioEditingId !== null ? 'Atualizar Usuário' : 'Salvar Usuário'}
                            </button>
                        </form>
                    </div>

                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>Usuários cadastrados</h2>
                                <p>Lista sincronizada com o backend para visualização, edição e remoção.</p>
                            </div>
                        </div>

                        {usuariosLoading ? (
                            <div className="empty-state">
                                <strong>Carregando usuários...</strong>
                            </div>
                        ) : usuarios.length > 0 ? (
                            <div className="table-responsive">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>E-mail</th>
                                            <th>CPF</th>
                                            <th>Data de Nascimento</th>
                                            <th>Perfil</th>
                                            <th>Status</th>
                                            <th>Ações</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {usuarios.map((usuario) => (
                                            <tr key={usuario.id}>
                                                <td>{usuario.email}</td>
                                                <td>{formatCpf(usuario.cpf)}</td>
                                                <td>
                                                    {usuario.dataNascimento
                                                        ? new Date(usuario.dataNascimento).toLocaleDateString('pt-BR')
                                                        : '—'}
                                                </td>
                                                <td>{usuario.perfilNome ?? '—'}</td>
                                                <td>
                                                    <span
                                                        className={`status-pill ${
                                                            usuario.status === 'ATIVO'
                                                                ? 'status-active'
                                                                : usuario.status === 'BLOQUEADO'
                                                                  ? 'status-blocked'
                                                                  : 'status-inactive'
                                                        }`}
                                                    >
                                                        {usuario.status}
                                                    </span>
                                                </td>
                                                <td>
                                                    <div className="action-group">
                                                        <button
                                                            type="button"
                                                            className="table-action-button"
                                                            onClick={() => handleUsuarioEdit(usuario)}
                                                        >
                                                            Editar
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="table-action-button danger"
                                                            onClick={() => handleUsuarioDelete(usuario.id)}
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
                                <strong>Nenhum usuário cadastrado ainda.</strong>
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
                    <p>Selecione uma opção no menu lateral para gerenciar os registros.</p>
                </div>
            );
    }
}

export default MainContent;
