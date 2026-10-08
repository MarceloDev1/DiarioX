import { useEffect, useState, type FormEvent, type ChangeEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { usePermissoes } from '../../hooks/usePermissoes';

type View = 'list' | 'form';

interface HabilidadeBncc {
    id: number;
    codigo: string;
    descricao: string;
    disciplinaId: number;
    disciplinaNome: string;
    ativa: boolean;
    etapasEnsino: { id: number; nome: string }[];
}

interface Opcao {
    id: number;
    nome: string;
}

interface HabilidadeForm {
    codigo: string;
    descricao: string;
    disciplinaId: string;
    etapasEnsinoIds: number[];
    ativa: boolean;
}

const emptyForm: HabilidadeForm = {
    codigo: '',
    descricao: '',
    disciplinaId: '',
    etapasEnsinoIds: [],
    ativa: true,
};

function HabilidadesBnccPage() {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();
    const [habilidades, setHabilidades] = useState<HabilidadeBncc[]>([]);
    const [disciplinas, setDisciplinas] = useState<Opcao[]>([]);
    const [etapasEnsino, setEtapasEnsino] = useState<Opcao[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<HabilidadeForm>(emptyForm);
    const [editingId, setEditingId] = useState<number | null>(null);

    const [filterTexto, setFilterTexto] = useState('');
    const [filterDisciplina, setFilterDisciplina] = useState('');
    const [applied, setApplied] = useState({ texto: '', disciplina: '' });

    useEffect(() => {
        let cancelled = false;

        async function init() {
            setIsLoading(true);
            setError(null);
            try {
                const [habRes, discRes, etapasRes] = await Promise.all([
                    apiFetch('/api/habilidadesbncc'),
                    apiFetch('/api/disciplinas'),
                    apiFetch('/api/etapasensino'),
                ]);
                if (cancelled) return;

                if (!habRes.ok) throw new Error(await readApiError(habRes));
                if (!discRes.ok) throw new Error(await readApiError(discRes));
                if (!etapasRes.ok) throw new Error(await readApiError(etapasRes));

                setHabilidades((await habRes.json()) as HabilidadeBncc[]);
                setDisciplinas((await discRes.json()) as Opcao[]);
                setEtapasEnsino((await etapasRes.json()) as Opcao[]);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar dados.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void init();
        return () => { cancelled = true; };
    }, []);

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setApplied({ texto: filterTexto.trim().toLowerCase(), disciplina: filterDisciplina });
    };

    const handleLimparFiltros = () => {
        setFilterTexto('');
        setFilterDisciplina('');
        setApplied({ texto: '', disciplina: '' });
    };

    const filtered = habilidades.filter(h =>
        (!applied.texto || h.codigo.toLowerCase().includes(applied.texto) || h.descricao.toLowerCase().includes(applied.texto)) &&
        (!applied.disciplina || String(h.disciplinaId) === applied.disciplina));

    const handleNovo = () => {
        setEditingId(null);
        setForm(emptyForm);
        setError(null);
        setView('form');
    };

    const handleEdit = (h: HabilidadeBncc) => {
        setEditingId(h.id);
        setForm({
            codigo: h.codigo,
            descricao: h.descricao,
            disciplinaId: String(h.disciplinaId),
            etapasEnsinoIds: h.etapasEnsino.map(e => e.id),
            ativa: h.ativa,
        });
        setError(null);
        setView('form');
    };

    const handleFieldChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
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
        setError(null);

        if (!form.codigo.trim()) return setError('Código da habilidade é obrigatório.');
        if (!form.descricao.trim()) return setError('Descrição da habilidade é obrigatória.');
        if (!form.disciplinaId) return setError('Selecione a disciplina (componente curricular).');
        if (form.etapasEnsinoIds.length === 0) return setError('Selecione pelo menos uma etapa de ensino.');

        setIsSaving(true);
        try {
            const res = await apiFetch(editingId !== null ? `/api/habilidadesbncc/${editingId}` : '/api/habilidadesbncc', {
                method: editingId !== null ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    codigo: form.codigo.trim(),
                    descricao: form.descricao.trim(),
                    disciplinaId: Number(form.disciplinaId),
                    etapasEnsinoIds: form.etapasEnsinoIds,
                    ativa: form.ativa,
                }),
            });
            if (!res.ok) throw new Error(await readApiError(res));

            const saved = (await res.json()) as HabilidadeBncc;
            setHabilidades(prev => editingId !== null
                ? prev.map(h => h.id === saved.id ? saved : h)
                : [...prev, saved].sort((a, b) => a.codigo.localeCompare(b.codigo)));

            setView('list');
            setEditingId(null);
            setForm(emptyForm);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao salvar habilidade.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (h: HabilidadeBncc) => {
        const confirmed = await confirm({
            title: 'Excluir habilidade',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: (
                <>
                    <p>Tem certeza que deseja excluir a habilidade <strong>{h.codigo}</strong>?</p>
                    <p>Habilidades já usadas em conteúdos ministrados não podem ser excluídas; inative-as.</p>
                </>
            ),
        });
        if (!confirmed) return;

        setError(null);
        try {
            const res = await apiFetch(`/api/habilidadesbncc/${h.id}`, { method: 'DELETE' });
            if (!res.ok) throw new Error(await readApiError(res));
            setHabilidades(prev => prev.filter(x => x.id !== h.id));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao excluir habilidade.');
        }
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId !== null ? 'Editar Habilidade' : 'Nova Habilidade'}</h2>
                            <p>Habilidade da BNCC sugerida no registro do conteúdo ministrado das etapas e da disciplina selecionadas.</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="ano-letivo-section-title">Dados da Habilidade</div>

                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="bncc-codigo">Código *</label>
                                <input
                                    id="bncc-codigo"
                                    name="codigo"
                                    type="text"
                                    maxLength={20}
                                    placeholder="Ex: EF06MA01"
                                    value={form.codigo}
                                    onChange={handleFieldChange}
                                    required
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="bncc-disciplina">Disciplina (componente curricular) *</label>
                                <select
                                    id="bncc-disciplina"
                                    name="disciplinaId"
                                    value={form.disciplinaId}
                                    onChange={handleFieldChange}
                                    required
                                >
                                    <option value="">Selecione...</option>
                                    {disciplinas.map(d => <option key={d.id} value={d.id}>{d.nome}</option>)}
                                </select>
                            </div>
                        </div>

                        <div className="form-field">
                            <label htmlFor="bncc-descricao">Descrição *</label>
                            <textarea
                                id="bncc-descricao"
                                name="descricao"
                                maxLength={1000}
                                placeholder="Texto oficial da habilidade..."
                                value={form.descricao}
                                onChange={handleFieldChange}
                                rows={4}
                                required
                            />
                        </div>

                        <div className="ano-letivo-section-title">Etapas de Ensino (anos) *</div>
                        <div className="checkbox-group">
                            {etapasEnsino.length === 0 ? (
                                <p className="no-data-message">Nenhuma etapa de ensino disponível. Cadastre etapas de ensino primeiro.</p>
                            ) : (
                                etapasEnsino.map(etapa => (
                                    <div key={etapa.id} className="checkbox-field">
                                        <input
                                            id={`bncc-etapa-${etapa.id}`}
                                            type="checkbox"
                                            value={etapa.id}
                                            checked={form.etapasEnsinoIds.includes(etapa.id)}
                                            onChange={handleEtapaChange}
                                        />
                                        <label htmlFor={`bncc-etapa-${etapa.id}`}>{etapa.nome}</label>
                                    </div>
                                ))
                            )}
                        </div>

                        <div className="checkbox-field">
                            <input
                                id="bncc-ativa"
                                type="checkbox"
                                checked={form.ativa}
                                onChange={e => setForm(f => ({ ...f, ativa: e.target.checked }))}
                            />
                            <label htmlFor="bncc-ativa">Ativa (sugerida no registro de conteúdo)</label>
                        </div>

                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Habilidade' : 'Salvar Habilidade'}
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
                        <h2>Habilidades da BNCC</h2>
                        <p>Cadastre as habilidades da Base Nacional Comum Curricular sugeridas no conteúdo ministrado.</p>
                    </div>
                    {can('habilidades-bncc.criar') && (
                        <button type="button" className="primary-button" onClick={handleNovo}>
                            + Nova Habilidade
                        </button>
                    )}
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-bncc-texto">Código ou descrição</label>
                        <input
                            id="filtro-bncc-texto"
                            type="text"
                            value={filterTexto}
                            onChange={e => setFilterTexto(e.target.value)}
                            className="filter-input"
                            placeholder="Ex: EF06MA01"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-bncc-disciplina">Disciplina</label>
                        <select
                            id="filtro-bncc-disciplina"
                            value={filterDisciplina}
                            onChange={e => setFilterDisciplina(e.target.value)}
                            className="filter-input"
                        >
                            <option value="">Todas</option>
                            {disciplinas.map(d => <option key={d.id} value={d.id}>{d.nome}</option>)}
                        </select>
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filtered.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando habilidades..."
                        emptyMessage="Nenhuma habilidade encontrada."
                        emptySubMessage={habilidades.length === 0 ? 'Clique em Nova Habilidade para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Código</th>
                                    <th>Descrição</th>
                                    <th>Disciplina</th>
                                    <th>Etapas de Ensino</th>
                                    <th>Situação</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filtered.map(h => (
                                    <tr key={h.id}>
                                        <td className="nowrap-cell"><strong>{h.codigo}</strong></td>
                                        <td className="description-cell" title={h.descricao}>{h.descricao}</td>
                                        <td>{h.disciplinaNome}</td>
                                        <td>
                                            <div className="anos-list">
                                                {h.etapasEnsino.map(e => <span key={e.id} className="ano-tag">{e.nome}</span>)}
                                            </div>
                                        </td>
                                        <td>
                                            <span className={`status-pill ${h.ativa ? 'status-active' : 'status-inactive'}`}>
                                                {h.ativa ? 'Ativa' : 'Inativa'}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="action-group">
                                                {can('habilidades-bncc.editar') && <button type="button" className="table-action-button" onClick={() => handleEdit(h)}>Editar</button>}
                                                {can('habilidades-bncc.excluir') && <button type="button" className="table-action-button danger" onClick={() => handleDelete(h)}>Excluir</button>}
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

export default HabilidadesBnccPage;
