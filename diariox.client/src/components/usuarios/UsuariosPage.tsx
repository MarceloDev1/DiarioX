import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCpf } from '../../utils/formatters';
import { validateCpf, validatePassword } from '../../utils/validators';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';
import { usePermissoes } from '../../hooks/usePermissoes';

type UsuarioStatus = 'ATIVO' | 'INATIVO' | 'BLOQUEADO';
type View = 'list' | 'form';

interface Perfil {
    id: number;
    nome: string;
}

interface Escola {
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
    /** Vazio = todas as escolas da instituição. */
    escolaIds: number[];
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
    escolaIds: number[];
}

const emptyUsuarioForm: UsuarioFormState = {
    email: '',
    cpf: '',
    dataNascimento: '',
    senha: '',
    status: 'ATIVO',
    perfilId: null,
    escolaIds: [],
};

function UsuariosPage() {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();
    const { items: usuarios, isLoading, isSaving, error, load, save, remove } = useCrudData<Usuario>('/api/users');
    const { form, setForm, editingId, startEdit, clear } = useCrudForm<UsuarioFormState & Record<string, unknown>>(
        emptyUsuarioForm as UsuarioFormState & Record<string, unknown>
    );
    const { items: perfis, load: loadPerfis } = useCrudData<Perfil>('/api/perfis');
    const { items: escolas, load: loadEscolas } = useCrudData<Escola>('/api/escolas');
    const [view, setView] = useState<View>('list');
    const [formError, setFormError] = useState<string | null>(null);
    // Explícito: desmarcar a última escola não pode virar "todas as escolas" sem o usuário escolher isso.
    const [restritoAEscolas, setRestritoAEscolas] = useState(false);

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
        void loadEscolas();
    }, []);

    const nomeDaEscola = (id: number) => escolas.find(e => e.id === id)?.nome ?? 'Outra escola';
    const perfilSelecionado = perfis.find(p => p.id === form.perfilId);

    const handleEscopo = (restrito: boolean) => {
        setRestritoAEscolas(restrito);
        if (!restrito) setForm(current => ({ ...current, escolaIds: [] }));
    };

    const handleEscolaToggle = (escolaId: number, marcada: boolean) => {
        setForm(current => ({
            ...current,
            escolaIds: marcada ? [...current.escolaIds, escolaId] : current.escolaIds.filter(id => id !== escolaId),
        }));
    };

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;
        if (name === 'perfilId') {
            setForm((current) => ({ ...current, perfilId: value ? Number(value) : null }));
        } else if (name === 'cpf') {
            setForm((current) => ({ ...current, cpf: formatCpf(value) }));
        } else {
            setForm((current) => ({ ...current, [name]: value }));
        }
    };

    const handleEdit = (usuario: Usuario) => {
        setFormError(null);
        startEdit(usuario.id, {
            email: usuario.email,
            cpf: usuario.cpf,
            dataNascimento: usuario.dataNascimento ? usuario.dataNascimento.split('T')[0] : '',
            senha: '',
            status: usuario.status,
            perfilId: usuario.perfilId,
            escolaIds: usuario.escolaIds,
        });
        setRestritoAEscolas(usuario.escolaIds.length > 0);
        setView('form');
    };

    const handleNovoUsuario = () => {
        setFormError(null);
        setRestritoAEscolas(false);
        clear();
        setView('form');
    };

    const handleCancelar = () => {
        setFormError(null);
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

    const handleLimparFiltros = () => {
        setFilterEmail('');
        setFilterCpf('');
        setFilterStatus('');
        setFilterPerfilId('');
        setAppliedEmail('');
        setAppliedCpf('');
        setAppliedStatus('');
        setAppliedPerfilId('');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setFormError(null);

        if (!validateCpf(form.cpf)) {
            setFormError('Informe um CPF valido.');
            return;
        }

        if (restritoAEscolas && form.escolaIds.length === 0) {
            setFormError('Selecione ao menos uma escola de atuação.');
            return;
        }

        if (form.senha.trim() && !validatePassword(form.senha)) {
            setFormError('A senha deve ter no minimo 8 caracteres, com letra maiuscula, minuscula, numero e simbolo.');
            return;
        }

        const body = {
            email: form.email,
            cpf: form.cpf,
            dataNascimento: form.dataNascimento || null,
            senha: form.senha || null,
            status: form.status,
            perfilId: form.perfilId,
            escolaIds: form.escolaIds,
        };
        const saved = await save(editingId, body);
        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleDelete = async (id: number, email: string) => {
        const confirmed = await confirm({
            title: 'Excluir usuário',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: (
                <>
                    <p>Tem certeza que deseja excluir o usuário <strong>{email}</strong>?</p>
                    <p>Ele perderá o acesso ao sistema. Esta ação não pode ser desfeita.</p>
                </>
            ),
        });
        if (!confirmed) return;

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
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancelar}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={formError ?? error} />

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
                            <fieldset className="form-field form-field-full usuario-escopo">
                                <legend>Escolas de atuação</legend>
                                <div className="usuario-escopo-opcoes">
                                    <label className="checkbox-label">
                                        <input type="radio" name="escopo" checked={!restritoAEscolas} onChange={() => handleEscopo(false)} />
                                        <span>
                                            {perfilSelecionado?.nome === 'Professor'
                                                ? 'Escolas em que leciona'
                                                : 'Todas as escolas da instituição'}
                                        </span>
                                    </label>
                                    <label className="checkbox-label">
                                        <input
                                            type="radio"
                                            name="escopo"
                                            checked={restritoAEscolas}
                                            onChange={() => handleEscopo(true)}
                                            disabled={escolas.length === 0}
                                        />
                                        <span>Somente as escolas selecionadas</span>
                                    </label>
                                </div>
                                {restritoAEscolas && (
                                    <div className="checkbox-group">
                                        {escolas.map(escola => (
                                            <label key={escola.id} className="checkbox-label">
                                                <input
                                                    type="checkbox"
                                                    checked={form.escolaIds.includes(escola.id)}
                                                    onChange={e => handleEscolaToggle(escola.id, e.target.checked)}
                                                />
                                                <span>{escola.nome}</span>
                                            </label>
                                        ))}
                                    </div>
                                )}
                                <p className="field-hint">
                                    O usuário só vê e altera escolas, turmas, alunos, professores e chamadas das escolas em que atua.
                                </p>
                            </fieldset>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Usuário' : 'Salvar Usuário'}
                            </button>
                            <button
                                type="button"
                                
                                onClick={() => {
                                    setFormError(null);
                                    clear();
                                }}
                            >
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
                    {can('usuarios.criar') && (
                        <button type="button" className="primary-button" onClick={handleNovoUsuario}>
                            + Novo Usuário
                        </button>
                    )}
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
                            onChange={e => setFilterCpf(formatCpf(e.target.value))}
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
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
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
                                    <th>Escolas</th>
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
                                        <td>
                                            {usuario.perfilId === null
                                                ? '—'
                                                : usuario.escolaIds.length === 0
                                                    ? (usuario.perfilNome === 'Professor' ? 'Onde leciona' : 'Todas')
                                                    : usuario.escolaIds.map(nomeDaEscola).join(', ')}
                                        </td>
                                        <td><StatusPill status={usuario.status} /></td>
                                        <td>
                                            <div className="action-group">
                                                {can('usuarios.editar') && <button type="button" className="table-action-button" onClick={() => handleEdit(usuario)}>Editar</button>}
                                                {can('usuarios.excluir') && <button type="button" className="table-action-button danger" onClick={() => handleDelete(usuario.id, usuario.email)}>Excluir</button>}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
            {confirmDialog}
        </div>
    );
}

export default UsuariosPage;
