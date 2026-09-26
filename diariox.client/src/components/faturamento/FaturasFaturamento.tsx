import { useEffect, useState } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import {
    classeSituacao, formatarCompetencia, formatarData, formatarMoeda, notaFiscalLabels, situacaoFaturaLabels,
    type AssinaturaResumo, type Fatura, type Resultado, type SituacaoFatura,
} from './tipos';

interface FaturasFaturamentoProps {
    tenantInicial: number | null;
}

const hojeIso = () => new Date().toLocaleDateString('sv-SE');

function memoriaDeCalculo(f: Fatura): string {
    const partes = [`${f.alunosAtivos} alunos ativos`];
    const bruto = f.valorFixo + f.valorPorAluno * f.alunosAtivos;
    partes.push(`${formatarMoeda(f.valorFixo)} + ${formatarMoeda(f.valorPorAluno)} × ${f.alunosAtivos} = ${formatarMoeda(bruto)}`);
    if (f.valorMinimo > bruto) partes.push(`mínimo ${formatarMoeda(f.valorMinimo)}`);
    if (f.descontoPercentual > 0) partes.push(`desconto ${f.descontoPercentual}%`);
    return partes.join(' · ');
}

function FaturasFaturamento({ tenantInicial }: FaturasFaturamentoProps) {
    const { confirm, confirmDialog } = useConfirm();
    const [faturas, setFaturas] = useState<Fatura[] | null>(null);
    const [instituicoes, setInstituicoes] = useState<AssinaturaResumo[]>([]);
    const [tenantId, setTenantId] = useState<number | null>(tenantInicial);
    const [situacao, setSituacao] = useState<SituacaoFatura | ''>('');
    const [versao, setVersao] = useState(0);
    const [pagamento, setPagamento] = useState<{ fatura: Fatura; data: string; valor: string } | null>(null);
    const [ocupado, setOcupado] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams();
                if (tenantId) params.set('tenantId', String(tenantId));
                if (situacao) params.set('situacao', situacao);
                const [faturasRes, instituicoesRes] = await Promise.all([
                    apiFetch(`/api/faturamento/faturas?${params}`),
                    apiFetch('/api/faturamento/assinaturas'),
                ]);
                if (!faturasRes.ok) throw new Error(await readApiError(faturasRes));
                const lista = (await faturasRes.json()) as Fatura[];
                const comAssinatura = instituicoesRes.ok
                    ? ((await instituicoesRes.json()) as AssinaturaResumo[]).filter(i => i.assinaturaId)
                    : [];
                if (cancelled) return;
                setFaturas(lista);
                setInstituicoes(comAssinatura);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as faturas.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [tenantId, situacao, versao]);

    // Executa uma ação da fatura e recarrega a lista, mostrando a mensagem do servidor.
    const executar = async (url: string, body?: unknown) => {
        setOcupado(true);
        setError(null);
        setSuccess(null);
        try {
            const response = await apiFetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body ?? {}),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            setSuccess(((await response.json()) as Resultado<Fatura>).message);
            setPagamento(null);
            setVersao(v => v + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao executar a ação.');
        } finally {
            setOcupado(false);
        }
    };

    const handleCancelar = async (f: Fatura) => {
        const confirmado = await confirm({
            title: 'Cancelar fatura',
            variant: 'danger',
            confirmLabel: 'Cancelar fatura',
            message: (
                <>
                    <p>Cancelar a fatura de <strong>{f.instituicao}</strong> ({formatarCompetencia(f.competencia)}, {formatarMoeda(f.valor)})?</p>
                    <p>A cobrança também é removida do Asaas. A rotina não gera outra para este mês; se precisar, use "Gerar fatura" na assinatura.</p>
                </>
            ),
        });
        if (confirmado) await executar(`/api/faturamento/faturas/${f.id}/cancelar`);
    };

    const podeEmitirNota = (f: Fatura) =>
        f.situacao === 'PAGA' && f.asaasCobrancaId !== null && f.valor > 0 &&
        f.notaFiscalSituacao !== 'AGENDADA' && f.notaFiscalSituacao !== 'EMITIDA';

    return (
        <>
            <div className="chamada-cabecalho">
                <div className="form-group">
                    <label htmlFor="fat-instituicao">Instituição</label>
                    <select id="fat-instituicao" value={tenantId ?? ''} onChange={e => setTenantId(e.target.value ? Number(e.target.value) : null)}>
                        <option value="">Todas</option>
                        {instituicoes.map(i => <option key={i.tenantId} value={i.tenantId}>{i.instituicao}</option>)}
                    </select>
                </div>
                <div className="form-group">
                    <label htmlFor="fat-situacao">Situação</label>
                    <select id="fat-situacao" value={situacao} onChange={e => setSituacao(e.target.value as SituacaoFatura | '')}>
                        <option value="">Todas</option>
                        {Object.entries(situacaoFaturaLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
                    </select>
                </div>
            </div>

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={success} type="success" />

            {pagamento && (
                <div className="pagamento-manual">
                    <strong>Registrar pagamento — {pagamento.fatura.instituicao} ({formatarCompetencia(pagamento.fatura.competencia)})</strong>
                    <div className="chamada-cabecalho">
                        <div className="form-group">
                            <label htmlFor="pag-data">Data do pagamento</label>
                            <input id="pag-data" type="date" max={hojeIso()} value={pagamento.data}
                                onChange={e => setPagamento({ ...pagamento, data: e.target.value })} />
                        </div>
                        <div className="form-group">
                            <label htmlFor="pag-valor">Valor recebido (R$)</label>
                            <input id="pag-valor" type="number" min={0.01} step="0.01" value={pagamento.valor}
                                onChange={e => setPagamento({ ...pagamento, valor: e.target.value })} />
                        </div>
                    </div>
                    <span className="field-hint">
                        Use para pagamentos fora do Asaas (transferência, dinheiro). A cobrança no Asaas também recebe a baixa.
                    </span>
                    <div className="form-actions">
                        <button type="button" className="btn btn-primary" disabled={ocupado || !pagamento.data}
                            onClick={() => void executar(`/api/faturamento/faturas/${pagamento.fatura.id}/pagamento-manual`, {
                                data: pagamento.data,
                                valor: Number(pagamento.valor.replace(',', '.')) || null,
                            })}>
                            Confirmar pagamento
                        </button>
                        <button type="button" className="btn btn-secondary" onClick={() => setPagamento(null)} disabled={ocupado}>
                            Cancelar
                        </button>
                    </div>
                </div>
            )}

            {!faturas ? (
                <div className="loading">Carregando faturas...</div>
            ) : faturas.length === 0 ? (
                <EmptyState emptyMessage="Nenhuma fatura encontrada." emptySubMessage="As faturas são geradas pela rotina ou pelo botão Gerar fatura da assinatura." />
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Instituição</th>
                                <th>Competência</th>
                                <th>Vencimento</th>
                                <th>Valor</th>
                                <th>Situação</th>
                                <th>Pagamento</th>
                                <th>Nota fiscal</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {faturas.map(f => (
                                <tr key={f.id}>
                                    <td>{f.instituicao}</td>
                                    <td>{formatarCompetencia(f.competencia)}</td>
                                    <td className="nowrap-cell">{formatarData(f.vencimento)}</td>
                                    <td className="nowrap-cell" title={memoriaDeCalculo(f)}>
                                        <strong>{formatarMoeda(f.valor)}</strong><br />
                                        <small>{f.alunosAtivos} alunos</small>
                                    </td>
                                    <td>
                                        <span className={`status-pill ${classeSituacao(f.situacao)}`}>{situacaoFaturaLabels[f.situacao]}</span>
                                        {f.diasEmAtraso > 0 && <><br /><small>{f.diasEmAtraso} dia(s) em atraso</small></>}
                                        {f.observacao && <><br /><small className="texto-alerta">{f.observacao}</small></>}
                                    </td>
                                    <td>
                                        {f.pagaEm ? (
                                            <small>
                                                {formatarData(f.pagaEm)}<br />
                                                {f.valorPago !== null && formatarMoeda(f.valorPago)}{f.formaPagamento && ` · ${f.formaPagamento}`}
                                            </small>
                                        ) : '—'}
                                    </td>
                                    <td>
                                        {f.notaFiscalSituacao ? (
                                            <>
                                                <span className={`status-pill ${classeSituacao(f.notaFiscalSituacao)}`}>
                                                    {notaFiscalLabels[f.notaFiscalSituacao] ?? f.notaFiscalSituacao}
                                                </span>
                                                {f.notaFiscalNumero && <><br /><small>Nº {f.notaFiscalNumero}</small></>}
                                                {f.notaFiscalPdfUrl && <><br /><a href={f.notaFiscalPdfUrl} target="_blank" rel="noreferrer">PDF</a></>}
                                                {f.notaFiscalErro && <><br /><small className="texto-alerta">{f.notaFiscalErro}</small></>}
                                            </>
                                        ) : '—'}
                                    </td>
                                    <td>
                                        <div className="action-group vertical">
                                            {f.linkPagamento && (
                                                <button type="button" className="table-action-button"
                                                    onClick={() => window.open(f.linkPagamento!, '_blank', 'noopener,noreferrer')}>
                                                    Cobrança
                                                </button>
                                            )}
                                            {(f.situacao === 'PENDENTE' || f.situacao === 'VENCIDA') && (
                                                <>
                                                    <button type="button" className="table-action-button" disabled={ocupado}
                                                        onClick={() => setPagamento({ fatura: f, data: hojeIso(), valor: String(f.valor) })}>
                                                        Registrar pagamento
                                                    </button>
                                                    <button type="button" className="table-action-button danger" disabled={ocupado}
                                                        onClick={() => void handleCancelar(f)}>
                                                        Cancelar
                                                    </button>
                                                </>
                                            )}
                                            {podeEmitirNota(f) && (
                                                <button type="button" className="table-action-button" disabled={ocupado}
                                                    onClick={() => void executar(`/api/faturamento/faturas/${f.id}/nota-fiscal`)}>
                                                    Emitir NF
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

export default FaturasFaturamento;
