import { useEffect, useState, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { calculoLabels, formatarNumero, lerNota, substituicaoLabels, type CalculoNotaPeriodo, type SubstituicaoRecuperacao } from './tipos';
import './Notas.css';

type View = 'list' | 'form';

interface RegraEtapa {
    id: number;
    nome: string;
    modalidadeNome: string;
}

interface Regra {
    /** Nulo = regra padrão do sistema. */
    id: number | null;
    nome: string;
    notaMaxima: number;
    mediaAprovacao: number;
    casasDecimais: number;
    calculoNotaPeriodo: CalculoNotaPeriodo;
    permiteRecuperacao: boolean;
    substituicaoRecuperacao: SubstituicaoRecuperacao;
    etapas: RegraEtapa[];
}

interface EtapaEnsino {
    id: number;
    nome: string;
    modalidadeEnsinoNome: string;
}

interface RegraForm {
    nome: string;
    notaMaxima: string;
    mediaAprovacao: string;
    casasDecimais: number;
    calculoNotaPeriodo: CalculoNotaPeriodo;
    permiteRecuperacao: boolean;
    substituicaoRecuperacao: SubstituicaoRecuperacao;
    etapaEnsinoIds: number[];
}

const emptyForm: RegraForm = {
    nome: '',
    notaMaxima: '10',
    mediaAprovacao: '6',
    casasDecimais: 1,
    calculoNotaPeriodo: 'MEDIA_PONDERADA',
    permiteRecuperacao: true,
    substituicaoRecuperacao: 'SUBSTITUI_MEDIA',
    etapaEnsinoIds: [],
};

const escala = (regra: Regra) => `0 a ${formatarNumero(regra.notaMaxima)} · média ${formatarNumero(regra.mediaAprovacao)}`;

function RegrasAvaliacaoPage() {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();
    const [regras, setRegras] = useState<Regra[]>([]);
    const [padrao, setPadrao] = useState<Regra | null>(null);
    const [etapas, setEtapas] = useState<EtapaEnsino[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<RegraForm>(emptyForm);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [versao, setVersao] = useState(0);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            setIsLoading(true);
            try {
                const [regrasRes, padraoRes, etapasRes] = await Promise.all([
                    apiFetch('/api/regras-avaliacao'),
                    apiFetch('/api/regras-avaliacao/padrao'),
                    apiFetch('/api/etapasensino'),
                ]);
                for (const res of [regrasRes, padraoRes, etapasRes])
                    if (!res.ok) throw new Error(await readApiError(res));
                if (cancelled) return;

                setRegras((await regrasRes.json()) as Regra[]);
                setPadrao((await padraoRes.json()) as Regra);
                setEtapas((await etapasRes.json()) as EtapaEnsino[]);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as regras de avaliação.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [versao]);

    // Etapa → regra em que está vinculada (as demais usam a regra padrão do sistema).
    const regraDaEtapa = new Map(regras.flatMap(r => r.etapas.map(e => [e.id, r] as const)));
    const etapasNoPadrao = etapas.filter(e => !regraDaEtapa.has(e.id));

    const handleNova = () => {
        setEditingId(null);
        setForm(emptyForm);
        setError(null);
        setSuccessMessage(null);
        setView('form');
    };

    const handleEditar = (regra: Regra) => {
        setEditingId(regra.id);
        setForm({
            nome: regra.nome,
            notaMaxima: formatarNumero(regra.notaMaxima),
            mediaAprovacao: formatarNumero(regra.mediaAprovacao),
            casasDecimais: regra.casasDecimais,
            calculoNotaPeriodo: regra.calculoNotaPeriodo,
            permiteRecuperacao: regra.permiteRecuperacao,
            substituicaoRecuperacao: regra.substituicaoRecuperacao,
            etapaEnsinoIds: regra.etapas.map(e => e.id),
        });
        setError(null);
        setSuccessMessage(null);
        setView('form');
    };

    const handleCancelar = () => {
        setEditingId(null);
        setForm(emptyForm);
        setError(null);
        setView('list');
    };

    const toggleEtapa = (id: number, marcada: boolean) => setForm(f => ({
        ...f,
        etapaEnsinoIds: marcada ? [...f.etapaEnsinoIds, id] : f.etapaEnsinoIds.filter(x => x !== id),
    }));

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const notaMaxima = lerNota(form.notaMaxima);
        const mediaAprovacao = lerNota(form.mediaAprovacao);
        if (!form.nome.trim()) {
            setError('Informe o nome da regra de avaliação.');
            return;
        }
        if (notaMaxima === null || Number.isNaN(notaMaxima) || mediaAprovacao === null || Number.isNaN(mediaAprovacao)) {
            setError('Informe a nota máxima e a média para aprovação (números com até duas casas decimais).');
            return;
        }

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch(editingId !== null ? `/api/regras-avaliacao/${editingId}` : '/api/regras-avaliacao', {
                method: editingId !== null ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ...form, nome: form.nome.trim(), notaMaxima, mediaAprovacao }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccessMessage(editingId !== null ? 'Regra de avaliação atualizada com sucesso!' : 'Regra de avaliação cadastrada com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
            setView('list');
            setEditingId(null);
            setForm(emptyForm);
            setVersao(v => v + 1);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao salvar a regra de avaliação.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleExcluir = async (regra: Regra) => {
        const confirmed = await confirm({
            title: 'Excluir regra de avaliação',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: (
                <>
                    <p>Deseja excluir a regra <strong>{regra.nome}</strong>?</p>
                    {regra.etapas.length > 0 && (
                        <p>
                            As etapas vinculadas ({regra.etapas.map(e => e.nome).join(', ')}) passam a usar a regra padrão do sistema
                            e as médias das notas já lançadas nelas serão recalculadas.
                        </p>
                    )}
                </>
            ),
        });
        if (!confirmed || regra.id === null) return;

        setError(null);
        try {
            const response = await apiFetch(`/api/regras-avaliacao/${regra.id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(await readApiError(response));
            setSuccessMessage('Regra de avaliação excluída com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
            setVersao(v => v + 1);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao excluir a regra de avaliação.');
        }
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId !== null ? 'Editar Regra de Avaliação' : 'Nova Regra de Avaliação'}</h2>
                            <p>Defina a escala das notas, a média para aprovação e como a nota de cada período é calculada.</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancelar}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="ano-letivo-section-title">Dados da Regra</div>
                        <div className="form-grid">
                            <div className="form-field form-field-full">
                                <label htmlFor="regra-nome">Nome *</label>
                                <input
                                    id="regra-nome"
                                    type="text"
                                    maxLength={100}
                                    placeholder="Ex.: Ensino Fundamental II — média 6"
                                    value={form.nome}
                                    onChange={e => setForm(f => ({ ...f, nome: e.target.value }))}
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="regra-maxima">Nota máxima *</label>
                                <input
                                    id="regra-maxima"
                                    type="text"
                                    inputMode="decimal"
                                    value={form.notaMaxima}
                                    onChange={e => setForm(f => ({ ...f, notaMaxima: e.target.value }))}
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="regra-media">Média para aprovação *</label>
                                <input
                                    id="regra-media"
                                    type="text"
                                    inputMode="decimal"
                                    value={form.mediaAprovacao}
                                    onChange={e => setForm(f => ({ ...f, mediaAprovacao: e.target.value }))}
                                    required
                                />
                            </div>
                            <div className="form-field">
                                <label htmlFor="regra-calculo">Nota do período</label>
                                <select
                                    id="regra-calculo"
                                    value={form.calculoNotaPeriodo}
                                    onChange={e => setForm(f => ({ ...f, calculoNotaPeriodo: e.target.value as CalculoNotaPeriodo }))}
                                >
                                    {(Object.keys(calculoLabels) as CalculoNotaPeriodo[]).map(c => (
                                        <option key={c} value={c}>{calculoLabels[c]}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="form-field">
                                <label htmlFor="regra-casas">Casas decimais das médias</label>
                                <select
                                    id="regra-casas"
                                    value={form.casasDecimais}
                                    onChange={e => setForm(f => ({ ...f, casasDecimais: Number(e.target.value) }))}
                                >
                                    <option value={0}>Nenhuma (ex.: 7)</option>
                                    <option value={1}>Uma (ex.: 7,5)</option>
                                    <option value={2}>Duas (ex.: 7,25)</option>
                                </select>
                            </div>
                        </div>

                        <div className="checkbox-field">
                            <input
                                id="regra-recuperacao"
                                type="checkbox"
                                checked={form.permiteRecuperacao}
                                onChange={e => setForm(f => ({ ...f, permiteRecuperacao: e.target.checked }))}
                            />
                            <label htmlFor="regra-recuperacao">
                                Recuperação por período (paralela/bimestral)
                            </label>
                        </div>

                        {form.permiteRecuperacao && (
                            <div className="form-field">
                                <label htmlFor="regra-substituicao">Regra de substituição da nota</label>
                                <select
                                    id="regra-substituicao"
                                    value={form.substituicaoRecuperacao}
                                    onChange={e => setForm(f => ({ ...f, substituicaoRecuperacao: e.target.value as SubstituicaoRecuperacao }))}
                                >
                                    {(Object.keys(substituicaoLabels) as SubstituicaoRecuperacao[]).map(valor => (
                                        <option key={valor} value={valor}>{substituicaoLabels[valor]}</option>
                                    ))}
                                </select>
                                <span className="field-hint">
                                    Aplicada automaticamente só quando a nota da recuperação é superior à média das avaliações do período.
                                </span>
                            </div>
                        )}

                        <p className="field-hint">
                            {form.calculoNotaPeriodo === 'SOMA'
                                ? 'Soma: cada avaliação vale alguns pontos (ex.: prova 6 + trabalho 4) e a nota do período é a soma, até a nota máxima.'
                                : 'Média ponderada: cada avaliação recebe nota na escala inteira e um peso; a nota do período é a média ponderada.'}
                            {' '}A média do ano é a média aritmética das notas dos períodos. Alterar a regra recalcula as médias das notas já lançadas.
                        </p>

                        <div className="ano-letivo-section-title">Etapas de ensino que usam esta regra</div>
                        <div className="checkbox-group">
                            {etapas.length === 0 ? (
                                <p className="no-data-message">Nenhuma etapa de ensino cadastrada.</p>
                            ) : etapas.map(etapa => {
                                const atual = regraDaEtapa.get(etapa.id);
                                const emOutra = atual && atual.id !== editingId;
                                return (
                                    <div key={etapa.id} className="checkbox-field">
                                        <input
                                            id={`regra-etapa-${etapa.id}`}
                                            type="checkbox"
                                            checked={form.etapaEnsinoIds.includes(etapa.id)}
                                            onChange={e => toggleEtapa(etapa.id, e.target.checked)}
                                        />
                                        <label htmlFor={`regra-etapa-${etapa.id}`}>
                                            {etapa.nome} <small>({etapa.modalidadeEnsinoNome})</small>
                                            {emOutra && <small className="regra-etapa-aviso"> · hoje em "{atual.nome}"</small>}
                                        </label>
                                    </div>
                                );
                            })}
                        </div>
                        <p className="field-hint">Uma etapa marcada aqui sai da regra em que estava. Etapas sem regra usam a regra padrão do sistema.</p>

                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {isSaving ? 'Salvando...' : editingId !== null ? 'Atualizar Regra' : 'Salvar Regra'}
                            </button>
                            <button type="button" onClick={handleCancelar} className="secondary-button cancel-button">
                                Cancelar
                            </button>
                        </div>
                    </form>
                </div>
                {confirmDialog}
            </div>
        );
    }

    return (
        <div className="school-page">
            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Regras de Avaliação</h2>
                        <p>Escala das notas, média para aprovação e cálculo da nota do período, por etapa de ensino.</p>
                    </div>
                    {can('regras-avaliacao.criar') && (
                        <button type="button" className="primary-button" onClick={handleNova}>
                            + Nova Regra
                        </button>
                    )}
                </div>

                <FeedbackMessage message={error} />
                <FeedbackMessage message={successMessage} type="success" />

                {isLoading ? (
                    <EmptyState loading loadingMessage="Carregando regras de avaliação..." emptyMessage="" />
                ) : (
                    <>
                        {padrao && (
                            <div className="regra-padrao">
                                <strong>Regra padrão do sistema</strong>
                                <span>
                                    {escala(padrao)} · {calculoLabels[padrao.calculoNotaPeriodo].toLowerCase()}
                                    {padrao.permiteRecuperacao && ' · com recuperação por período'}
                                </span>
                                <span className="field-hint">
                                    {etapasNoPadrao.length === 0
                                        ? 'Todas as etapas têm regra própria.'
                                        : `Usada por: ${etapasNoPadrao.map(e => e.nome).join(', ')}.`}
                                </span>
                            </div>
                        )}

                        {regras.length === 0 ? (
                            <EmptyState
                                emptyMessage="Nenhuma regra própria cadastrada."
                                emptySubMessage="Enquanto isso, todas as etapas usam a regra padrão do sistema."
                            />
                        ) : (
                            <div className="table-responsive">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Nome</th>
                                            <th>Escala</th>
                                            <th>Nota do período</th>
                                            <th>Recuperação</th>
                                            <th>Etapas</th>
                                            <th>Ações</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {regras.map(regra => (
                                            <tr key={regra.id}>
                                                <td><strong>{regra.nome}</strong></td>
                                                <td className="nowrap-cell">{escala(regra)}</td>
                                                <td>{calculoLabels[regra.calculoNotaPeriodo]}</td>
                                                <td>{regra.permiteRecuperacao ? substituicaoLabels[regra.substituicaoRecuperacao] : 'Não'}</td>
                                                <td>
                                                    {regra.etapas.length === 0 ? '—' : (
                                                        <div className="anos-list">
                                                            {regra.etapas.map(e => <span key={e.id} className="ano-tag">{e.nome}</span>)}
                                                        </div>
                                                    )}
                                                </td>
                                                <td>
                                                    <div className="action-group">
                                                        {can('regras-avaliacao.editar') && (
                                                            <button type="button" className="table-action-button" onClick={() => handleEditar(regra)}>Editar</button>
                                                        )}
                                                        {can('regras-avaliacao.excluir') && (
                                                            <button type="button" className="table-action-button danger" onClick={() => void handleExcluir(regra)}>Excluir</button>
                                                        )}
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </>
                )}
            </div>
            {confirmDialog}
        </div>
    );
}

export default RegrasAvaliacaoPage;
