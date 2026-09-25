import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';
import '../MainContent.css';

type InstituicaoStatus = 'ATIVO' | 'INATIVO';
type View = 'list' | 'form';

interface InstituicaoFormState {
    nome: string;
    slug: string;
    status: InstituicaoStatus;
}

interface Instituicao extends InstituicaoFormState {
    id: number;
    createdAt: string;
}

const emptyForm: InstituicaoFormState = {
    nome: '',
    slug: '',
    status: 'ATIVO',
};

// Converte o nome digitado em um subdomínio válido (ex.: "Colégio São José" → "colegio-sao-jose").
function toSlug(value: string) {
    return value
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .slice(0, 63);
}

/** Gestão das instituições (tenants), disponível apenas para o Administrador global. */
function InstituicoesPage() {
    const { items: instituicoes, isLoading, isSaving, error, load, save } =
        useCrudData<Instituicao>('/api/tenants');
    const { form, setForm, editingId, handleFieldChange, startEdit, clear } =
        useCrudForm<InstituicaoFormState & Record<string, unknown>>(
            emptyForm as InstituicaoFormState & Record<string, unknown>
        );
    const [view, setView] = useState<View>('list');
    const [slugEditado, setSlugEditado] = useState(false);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [statusUpdatingId, setStatusUpdatingId] = useState<number | null>(null);
    const [filterNome, setFilterNome] = useState('');
    const [appliedNome, setAppliedNome] = useState('');

    useEffect(() => {
        void load();
    }, []);

    const filteredInstituicoes = instituicoes.filter(i => {
        if (!appliedNome) return true;
        const termo = appliedNome.toLowerCase();
        return i.nome.toLowerCase().includes(termo) || i.slug.includes(termo);
    });

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedNome(filterNome.trim());
    };

    const handleLimparFiltros = () => {
        setFilterNome('');
        setAppliedNome('');
    };

    // Enquanto o subdomínio não for editado à mão, ele acompanha o nome.
    const handleNomeChange = (e: ChangeEvent<HTMLInputElement>) => {
        const nome = e.target.value;
        setForm(current => ({ ...current, nome, slug: slugEditado ? current.slug : toSlug(nome) }));
    };

    const handleSlugChange = (e: ChangeEvent<HTMLInputElement>) => {
        setSlugEditado(true);
        const slug = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '').slice(0, 63);
        setForm(current => ({ ...current, slug }));
    };

    const handleNova = () => {
        setSuccessMessage(null);
        setSlugEditado(false);
        clear();
        setView('form');
    };

    const handleEdit = (instituicao: Instituicao) => {
        setSuccessMessage(null);
        setSlugEditado(true);
        startEdit(instituicao.id, {
            nome: instituicao.nome,
            slug: instituicao.slug,
            status: instituicao.status,
        });
        setView('form');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const isEditing = editingId !== null;

        const saved = await save(editingId, {
            nome: form.nome,
            slug: form.slug,
            status: form.status,
        });

        if (saved) {
            setSuccessMessage(isEditing ? 'Instituição atualizada com sucesso!' : 'Instituição cadastrada com sucesso!');
            clear();
            setView('list');
        }
    };

    const handleCancel = () => {
        clear();
        setView('list');
    };

    const handleToggleStatus = async (instituicao: Instituicao) => {
        const inativar = instituicao.status === 'ATIVO';
        const confirmMessage = inativar
            ? `Inativar "${instituicao.nome}"? Os usuários dela não conseguirão mais entrar no sistema.`
            : `Ativar "${instituicao.nome}"?`;
        if (!window.confirm(confirmMessage)) return;

        setSuccessMessage(null);
        setStatusUpdatingId(instituicao.id);
        const saved = await save(instituicao.id, {
            nome: instituicao.nome,
            slug: instituicao.slug,
            status: inativar ? 'INATIVO' : 'ATIVO',
        });
        setStatusUpdatingId(null);

        if (saved)
            setSuccessMessage(inativar ? 'Instituição inativada com sucesso!' : 'Instituição ativada com sucesso!');
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId ? 'Editar Instituição' : 'Nova Instituição'}</h2>
                            <p>
                                {editingId
                                    ? 'Altere os dados e salve para atualizar.'
                                    : 'Cadastre uma instituição ou mantenedora. Ela começa sem escolas, modalidades e usuários.'}
                            </p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="instituicao-nome">Nome</label>
                                <input
                                    id="instituicao-nome"
                                    name="nome"
                                    value={form.nome as string}
                                    onChange={handleNomeChange}
                                    type="text"
                                    maxLength={255}
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="instituicao-slug">Subdomínio</label>
                                <input
                                    id="instituicao-slug"
                                    name="slug"
                                    value={form.slug as string}
                                    onChange={handleSlugChange}
                                    type="text"
                                    maxLength={63}
                                    placeholder="Ex: colegio-horizonte"
                                    required
                                />
                                <span className="label-optional">
                                    Endereço de acesso: {(form.slug as string) || 'subdominio'}.&lt;domínio do sistema&gt;
                                    {editingId && ' — alterar muda o endereço usado pelos usuários.'}
                                </span>
                            </div>
                            <div className="form-field">
                                <label htmlFor="instituicao-status">Status</label>
                                <select
                                    id="instituicao-status"
                                    name="status"
                                    value={form.status as string}
                                    onChange={handleFieldChange}
                                >
                                    <option value="ATIVO">Ativo</option>
                                    <option value="INATIVO">Inativo</option>
                                </select>
                            </div>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Instituição' : 'Salvar Instituição'}
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
                        <h2>Instituições</h2>
                        <p>Instituições e mantenedoras que usam o sistema. Cada uma tem seus próprios dados e usuários.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNova}>
                        + Nova Instituição
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-instituicao">Nome ou subdomínio</label>
                        <input
                            id="filtro-instituicao"
                            type="text"
                            value={filterNome}
                            onChange={e => setFilterNome(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={successMessage} type="success" />
                <FeedbackMessage message={error} />

                {isLoading || filteredInstituicoes.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando instituições..."
                        emptyMessage="Nenhuma instituição encontrada."
                        emptySubMessage={instituicoes.length === 0 ? 'Clique em Nova Instituição para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nome</th>
                                    <th>Subdomínio</th>
                                    <th>Cadastrada em</th>
                                    <th>Status</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredInstituicoes.map(i => (
                                    <tr key={i.id}>
                                        <td>{i.nome}</td>
                                        <td>{i.slug}</td>
                                        <td>{new Date(i.createdAt).toLocaleDateString('pt-BR')}</td>
                                        <td><StatusPill status={i.status} /></td>
                                        <td>
                                            <div className="action-group vertical">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(i)}>Editar</button>
                                                <button
                                                    type="button"
                                                    className="table-action-button"
                                                    onClick={() => handleToggleStatus(i)}
                                                    disabled={statusUpdatingId === i.id}
                                                >
                                                    {i.status === 'ATIVO' ? 'Inativar' : 'Ativar'}
                                                </button>
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

export default InstituicoesPage;
