import { useEffect, useState, type FormEvent, type ChangeEvent } from 'react';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

type View = 'list' | 'form';

interface EtapaEnsinoRef {
    id: number;
    etapaEnsinoNome: string;
}

interface Disciplina {
    id: number;
    nome: string;
    codigo: string;
    descricao: string;
    ativa: boolean;
    etapasEnsino: EtapaEnsinoRef[];
}

interface EtapaEnsino {
    id: number;
    nome: string;
}

interface DisciplinaForm {
    nome: string;
    codigo: string;
    descricao: string;
    etapasEnsinoIds: number[];
}

const emptyForm: DisciplinaForm = {
    nome: '',
    codigo: '',
    descricao: '',
    etapasEnsinoIds: [],
};

async function readApiError(res: Response): Promise<string> {
    try {
        const body = (await res.json()) as { message?: string };
        return body.message ?? `Erro ${res.status}`;
    } catch {
        return `Erro ${res.status}`;
    }
}

function DisciplinasPage() {
    const [disciplinas, setDisciplinas] = useState<Disciplina[]>([]);
    const [etapasEnsino, setEtapasEnsino] = useState<EtapaEnsino[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<DisciplinaForm>(emptyForm);
    const [editingId, setEditingId] = useState<number | null>(null);

    const [filterNome, setFilterNome] = useState('');
    const [appliedNome, setAppliedNome] = useState('');

    useEffect(() => {
        let cancelled = false;

        async function init() {
            setIsLoading(true);
            setError(null);
            try {
                const [discRes, etapasRes] = await Promise.all([
                    fetch('/api/disciplinas'),
                    fetch('/api/etapasensino'),
                ]);
                if (cancelled) return;

                if (!discRes.ok) throw new Error(await readApiError(discRes));
                if (!etapasRes.ok) throw new Error(await readApiError(etapasRes));

                setDisciplinas((await discRes.json()) as Disciplina[]);
                setEtapasEnsino((await etapasRes.json()) as EtapaEnsino[]);
            } catch (e) {
                if (!cancelled) {
                    setError(e instanceof Error ? e.message : 'Falha ao carregar dados.');
                }
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        init();
        return () => { cancelled = true; };
    }, []);

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedNome(filterNome.trim());
    };

    const handleLimparFiltros = () => {
        setFilterNome('');
        setAppliedNome('');
    };

    const filteredDisciplinas = disciplinas.filter(d => {
        return !appliedNome || d.nome.toLowerCase().includes(appliedNome.toLowerCase());
    });

    const handleNovo = () => {
        setEditingId(null);
        setForm(emptyForm);
        setError(null);
        setView('form');
    };

    const handleEdit = (disciplina: Disciplina) => {
        setEditingId(disciplina.id);
        setForm({
            nome: disciplina.nome,
            codigo: disciplina.codigo,
            descricao: disciplina.descricao,
            etapasEnsinoIds: disciplina.etapasEnsino.map(e => e.id),
        });
        setError(null);
        setView('form');
    };

    const handleFieldChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const { name, value } = e.target;
        setForm(f => ({ ...f, [name]: value }));
    };

    const handleEtapaChange = (e: ChangeEvent<HTMLInputElement>) => {
        const id = parseInt(e.target.value);
        setForm(f => ({
            ...f,
            etapasEnsinoIds: e.target.checked
                ? [...f.etapasEnsinoIds, id]
                : f.etapasEnsinoIds.filter(x => x !== id),
        }));
    };

    const handleCancel = () => {
        setEditingId(null);
        setForm(emptyForm);
        setError(null);
        setView('list');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setIsSaving(true);
        setError(null);

        if (!form.nome.trim()) {
            setError('Nome da disciplina é obrigatório.');
            setIsSaving(false);
            return;
        }

        if (!form.codigo.trim()) {
            setError('Código da disciplina é obrigatório.');
            setIsSaving(false);
            return;
        }

        if (form.etapasEnsinoIds.length === 0) {
            setError('Selecione pelo menos uma etapa de ensino.');
            setIsSaving(false);
            return;
        }

        const body = {
            nome: form.nome.trim(),
            codigo: form.codigo.trim(),
            descricao: form.descricao.trim(),
            etapasEnsinoIds: form.etapasEnsinoIds,
        };

        try {
            const url = editingId !== null ? `/api/disciplinas/${editingId}` : '/api/disciplinas';
            const method = editingId !== null ? 'PUT' : 'POST';
            const res = await fetch(url, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!res.ok) throw new Error(await readApiError(res));

            const saved = (await res.json()) as Disciplina;

            if (editingId !== null) {
                setDisciplinas(prev => prev.map(d => d.id === saved.id ? saved : d));
            } else {
                setDisciplinas(prev => [...prev, saved]);
            }

            setView('list');
            setEditingId(null);
            setForm(emptyForm);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao salvar disciplina.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (id: number) => {
        if (!window.confirm('Tem certeza que deseja excluir esta disciplina?')) return;

        setError(null);
        try {
            const res = await fetch(`/api/disciplinas/${id}`, { method: 'DELETE' });
            if (!res.ok) throw new Error(await readApiError(res));
            setDisciplinas(prev => prev.filter(d => d.id !== id));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao excluir disciplina.');
        }
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId !== null ? 'Editar Disciplina' : 'Nova Disciplina'}</h2>
                            <p>{editingId !== null ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar uma nova disciplina.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="ano-letivo-section-title">Dados da Disciplina</div>

                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="disc-nome">Nome da Disciplina *</label>
                                <input
                                    id="disc-nome"
                                    name="nome"
                                    type="text"
                                    placeholder="Ex: Matemática"
                                    value={form.nome}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="disc-codigo">Código *</label>
                                <input
                                    id="disc-codigo"
                                    name="codigo"
                                    type="text"
                                    placeholder="Ex: MAT"
                                    value={form.codigo}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>
                        </div>

                        <div className="form-field">
                            <label htmlFor="disc-descricao">Descrição</label>
                            <textarea
                                id="disc-descricao"
                                name="descricao"
                                placeholder="Descrição da disciplina..."
                                value={form.descricao}
                                onChange={handleFieldChange}
                                rows={4}
                            />
                        </div>

                        <div className="ano-letivo-section-title">Etapas de Ensino *</div>
                        <div className="checkbox-group">
                            {etapasEnsino.length === 0 ? (
                                <p className="no-data-message">Nenhuma etapa de ensino disponível. Cadastre etapas de ensino primeiro.</p>
                            ) : (
                                etapasEnsino.map(etapa => (
                                    <div key={etapa.id} className="checkbox-field">
                                        <input
                                            id={`etapa-${etapa.id}`}
                                            type="checkbox"
                                            value={etapa.id}
                                            checked={form.etapasEnsinoIds.includes(etapa.id)}
                                            onChange={handleEtapaChange}
                                        />
                                        <label htmlFor={`etapa-${etapa.id}`}>{etapa.nome}</label>
                                    </div>
                                ))
                            )}
                        </div>

                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Disciplina' : 'Salvar Disciplina'}
                            </button>
                            <button type="button" onClick={handleCancel} className="secondary-button cancel-button">
                                Cancelar
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
                        <h2>Disciplinas</h2>
                        <p>Consulte, edite e remova disciplinas/componentes curriculares.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNovo}>
                        + Nova Disciplina
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-nome">Nome da Disciplina</label>
                        <input
                            id="filtro-nome"
                            type="text"
                            value={filterNome}
                            onChange={e => setFilterNome(e.target.value)}
                            className="filter-input"
                            placeholder="Ex: Matemática"
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredDisciplinas.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando disciplinas..."
                        emptyMessage="Nenhuma disciplina encontrada."
                        emptySubMessage={disciplinas.length === 0 ? 'Clique em Nova Disciplina para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nome</th>
                                    <th>Código</th>
                                    <th>Etapas de Ensino</th>
                                    <th>Descrição</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredDisciplinas.map(d => (
                                    <tr key={d.id}>
                                        <td><strong>{d.nome}</strong></td>
                                        <td>{d.codigo}</td>
                                        <td>
                                            <div className="anos-list">
                                                {d.etapasEnsino.map(e => (
                                                    <span key={e.id} className="ano-tag">
                                                        {e.etapaEnsinoNome}
                                                    </span>
                                                ))}
                                            </div>
                                        </td>
                                        <td className="description-cell">{d.descricao || '—'}</td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(d)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(d.id)}>Excluir</button>
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

export default DisciplinasPage;
