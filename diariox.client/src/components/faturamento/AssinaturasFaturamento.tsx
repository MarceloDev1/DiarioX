import { useEffect, useState, type FormEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { apiFetch, readApiError } from '../../utils/api';
import { buscarEnderecoPorCep, formatEnderecoCep } from '../../utils/cep';
import { formatCep, formatCnpj, formatCpf, formatTelefone } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import {
    classeSituacao, descreverPlano, formatarData, formatarMoeda, situacaoAssinaturaLabels, situacaoFinanceiraLabels,
    type Assinatura, type AssinaturaResumo, type Fatura, type Plano, type Resultado, type SituacaoAssinatura,
} from './tipos';

interface AssinaturasFaturamentoProps {
    onAbrirFaturas: (tenantId: number) => void;
}

interface AssinaturaForm {
    planoId: string;
    situacao: SituacaoAssinatura;
    dataInicio: string;
    testeAte: string;
    diaVencimento: string;
    descontoPercentual: string;
    razaoSocial: string;
    cpfCnpj: string;
    email: string;
    telefone: string;
    cep: string;
    endereco: string;
    numero: string;
    complemento: string;
    bairro: string;
}

const hojeIso = () => new Date().toLocaleDateString('sv-SE');

const formatarDocumento = (valor: string) => {
    const digitos = valor.replace(/\D/g, '');
    return digitos.length > 11 ? formatCnpj(digitos) : formatCpf(digitos);
};

function formDe(assinatura: Assinatura | null, planos: Plano[], instituicao: string): AssinaturaForm {
    if (!assinatura) {
        return {
            planoId: String(planos.find(p => p.ativo)?.id ?? ''), situacao: 'ATIVA', dataInicio: hojeIso(), testeAte: '',
            diaVencimento: '10', descontoPercentual: '0', razaoSocial: instituicao, cpfCnpj: '', email: '', telefone: '',
            cep: '', endereco: '', numero: '', complemento: '', bairro: '',
        };
    }

    return {
        planoId: String(assinatura.planoId), situacao: assinatura.situacao, dataInicio: assinatura.dataInicio,
        testeAte: assinatura.testeAte ?? '', diaVencimento: String(assinatura.diaVencimento),
        descontoPercentual: String(assinatura.descontoPercentual), razaoSocial: assinatura.razaoSocial,
        cpfCnpj: formatarDocumento(assinatura.cpfCnpj), email: assinatura.email,
        telefone: assinatura.telefone ? formatTelefone(assinatura.telefone) : '', cep: assinatura.cep ? formatCep(assinatura.cep) : '',
        endereco: assinatura.endereco ?? '', numero: assinatura.numero ?? '', complemento: assinatura.complemento ?? '',
        bairro: assinatura.bairro ?? '',
    };
}

function AssinaturasFaturamento({ onAbrirFaturas }: AssinaturasFaturamentoProps) {
    const { confirm, confirmDialog } = useConfirm();
    const [linhas, setLinhas] = useState<AssinaturaResumo[] | null>(null);
    const [planos, setPlanos] = useState<Plano[]>([]);
    const [versao, setVersao] = useState(0);
    const [editando, setEditando] = useState<{ linha: AssinaturaResumo; form: AssinaturaForm } | null>(null);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const [assinaturasRes, planosRes] = await Promise.all([
                    apiFetch('/api/faturamento/assinaturas'),
                    apiFetch('/api/faturamento/planos'),
                ]);
                if (!assinaturasRes.ok) throw new Error(await readApiError(assinaturasRes));
                if (!planosRes.ok) throw new Error(await readApiError(planosRes));
                const [assinaturas, listaPlanos] = [
                    (await assinaturasRes.json()) as AssinaturaResumo[],
                    (await planosRes.json()) as Plano[],
                ];
                if (cancelled) return;
                setLinhas(assinaturas);
                setPlanos(listaPlanos);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as assinaturas.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [versao]);

    const abrirForm = async (linha: AssinaturaResumo) => {
        setError(null);
        setSuccess(null);
        let assinatura: Assinatura | null = null;
        if (linha.assinaturaId) {
            const response = await apiFetch(`/api/faturamento/assinaturas/${linha.tenantId}`);
            if (!response.ok) {
                setError(await readApiError(response));
                return;
            }
            assinatura = ((await response.json()) as Resultado<Assinatura>).data;
        }
        setEditando({ linha, form: formDe(assinatura, planos, linha.instituicao) });
    };

    const setCampo = (campo: keyof AssinaturaForm, valor: string) =>
        setEditando(atual => atual && { ...atual, form: { ...atual.form, [campo]: valor } });

    const handleCepBlur = async () => {
        if (!editando) return;
        try {
            const endereco = await buscarEnderecoPorCep(editando.form.cep);
            if (!endereco) return;
            setEditando(atual => atual && {
                ...atual,
                form: {
                    ...atual.form,
                    endereco: atual.form.endereco || formatEnderecoCep(endereco),
                    bairro: atual.form.bairro || endereco.bairro,
                },
            });
        } catch {
            // Consulta de CEP é só uma conveniência: o preenchimento manual continua valendo.
        }
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!editando) return;
        const { form, linha } = editando;

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch(`/api/faturamento/assinaturas/${linha.tenantId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    planoId: Number(form.planoId),
                    situacao: form.situacao,
                    dataInicio: form.dataInicio || null,
                    testeAte: form.testeAte || null,
                    diaVencimento: Number(form.diaVencimento),
                    descontoPercentual: Number(form.descontoPercentual.replace(',', '.')) || 0,
                    razaoSocial: form.razaoSocial,
                    cpfCnpj: form.cpfCnpj,
                    email: form.email,
                    telefone: form.telefone || null,
                    cep: form.cep || null,
                    endereco: form.endereco || null,
                    numero: form.numero || null,
                    complemento: form.complemento || null,
                    bairro: form.bairro || null,
                }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccess(((await response.json()) as Resultado<Assinatura>).message);
            setEditando(null);
            setVersao(v => v + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar a assinatura.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleGerarFatura = async (linha: AssinaturaResumo) => {
        const confirmado = await confirm({
            title: 'Gerar fatura',
            confirmLabel: 'Gerar',
            message: (
                <>
                    <p>Gerar agora a fatura de <strong>{linha.instituicao}</strong> para o próximo vencimento (dia {linha.diaVencimento})?</p>
                    <p>O valor usa os {linha.alunosAtivos} alunos ativos de hoje. A rotina automática já gera a fatura sozinha dias antes do vencimento; use isto só para adiantar.</p>
                </>
            ),
        });
        if (!confirmado) return;

        setError(null);
        setSuccess(null);
        const response = await apiFetch(`/api/faturamento/assinaturas/${linha.tenantId}/faturas`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({}),
        });
        if (!response.ok) {
            setError(await readApiError(response));
            return;
        }
        const resultado = (await response.json()) as Resultado<Fatura>;
        setSuccess(`${resultado.message} Valor: ${formatarMoeda(resultado.data.valor)}, vencimento ${formatarData(resultado.data.vencimento)}.`);
        setVersao(v => v + 1);
    };

    if (editando) {
        const { form, linha } = editando;
        const plano = planos.find(p => p.id === Number(form.planoId));
        const disponiveis = planos.filter(p => p.ativo || p.id === Number(form.planoId));

        return (
            <form className="form-grid" onSubmit={handleSubmit}>
                <h3 className="secao-titulo form-field-full">
                    {linha.assinaturaId ? 'Assinatura' : 'Nova assinatura'} — {linha.instituicao}
                </h3>
                <FeedbackMessage message={error} type="error" />

                <div className="form-group">
                    <label htmlFor="ass-plano">Plano</label>
                    <select id="ass-plano" value={form.planoId} required onChange={e => setCampo('planoId', e.target.value)}>
                        <option value="">Selecione...</option>
                        {disponiveis.map(p => <option key={p.id} value={p.id}>{p.nome}{p.ativo ? '' : ' (inativo)'}</option>)}
                    </select>
                    {plano && <span className="field-hint">{descreverPlano(plano)}</span>}
                </div>
                <div className="form-group">
                    <label htmlFor="ass-situacao">Situação</label>
                    <select id="ass-situacao" value={form.situacao} onChange={e => setCampo('situacao', e.target.value)}>
                        {Object.entries(situacaoAssinaturaLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
                    </select>
                </div>
                <div className="form-group">
                    <label htmlFor="ass-inicio">Início da assinatura</label>
                    <input id="ass-inicio" type="date" value={form.dataInicio} required onChange={e => setCampo('dataInicio', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-teste">Período de teste até</label>
                    <input id="ass-teste" type="date" value={form.testeAte} onChange={e => setCampo('testeAte', e.target.value)} />
                    <span className="field-hint">Vencimentos até esta data não geram fatura.</span>
                </div>
                <div className="form-group">
                    <label htmlFor="ass-dia">Dia do vencimento</label>
                    <input id="ass-dia" type="number" min={1} max={28} value={form.diaVencimento} required
                        onChange={e => setCampo('diaVencimento', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-desconto">Desconto (%)</label>
                    <input id="ass-desconto" type="number" min={0} max={100} step="0.01" value={form.descontoPercentual}
                        onChange={e => setCampo('descontoPercentual', e.target.value)} />
                </div>

                <h4 className="secao-subtitulo form-field-full">Tomador (cobrança e nota fiscal)</h4>
                <div className="form-group">
                    <label htmlFor="ass-razao">Razão social / nome</label>
                    <input id="ass-razao" value={form.razaoSocial} maxLength={255} required onChange={e => setCampo('razaoSocial', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-doc">CNPJ ou CPF</label>
                    <input id="ass-doc" value={form.cpfCnpj} maxLength={18} required inputMode="numeric"
                        onChange={e => setCampo('cpfCnpj', formatarDocumento(e.target.value))} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-email">E-mail para faturas</label>
                    <input id="ass-email" type="email" value={form.email} maxLength={255} required onChange={e => setCampo('email', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-telefone">Telefone</label>
                    <input id="ass-telefone" value={form.telefone} maxLength={15} inputMode="tel"
                        onChange={e => setCampo('telefone', formatTelefone(e.target.value))} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-cep">CEP</label>
                    <input id="ass-cep" value={form.cep} maxLength={9} inputMode="numeric"
                        onChange={e => setCampo('cep', formatCep(e.target.value))} onBlur={() => void handleCepBlur()} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-endereco">Endereço</label>
                    <input id="ass-endereco" value={form.endereco} maxLength={255} onChange={e => setCampo('endereco', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-numero">Número</label>
                    <input id="ass-numero" value={form.numero} maxLength={20} onChange={e => setCampo('numero', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-complemento">Complemento</label>
                    <input id="ass-complemento" value={form.complemento} maxLength={100} onChange={e => setCampo('complemento', e.target.value)} />
                </div>
                <div className="form-group">
                    <label htmlFor="ass-bairro">Bairro</label>
                    <input id="ass-bairro" value={form.bairro} maxLength={100} onChange={e => setCampo('bairro', e.target.value)} />
                </div>

                <div className="form-actions">
                    <button type="submit" className="btn btn-primary" disabled={isSaving}>
                        {isSaving ? 'Salvando...' : 'Salvar assinatura'}
                    </button>
                    <button type="button" className="btn btn-secondary" onClick={() => setEditando(null)} disabled={isSaving}>
                        Cancelar
                    </button>
                </div>
            </form>
        );
    }

    return (
        <>
            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={success} type="success" />

            {!linhas ? (
                <div className="loading">Carregando assinaturas...</div>
            ) : linhas.length === 0 ? (
                <EmptyState emptyMessage="Nenhuma instituição cadastrada." />
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Instituição</th>
                                <th>Plano</th>
                                <th>Assinatura</th>
                                <th>Financeiro</th>
                                <th>Alunos ativos</th>
                                <th>Valor estimado</th>
                                <th>Em aberto</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {linhas.map(l => (
                                <tr key={l.tenantId}>
                                    <td>
                                        <strong>{l.instituicao}</strong><br />
                                        <small>{l.slug}{l.statusInstituicao !== 'ATIVO' && ' · inativa'}</small>
                                    </td>
                                    <td>{l.planoNome ?? '—'}</td>
                                    <td>
                                        {l.situacao ? (
                                            <span className={`status-pill ${classeSituacao(l.situacao)}`}>
                                                {situacaoAssinaturaLabels[l.situacao]}
                                                {l.situacao === 'TESTE' && l.testeAte && ` até ${formatarData(l.testeAte)}`}
                                            </span>
                                        ) : <span className="status-pill status-inactive">Sem assinatura</span>}
                                    </td>
                                    <td>
                                        <span className={`status-pill ${classeSituacao(l.situacaoFinanceira)}`}>
                                            {situacaoFinanceiraLabels[l.situacaoFinanceira]}
                                        </span>
                                    </td>
                                    <td>{l.alunosAtivos}</td>
                                    <td className="nowrap-cell">{l.valorEstimado === null ? '—' : formatarMoeda(l.valorEstimado)}</td>
                                    <td className="nowrap-cell">
                                        {l.faturasEmAberto > 0 ? `${l.faturasEmAberto} · ${formatarMoeda(l.valorEmAberto)}` : '—'}
                                    </td>
                                    <td>
                                        <div className="action-group vertical">
                                            <button type="button" className="table-action-button" onClick={() => void abrirForm(l)}>
                                                {l.assinaturaId ? 'Editar' : 'Criar assinatura'}
                                            </button>
                                            {l.assinaturaId && l.situacao !== 'CANCELADA' && (
                                                <button type="button" className="table-action-button" onClick={() => void handleGerarFatura(l)}>
                                                    Gerar fatura
                                                </button>
                                            )}
                                            {l.assinaturaId && (
                                                <button type="button" className="table-action-button" onClick={() => onAbrirFaturas(l.tenantId)}>
                                                    Faturas
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            {confirmDialog}
        </>
    );
}

export default AssinaturasFaturamento;
