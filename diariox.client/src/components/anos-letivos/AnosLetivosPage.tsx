import { useEffect, useState, type FormEvent, type ChangeEvent } from 'react';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

type View = 'list' | 'form';

interface PeriodoAvaliativo {
    id: number;
    anoLetivoId: number;
    nome: string;
    numero: number;
    dataInicio: string;
    dataTermino: string;
}

interface AnoLetivo {
    id: number;
    anoReferencia: number;
    dataInicio: string;
    dataTermino: string;
    tipoPeriodo: string;
    periodos: PeriodoAvaliativo[];
}

interface PeriodoForm {
    nome: string;
    numero: number;
    dataInicio: string;
    dataTermino: string;
}

interface AnoLetivoForm {
    anoReferencia: string;
    dataInicio: string;
    dataTermino: string;
    tipoPeriodo: string;
}

const TIPO_CONFIG: Record<string, { count: number; nomeBase: string }> = {
    BIMESTRAL: { count: 4, nomeBase: 'Bimestre' },
    TRIMESTRAL: { count: 3, nomeBase: 'Trimestre' },
    SEMESTRAL: { count: 2, nomeBase: 'Semestre' },
};

const ORDINALS = ['1º', '2º', '3º', '4º'];

function generatePeriodos(tipoPeriodo: string, existing?: PeriodoAvaliativo[]): PeriodoForm[] {
    const config = TIPO_CONFIG[tipoPeriodo];
    if (!config) return [];
    return Array.from({ length: config.count }, (_, i) => ({
        nome: `${ORDINALS[i]} ${config.nomeBase}`,
        numero: i + 1,
        dataInicio: existing?.[i]?.dataInicio ?? '',
        dataTermino: existing?.[i]?.dataTermino ?? '',
    }));
}

const emptyForm: AnoLetivoForm = {
    anoReferencia: '',
    dataInicio: '',
    dataTermino: '',
    tipoPeriodo: '',
};

async function readApiError(res: Response): Promise<string> {
    try {
        const body = (await res.json()) as { message?: string };
        return body.message ?? `Erro ${res.status}`;
    } catch {
        return `Erro ${res.status}`;
    }
}

