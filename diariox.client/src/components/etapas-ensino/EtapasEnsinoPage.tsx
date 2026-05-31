import { useEffect, useState, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

type View = 'list' | 'form';

interface ModalidadeOption {
    id: number;
    nome: string;
    status: string;
}

interface EtapaEnsinoFormState {
    modalidadeEnsinoId: string;
    nome: string;
    sigla: string;
    ordemCronologica: string;
    idadeRecomendada: string;
}

interface EtapaEnsino {
    id: number;
    modalidadeEnsinoId: number;
    modalidadeEnsinoNome: string;
    nome: string;
    sigla: string;
    ordemCronologica: number;
    idadeRecomendada: number | null;
}

const emptyForm: EtapaEnsinoFormState = {
    modalidadeEnsinoId: '',
    nome: '',
    sigla: '',
    ordemCronologica: '',
    idadeRecomendada: '',
};

function EtapasEnsinoPage() {
    const { items: etapas, isLoading, isSaving, error, load, save, remove } =
        useCrudData<EtapaEnsino>('/api/etapasensino');
    const { form, editingId, handleFieldChange, startEdit, clear } =
        useCrudForm<EtapaEnsinoFormState & Record<string, unknown>>(
            emptyForm as EtapaEnsinoFormState & Record<string, unknown>
        );

    const [modalidades, setModalidades] = useState<ModalidadeOption[]>([]);
    const [view, setView] = useState<View>('list');

    const [filterNome, setFilterNome] = useState('');
    const [filterModalidadeId, setFilterModalidadeId] = useState('');
    const [appliedNome, setAppliedNome] = useState('');
    const [appliedModalidadeId, setAppliedModalidadeId] = useState('');

    useEffect(() => {
        void load();
        void fetchModalidades();
    }, []);

    const fetchModalidades = async () => {
        try {
            const res = await fetch('/api/modalidadesensino');
            if (res.ok) {
                const data = (await res.json()) as ModalidadeOption[];
                setModalidades(data.filter(m => m.status === 'ATIVO'));
            }
        } catch { /* ignore */ }
    };

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedNome(filterNome.trim());
        setAppliedModalidadeId(filterModalidadeId);
    };

    const handleLimparFiltros = () => {
        setFilterNome('');
        setFilterModalidadeId('');
        setAppliedNome('');
        setAppliedModalidadeId('');
    };

    const filteredEtapas = etapas.filter(e => {
        const matchNome = !appliedNome || e.nome.toLowerCase().includes(appliedNome.toLowerCase());
        const matchModalidade = !appliedModalidadeId || e.modalidadeEnsinoId === parseInt(appliedModalidadeId);
        return matchNome && matchModalidade;
    });

    const handleNova = () => {
        clear();
        setView('form');
    };

    const handleEdit = (etapa: EtapaEnsino) => {
        startEdit(etapa.id, {
            modalidadeEnsinoId: String(etapa.modalidadeEnsinoId),
            nome: etapa.nome,
            sigla: etapa.sigla,
            ordemCronologica: String(etapa.ordemCronologica),
            idadeRecomendada: etapa.idadeRecomendada != null ? String(etapa.idadeRecomendada) : '',
        });
        setView('form');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        const idadeStr = (form.idadeRecomendada as string).trim();
        const saved = await save(editingId, {
            modalidadeEnsinoId: parseInt(form.modalidadeEnsinoId as string),
            nome: (form.nome as string).trim(),
            sigla: (form.sigla as string).trim(),
            ordemCronologica: parseInt(form.ordemCronologica as string),
            idadeRecomendada: idadeStr ? parseInt(idadeStr) : null,
        });

        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleCancel = () => {
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
                            <h2>{editingId ? 'Editar Etapa de Ensino' : 'Nova Etapa de Ensino'}</h2>
                            <p>{editingId ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar uma nova etapa.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-field form-field-full">
                                <label htmlFor="etapa-modalidade">Modalidade de Ensino</label>
                                <select
                                    id="etapa-modalidade"
                                    name="modalidadeEnsinoId"
                                    value={form.modalidadeEnsinoId as string}
                                    onChange={handleFieldChange}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {modalidades.map(m => (
                                        <option key={m.id} value={String(m.id)}>{m.nome}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="form-field">
                                <label htmlFor="etapa-nome">Nome do Ano/Etapa</label>
                                <input
                                    id="etapa-nome"
                                    name="nome"
                                    value={form.nome as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Ex: 3º Ano"
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="etapa-sigla">Sigla</label>
                                <input
                                    id="etapa-sigla"
                                    name="sigla"
                                    value={form.sigla as string}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Ex: 3ANO"
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="etapa-ordem">Ordem Cronológica</label>
                                <input
                                    id="etapa-ordem"
                                    name="ordemCronologica"
                                    value={form.ordemCronologica as string}
                                    onChange={handleFieldChange}
                                    type="number"
                                    min={1}
                                    placeholder="Ex: 3"
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="etapa-idade">
                                    Idade Recomendada <span className="label-optional">(opcional)</span>
                                </label>
                                <input
                                    id="etapa-idade"
                                    name="idadeRecomendada"
                                    value={form.idadeRecomendada as string}
                                    onChange={handleFieldChange}
                                    type="number"
                                    min={1}
                                    placeholder="Ex: 8"
                                />
                            </div>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Etapa' : 'Salvar Etapa'}
                            </button>
                            <button
                                type="button"
                                onClick={() => clear()}
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
                        <h2>Etapas de Ensino</h2>
                        <p>Consulte, edite e remova etapas cadastradas.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNova}>
                        + Nova Etapa
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-modalidade">Modalidade</label>
                        <select
                            id="filtro-modalidade"
                            value={filterModalidadeId}
                            onChange={e => setFilterModalidadeId(e.target.value)}
                            className="filter-input"
                        >
                            <option value="">Todas</option>
                            {modalidades.map(m => (
                                <option key={m.id} value={String(m.id)}>{m.nome}</option>
                            ))}
                        </select>
                    </div>
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
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredEtapas.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando etapas..."
                        emptyMessage="Nenhuma etapa encontrada."
                        emptySubMessage={etapas.length === 0 ? 'Clique em Nova Etapa para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Modalidade</th>
                                    <th>Nome</th>
                                    <th>Sigla</th>
                                    <th>Ordem</th>
                                    <th>Idade</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredEtapas.map(e => (
                                    <tr key={e.id}>
                                        <td>{e.modalidadeEnsinoNome}</td>
                                        <td>{e.nome}</td>
                                        <td>{e.sigla}</td>
                                        <td>{e.ordemCronologica}</td>
                                        <td>{e.idadeRecomendada != null ? `${e.idadeRecomendada} anos` : '—'}</td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(e)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(e.id)}>Excluir</button>
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

export default EtapasEnsinoPage;
