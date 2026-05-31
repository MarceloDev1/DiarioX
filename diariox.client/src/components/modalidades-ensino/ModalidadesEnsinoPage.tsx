import { useEffect, useState, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';

type ModalidadeEnsinoStatus = 'ATIVO' | 'INATIVO';
type View = 'list' | 'form';

interface ModalidadeEnsinoFormState {
    nome: string;
    sigla: string;
    codigoMec: string;
    descricao: string;
    status: ModalidadeEnsinoStatus;
}

interface ModalidadeEnsino extends ModalidadeEnsinoFormState {
    id: number;
}

const emptyForm: ModalidadeEnsinoFormState = {
    nome: '',
    sigla: '',
    codigoMec: '',
    descricao: '',
    status: 'ATIVO',
};

function ModalidadesEnsinoPage() {
    const { items: modalidades, isLoading, isSaving, error, load, save, remove } =
        useCrudData<ModalidadeEnsino>('/api/modalidadesensino');
    const { form, editingId, handleFieldChange, startEdit, clear } =
        useCrudForm<ModalidadeEnsinoFormState & Record<string, unknown>>(
            emptyForm as ModalidadeEnsinoFormState & Record<string, unknown>
        );
    const [formError, setFormError] = useState<string | null>(null);
    const [view, setView] = useState<View>('list');

    const [filterNome, setFilterNome] = useState('');
    const [filterSigla, setFilterSigla] = useState('');
    const [appliedNome, setAppliedNome] = useState('');
    const [appliedSigla, setAppliedSigla] = useState('');

    useEffect(() => {
        void load();
    }, []);

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedNome(filterNome.trim());
        setAppliedSigla(filterSigla.trim());
    };

    const handleLimparFiltros = () => {
        setFilterNome('');
        setFilterSigla('');
        setAppliedNome('');
        setAppliedSigla('');
    };

    const filteredModalidades = modalidades.filter(m => {
        const matchNome = !appliedNome || m.nome.toLowerCase().includes(appliedNome.toLowerCase());
        const matchSigla = !appliedSigla || m.sigla.toLowerCase().includes(appliedSigla.toLowerCase());
        return matchNome && matchSigla;
    });

    const handleNova = () => {
        setFormError(null);
        clear();
        setView('form');
    };

    const handleEdit = (modalidade: ModalidadeEnsino) => {
        setFormError(null);
        startEdit(modalidade.id, {
            nome: modalidade.nome,
            sigla: modalidade.sigla,
            codigoMec: modalidade.codigoMec ?? '',
            descricao: modalidade.descricao,
            status: modalidade.status,
        });
        setView('form');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setFormError(null);

        const saved = await save(editingId, {
            nome: form.nome,
            sigla: form.sigla,
            codigoMec: (form.codigoMec as string).trim() || null,
            descricao: form.descricao,
            status: form.status,
        });

        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleCancel = () => {
        setFormError(null);
        clear();
        setView('list');
    };

    const handleDelete = async (id: number) => {
        await remove(id);
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId ? 'Editar Modalidade de Ensino' : 'Nova Modalidade de Ensino'}</h2>
                            <p>{editingId ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar uma nova modalidade.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={formError ?? error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="modalidade-nome">Nome</label>
                                <input
                                    id="modalidade-nome"
                                    name="nome"
                                    value={form.nome as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="modalidade-sigla">Sigla</label>
                                <input
                                    id="modalidade-sigla"
                                    name="sigla"
                                    value={form.sigla as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="modalidade-codigoMec">Código MEC <span className="label-optional">(opcional)</span></label>
                                <input
                                    id="modalidade-codigoMec"
                                    name="codigoMec"
                                    value={form.codigoMec as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="modalidade-status">Status</label>
                                <select
                                    id="modalidade-status"
                                    name="status"
                                    value={form.status as string}
                                    onChange={handleFieldChange}
                                >
                                    <option value="ATIVO">Ativo</option>
                                    <option value="INATIVO">Inativo</option>
                                </select>
                            </div>
                            <div className="form-field form-field-full">
                                <label htmlFor="modalidade-descricao">Descrição</label>
                                <input
                                    id="modalidade-descricao"
                                    name="descricao"
                                    value={form.descricao as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                    required
                                />
                            </div>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Modalidade' : 'Salvar Modalidade'}
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
                        <h2>Modalidades de Ensino</h2>
                        <p>Consulte, edite e remova modalidades cadastradas.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNova}>
                        + Nova Modalidade
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-nome">Nome</label>
                        <input
                            id="filtro-nome"
                            type="text"
                            value={filterNome}
                            onChange={e => setFilterNome(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-sigla">Sigla</label>
                        <input
                            id="filtro-sigla"
                            type="text"
                            value={filterSigla}
                            onChange={e => setFilterSigla(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredModalidades.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando modalidades..."
                        emptyMessage="Nenhuma modalidade encontrada."
                        emptySubMessage={modalidades.length === 0 ? 'Clique em Nova Modalidade para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nome</th>
                                    <th>Sigla</th>
                                    <th>Código MEC</th>
                                    <th>Descrição</th>
                                    <th>Status</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredModalidades.map(m => (
                                    <tr key={m.id}>
                                        <td>{m.nome}</td>
                                        <td>{m.sigla}</td>
                                        <td>{m.codigoMec ?? '—'}</td>
                                        <td>{m.descricao}</td>
                                        <td><StatusPill status={m.status} /></td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(m)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(m.id)}>Excluir</button>
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

export default ModalidadesEnsinoPage;