function AnosLetivosPage() {
    const [anos, setAnos] = useState<AnoLetivo[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<AnoLetivoForm>(emptyForm);
    const [periodos, setPeriodos] = useState<PeriodoForm[]>([]);
    const [editingId, setEditingId] = useState<number | null>(null);

    const [filterAno, setFilterAno] = useState('');
    const [appliedAno, setAppliedAno] = useState('');

    useEffect(() => {
        void load();
    }, []);

    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const res = await fetch('/api/anosletivos');
            if (!res.ok) throw new Error(await readApiError(res));
            setAnos((await res.json()) as AnoLetivo[]);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao carregar dados.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedAno(filterAno.trim());
    };

    const handleLimparFiltros = () => {
        setFilterAno('');
        setAppliedAno('');
    };

    const filteredAnos = anos.filter(a => {
        return !appliedAno || String(a.anoReferencia).includes(appliedAno);
    });

    const handleNovo = () => {
        setEditingId(null);
        setForm(emptyForm);
        setPeriodos([]);
        setError(null);
        setView('form');
    };

    const handleEdit = (ano: AnoLetivo) => {
        setEditingId(ano.id);
        setForm({
            anoReferencia: String(ano.anoReferencia),
            dataInicio: ano.dataInicio,
            dataTermino: ano.dataTermino,
            tipoPeriodo: ano.tipoPeriodo,
        });
        setPeriodos(generatePeriodos(ano.tipoPeriodo, ano.periodos));
        setError(null);
        setView('form');
    };

    const handleFieldChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = e.target;
        setForm(f => ({ ...f, [name]: value }));
    };

    const handleTipoPeriodoChange = (e: ChangeEvent<HTMLSelectElement>) => {
        const newTipo = e.target.value;
        setForm(f => ({ ...f, tipoPeriodo: newTipo }));
        setPeriodos(generatePeriodos(newTipo));
    };

    const handlePeriodoChange = (index: number, field: 'dataInicio' | 'dataTermino', value: string) => {
        setPeriodos(prev => prev.map((p, i) => i === index ? { ...p, [field]: value } : p));
    };

    const handleCancel = () => {
        setEditingId(null);
        setForm(emptyForm);
        setPeriodos([]);
        setError(null);
        setView('list');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setIsSaving(true);
        setError(null);

        const body = {
            anoReferencia: parseInt(form.anoReferencia),
            dataInicio: form.dataInicio,
            dataTermino: form.dataTermino,
            tipoPeriodo: form.tipoPeriodo,
            periodos: periodos.map(p => ({
                nome: p.nome,
                numero: p.numero,
                dataInicio: p.dataInicio,
                dataTermino: p.dataTermino,
            })),
        };

        try {
            const url = editingId !== null ? `/api/anosletivos/${editingId}` : '/api/anosletivos';
            const method = editingId !== null ? 'PUT' : 'POST';
            const res = await fetch(url, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });

            if (!res.ok) throw new Error(await readApiError(res));

            const saved = (await res.json()) as AnoLetivo;

            if (editingId !== null) {
                setAnos(prev => prev.map(a => a.id === saved.id ? saved : a));
            } else {
                setAnos(prev => [...prev, saved]);
            }

            setView('list');
            setEditingId(null);
            setForm(emptyForm);
            setPeriodos([]);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao salvar.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (id: number) => {
        setError(null);
        try {
            const res = await fetch(`/api/anosletivos/${id}`, { method: 'DELETE' });
            if (!res.ok) throw new Error(await readApiError(res));
            setAnos(prev => prev.filter(a => a.id !== id));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao excluir.');
        }
    };

    const formatDate = (d: string) => {
        if (!d) return '—';
        const [y, m, day] = d.split('-');
        return `${day}/${m}/${y}`;
    };

    const tipoPeriodoLabel = (tipo: string) => {
        const labels: Record<string, string> = {
            BIMESTRAL: 'Bimestral',
            TRIMESTRAL: 'Trimestral',
            SEMESTRAL: 'Semestral',
        };
        return labels[tipo] ?? tipo;
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId !== null ? 'Editar Ano Letivo' : 'Novo Ano Letivo'}</h2>
                            <p>{editingId !== null ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar um novo ano letivo.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="ano-letivo-section-title">Dados Gerais</div>

                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="al-ano">Ano de Referência</label>
                                <input
                                    id="al-ano"
                                    name="anoReferencia"
                                    type="number"
                                    min={2000}
                                    max={2100}
                                    placeholder="Ex: 2026"
                                    value={form.anoReferencia}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="al-tipo">Tipo de Período</label>
                                <select
                                    id="al-tipo"
                                    name="tipoPeriodo"
                                    value={form.tipoPeriodo}
                                    onChange={handleTipoPeriodoChange}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    <option value="BIMESTRAL">Bimestral</option>
                                    <option value="TRIMESTRAL">Trimestral</option>
                                    <option value="SEMESTRAL">Semestral</option>
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="al-inicio">Data de Início do Ano Letivo</label>
                                <input
                                    id="al-inicio"
                                    name="dataInicio"
                                    type="date"
                                    value={form.dataInicio}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="al-termino">Data de Término do Ano Letivo</label>
                                <input
                                    id="al-termino"
                                    name="dataTermino"
                                    type="date"
                                    value={form.dataTermino}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>
                        </div>

                        {periodos.length > 0 && (
                            <div className="periodos-section">
                                <div className="ano-letivo-section-title">Períodos Avaliativos</div>
                                <div className="periodos-grid">
                                    {periodos.map((p, i) => (
                                        <div key={p.numero} className="periodo-block">
                                            <div className="periodo-block-header">{p.nome}</div>
                                            <div className="form-grid">
                                                <div className="form-field">
                                                    <label htmlFor={`p-inicio-${i}`}>Data de Início</label>
                                                    <input
                                                        id={`p-inicio-${i}`}
                                                        type="date"
                                                        value={p.dataInicio}
                                                        onChange={e => handlePeriodoChange(i, 'dataInicio', e.target.value)}
                                                        required
                                                    />
                                                </div>
                                                <div className="form-field">
                                                    <label htmlFor={`p-termino-${i}`}>Data de Término</label>
                                                    <input
                                                        id={`p-termino-${i}`}
                                                        type="date"
                                                        value={p.dataTermino}
                                                        onChange={e => handlePeriodoChange(i, 'dataTermino', e.target.value)}
                                                        required
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}

                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Ano Letivo' : 'Salvar Ano Letivo'}
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
                        <h2>Anos Letivos</h2>
                        <p>Consulte, edite e remova anos letivos cadastrados.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNovo}>
                        + Novo Ano Letivo
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-ano">Ano de Referência</label>
                        <input
                            id="filtro-ano"
                            type="text"
                            value={filterAno}
                            onChange={e => setFilterAno(e.target.value)}
                            className="filter-input"
                            placeholder="Ex: 2026"
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredAnos.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando anos letivos..."
                        emptyMessage="Nenhum ano letivo encontrado."
                        emptySubMessage={anos.length === 0 ? 'Clique em Novo Ano Letivo para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Ano</th>
                                    <th>Início</th>
                                    <th>Término</th>
                                    <th>Tipo de Período</th>
                                    <th>Períodos</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredAnos.map(a => (
                                    <tr key={a.id}>
                                        <td><strong>{a.anoReferencia}</strong></td>
                                        <td>{formatDate(a.dataInicio)}</td>
                                        <td>{formatDate(a.dataTermino)}</td>
                                        <td>{tipoPeriodoLabel(a.tipoPeriodo)}</td>
                                        <td>
                                            <div className="periodos-list">
                                                {a.periodos.map(p => (
                                                    <span key={p.id} className="periodo-tag">
                                                        {p.nome}: {formatDate(p.dataInicio)} – {formatDate(p.dataTermino)}
                                                    </span>
                                                ))}
                                            </div>
                                        </td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(a)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(a.id)}>Excluir</button>
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

export default AnosLetivosPage;
