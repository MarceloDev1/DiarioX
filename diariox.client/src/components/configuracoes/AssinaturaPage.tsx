import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import {
    classeSituacao, formatarCompetencia, formatarData, formatarMoeda, situacaoAssinaturaLabels, situacaoFaturaLabels,
    situacaoFinanceiraLabels, type SituacaoAssinatura, type SituacaoFatura, type SituacaoFinanceira,
} from '../faturamento/tipos';

interface MinhaFatura {
    id: number;
    competencia: string;
    vencimento: string;
    alunosAtivos: number;
    valor: number;
    situacao: SituacaoFatura;
    linkPagamento: string | null;
    pagaEm: string | null;
    notaFiscalNumero: string | null;
    notaFiscalPdfUrl: string | null;
}

interface MinhaAssinatura {
    planoNome: string;
    situacao: SituacaoAssinatura;
    diaVencimento: number;
    testeAte: string | null;
    situacaoFinanceira: SituacaoFinanceira;
    faturas: MinhaFatura[];
}

/** Assinatura do Diário X vista pela instituição: plano, faturas, links de pagamento e notas fiscais. */
function AssinaturaPage() {
    const [assinatura, setAssinatura] = useState<MinhaAssinatura | null | undefined>(undefined);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/assinatura');
                if (!response.ok) throw new Error(await readApiError(response));
                const data = response.status === 204 ? null : ((await response.json()) as MinhaAssinatura);
                if (!cancelled) setAssinatura(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar a assinatura.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Assinatura do Diário X</h1>
            </div>

            <FeedbackMessage message={error} type="error" />

            {assinatura === undefined ? (
                !error && <div className="loading">Carregando assinatura...</div>
            ) : assinatura === null ? (
                <EmptyState emptyMessage="A instituição ainda não tem assinatura cadastrada." emptySubMessage="Fale com o suporte do Diário X." />
            ) : (
                <>
                    <div className="kpi-grid">
                        <div className="kpi-card">
                            <span className="kpi-label">Plano</span>
                            <strong className="kpi-valor">{assinatura.planoNome}</strong>
                            <span className="kpi-detalhe">
                                {situacaoAssinaturaLabels[assinatura.situacao]}
                                {assinatura.situacao === 'TESTE' && assinatura.testeAte && ` até ${formatarData(assinatura.testeAte)}`}
                            </span>
                        </div>
                        <div className="kpi-card">
                            <span className="kpi-label">Vencimento</span>
                            <strong className="kpi-valor">Dia {assinatura.diaVencimento}</strong>
                            <span className="kpi-detalhe">Boleto, PIX ou cartão pelo link da fatura</span>
                        </div>
                        <div className={`kpi-card${assinatura.situacaoFinanceira !== 'REGULAR' ? ' kpi-alerta' : ''}`}>
                            <span className="kpi-label">Situação</span>
                            <strong className="kpi-valor">{situacaoFinanceiraLabels[assinatura.situacaoFinanceira]}</strong>
                        </div>
                    </div>

                    <h3 className="secao-titulo">Faturas</h3>
                    {assinatura.faturas.length === 0 ? (
                        <EmptyState emptyMessage="Nenhuma fatura emitida até o momento." />
                    ) : (
                        <div className="table-container">
                            <table className="data-table">
                                <thead>
                                    <tr>
                                        <th>Competência</th>
                                        <th>Vencimento</th>
                                        <th>Alunos ativos</th>
                                        <th>Valor</th>
                                        <th>Situação</th>
                                        <th>Nota fiscal</th>
                                        <th>Pagamento</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {assinatura.faturas.map(f => (
                                        <tr key={f.id}>
                                            <td>{formatarCompetencia(f.competencia)}</td>
                                            <td className="nowrap-cell">{formatarData(f.vencimento)}</td>
                                            <td>{f.alunosAtivos}</td>
                                            <td className="nowrap-cell">{formatarMoeda(f.valor)}</td>
                                            <td>
                                                <span className={`status-pill ${classeSituacao(f.situacao)}`}>{situacaoFaturaLabels[f.situacao]}</span>
                                                {f.pagaEm && <><br /><small>em {formatarData(f.pagaEm)}</small></>}
                                            </td>
                                            <td>
                                                {f.notaFiscalPdfUrl
                                                    ? <a href={f.notaFiscalPdfUrl} target="_blank" rel="noreferrer">NF {f.notaFiscalNumero ?? ''}</a>
                                                    : '—'}
                                            </td>
                                            <td>
                                                {f.linkPagamento
                                                    ? <a className="btn btn-primary btn-sm" href={f.linkPagamento} target="_blank" rel="noreferrer">Pagar</a>
                                                    : '—'}
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
    );
}

export default AssinaturaPage;
