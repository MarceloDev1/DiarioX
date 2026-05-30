import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCpf } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';

type UsuarioStatus = 'ATIVO' | 'INATIVO' | 'BLOQUEADO';
type View = 'list' | 'form';

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

function UsuariosPage() {
    const { items: usuarios, isLoading, isSaving, error, load, save, remove } = useCrudData<Usuario>('/api/users');
    const { form, setForm, editingId, startEdit, clear } = useCrudForm<UsuarioFormState & Record<string, unknown>>(
        emptyUsuarioForm as UsuarioFormState & Record<string, unknown>
    );
    const { items: perfis, load: loadPerfis } = useCrudData<Perfil>('/api/perfis');
    const [view, setView] = useState<View>('list');

    const [filterEmail, setFilterEmail] = useState('');
    const [filterCpf, setFilterCpf] = useState('');
    const [filterStatus, setFilterStatus] = useState('');
    const [filterPerfilId, setFilterPerfilId] = useState('');

    const [appliedEmail, setAppliedEmail] = useState('');
    const [appliedCpf, setAppliedCpf] = useState('');
    const [appliedStatus, setAppliedStatus] = useState('');
    const [appliedPerfilId, setAppliedPerfilId] = useState('');

    useEffect(() => {
        void load();
        void loadPerfis();
    }, []);

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;
        if (name === 'perfilId') {
            setForm((current) => ({ ...current, perfilId: value ? Number(value) : null }));
        } else {
            setForm((current) => ({ ...current, [name]: value }));
        }
    };

    const handleEdit = (usuario: Usuario) => {
        startEdit(usuario.id, {
            email: usuario.email,
            cpf: usuario.cpf,
            dataNascimento: usuario.dataNascimento ? usuario.dataNascimento.split('T')[0] : '',
            senha: '',
            status: usuario.status,
            perfilId: usuario.perfilId,
        });
        setView('form');
    };

    const handleNovoUsuario = () => {
        clear();
        setView('form');
    };

    const handleCancelar = () => {
        clear();
        setView('list');
    };

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedEmail(filterEmail.trim());
        setAppliedCpf(filterCpf.trim());
        setAppliedStatus(filterStatus);
        setAppliedPerfilId(filterPerfilId);
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const body = {
            email: form.email,
            cpf: form.cpf,
            dataNascimento: form.dataNascimento || null,
            senha: form.senha || null,
            status: form.status,
            perfilId: form.perfilId,
        };
        const saved = await save(editingId, body);
        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleDelete = async (id: number) => {
        const deleted = await remove(id);
        if (deleted && editingId === id) clear();
    };

    const filteredUsuarios = usuarios.filter((usuario) => {
        const matchEmail = !appliedEmail || usuario.email.toLowerCase().includes(appliedEmail.toLowerCase());
        const matchCpf = !appliedCpf || usuario.cpf.replace(/\D/g, '').includes(appliedCpf.replace(/\D/g, ''));
        const matchStatus = !appliedStatus || usuario.status === appliedStatus;
        const matchPerfil = !appliedPerfilId || String(usuario.perfilId ?? '') === appliedPerfilId;
        return matchEmail && matchCpf && matchStatus && matchPerfil;
    });

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId ? 'Editar Usuário' : 'Novo Usuário'}</h2>
                            <p>{editingId ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar um novo usuário.'}</p>
                        </div>
                        <button type="button" className="secondary-button" onClick={handleCancelar}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="usuario-email">E-mail</label>
                                <input id="usuario-email" name="email" value={form.email} onChange={handleFieldChange} type="email" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="usuario-cpf">CPF</label>
                                <input id="usuario-cpf" name="cpf" value={form.cpf} onChange={handleFieldChange} type="text" maxLength={14} required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="usuario-dataNascimento">Data de nascimento</label>
                                <input id="usuario-dataNascimento" name="dataNascimento" value={form.dataNascimento} onChange={handleFieldChange} type="date" />
                            </div>
                            <div className="form-field">
                                <label htmlFor="usuario-senha">{editingId ? 'Nova senha' : 'Senha'}</label>
                                <input
                                    id="usuario-senha"
                                    name="senha"
                                    value={form.senha}
                                    onChange={handleFieldChange}
                                    type="password"
                                    placeholder={editingId ? 'Deixe em branco para manter a senha atual' : 'Opcional - use primeiro acesso'}
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="usuario-status">Status</label>
                                <select id="usuario-status" name="status" value={form.status} onChange={handleFieldChange}>
                                    <option value="ATIVO">Ativo</option>
                                    <option value="INATIVO">Inativo</option>
                                    <option value="BLOQUEADO">Bloqueado</option>
                                </select>
                            </div>
                            <div className="form-field">
                                <label htmlFor="usuario-perfilId">Perfil</label>
                                <select id="usuario-perfilId" name="perfilId" value={form.perfilId ?? ''} onChange={handleFieldChange}>
                                    <option value="">Selecione um perfil</option>
                                    {perfis.map((perfil) => (
                                        <option key={perfil.id} value={perfil.id}>
                                            {perfil.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Usuário' : 'Salvar Usuário'}
                            </button>
                            <button type="button" className="secondary-button" onClick={clear}>
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
                        <h2>Usuários</h2>
                        <p>Consulte, edite e remova usuários cadastrados.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNovoUsuario}>
                        + Novo Usuário
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-usuario-email">Email</label>
                        <input
                            id="filtro-usuario-email"
                            type="text"
                            value={filterEmail}
                            onChange={e => setFilterEmail(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-usuario-cpf">CPF</label>
                        <input
                            id="filtro-usuario-cpf"
                            type="text"
                            value={filterCpf}
                            onChange={e => setFilterCpf(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-usuario-status">Status</label>
                        <select
                            id="filtro-usuario-status"
                            value={filterStatus}
                            onChange={e => setFilterStatus(e.target.value)}
                            className="filter-input"
                        >
                            <option value="">Todos</option>
                            <option value="ATIVO">Ativo</option>
                            <option value="INATIVO">Inativo</option>
                            <option value="BLOQUEADO">Bloqueado</option>
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-usuario-perfil">Perfil</label>
                        <select
                            id="filtro-usuario-perfil"
                            value={filterPerfilId}
                            onChange={e => setFilterPerfilId(e.target.value)}
                            className="filter-input"
                        >
                            <option value="">Todos</option>
                            {perfis.map((perfil) => (
                                <option key={perfil.id} value={perfil.id}>
                                    {perfil.nome}
                                </option>
                            ))}
                        </select>
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredUsuarios.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando usuários..."
                        emptyMessage="Nenhum usuário cadastrado ainda."
                        emptySubMessage={usuarios.length === 0 ? 'Clique em Novo Usuário para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
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
                                {filteredUsuarios.map((usuario) => (
                                    <tr key={usuario.id}>
                                        <td>{usuario.email}</td>
                                        <td>{formatCpf(usuario.cpf)}</td>
                                        <td>
                                            {usuario.dataNascimento
                                                ? new Date(usuario.dataNascimento).toLocaleDateString('pt-BR')
                                                : '—'}
                                        </td>
                                        <td>{usuario.perfilNome ?? '—'}</td>
                                        <td><StatusPill status={usuario.status} /></td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(usuario)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(usuario.id)}>Excluir</button>
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

export default UsuariosPage;
