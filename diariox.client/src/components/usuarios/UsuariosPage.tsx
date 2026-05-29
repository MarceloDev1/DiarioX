import { useEffect, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCpf } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';

type UsuarioStatus = 'ATIVO' | 'INATIVO' | 'BLOQUEADO';

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
    const { form, setForm, editingId, startEdit, clear } = useCrudForm(emptyUsuarioForm);
    const { items: perfis, load: loadPerfis } = useCrudData<Perfil>('/api/perfis');

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
        if (saved) clear();
    };

    const handleDelete = async (id: number) => {
        const deleted = await remove(id);
        if (deleted && editingId === id) clear();
    };

    return (
        <div className="school-page">
            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>{editingId ? 'Editar Usuário' : 'Cadastro de Usuários'}</h2>
                        <p>Cadastre, edite e remova usuários do sistema.</p>
                    </div>
                    {editingId !== null ? (
                        <button type="button" className="secondary-button" onClick={clear}>
                            Cancelar edição
                        </button>
                    ) : null}
                </div>

                <FeedbackMessage message={error} />

                <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                    <div className="form-grid">
                        <input name="email" value={form.email} onChange={handleFieldChange} type="email" placeholder="E-mail" required />
                        <input name="cpf" value={form.cpf} onChange={handleFieldChange} type="text" placeholder="CPF (somente números)" maxLength={14} required />
                        <input name="dataNascimento" value={form.dataNascimento} onChange={handleFieldChange} type="date" placeholder="Data de nascimento" />
                        <input
                            name="senha"
                            value={form.senha}
                            onChange={handleFieldChange}
                            type="password"
                            placeholder={editingId ? 'Nova senha (deixe em branco para manter)' : 'Senha (opcional — use primeiro acesso)'}
                        />
                        <select name="status" value={form.status} onChange={handleFieldChange}>
                            <option value="ATIVO">Ativo</option>
                            <option value="INATIVO">Inativo</option>
                            <option value="BLOQUEADO">Bloqueado</option>
                        </select>
                        <select name="perfilId" value={form.perfilId ?? ''} onChange={handleFieldChange}>
                            <option value="">Selecione um perfil</option>
                            {perfis.map((perfil) => (
                                <option key={perfil.id} value={perfil.id}>
                                    {perfil.nome}
                                </option>
                            ))}
                        </select>
                    </div>
                    <button type="submit" disabled={isSaving}>
                        {editingId !== null ? 'Atualizar Usuário' : 'Salvar Usuário'}
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

                {isLoading || usuarios.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando usuários..."
                        emptyMessage="Nenhum usuário cadastrado ainda."
                        emptySubMessage="Use o formulário acima para criar o primeiro registro."
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
