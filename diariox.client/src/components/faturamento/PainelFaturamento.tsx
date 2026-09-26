import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import { formatarCompetencia, formatarData, formatarMoeda, type Painel, type RotinaResultado } from './tipos';

interface PainelFaturamentoProps {
    onAbrirFaturas: (tenantId: number | null) => void;
}

function PainelFaturamento({ onAbrirFaturas }: PainelFaturamentoProps) {
    const [painel, setPainel] = useState<Painel | null>(null);
    const [versao, setVersao] = useState(0);
    const [error, setError] = useState<string | null>(null);
    const [resultado, setResultado] = useState<string | null>(null);
    const [executando, setExecutando] = useState(false);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/faturamento/painel');
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Painel;
                if (!cancelled) setPainel(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar o painel.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [versao]);

    const handleExecutarRotina = async () => {
        setExecutando(true);
        setError(null);
        setResultado(null);
        try {
            const response = await apiFetch('/api/faturamento/rotina', { method: 'POST' });
            if (!response.ok) throw new Error(await readApiError(response));
            const r = (await response.json()) as RotinaResultado;
            setResultado(
                `Rotina executada: ${r.faturasGeradas} fatura(s) gerada(s), ${r.cobrancasReenviadas} cobrança(s) reenviada(s), ` +
                `${r.faturasVencidas} marcada(s) como vencida(s) e ${r.instituicoesAtualizadas} instituição(ões) com situação alterada.` +
                (r.erros.length > 0 ? ` Atenção: ${r.erros.join(' | ')}` : ''));
            setVersao(v => v + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao executar a rotina.');
        } finally {
            setExecutando(false);
        }
    };

    if (!painel) {
        return error ? <FeedbackMessage message={error} type="error" /> : <div className="loading">Carregando painel...</div>;
    }

    const indicadores: { label: string; valor: string; detalhe?: string; alerta?: boolean }[] = [
        { label: 'Receita mensal estimada', valor: formatarMoeda(painel.receitaMensalEstimada), detalhe: `${painel.assinaturasAtivas} assinatura(s) ativa(s) · ${painel.assinaturasEmTeste} em teste` },
        { label: 'Faturado no mês', valor: formatarMoeda(painel.faturadoNoMes) },
        { label: 'Recebido no mês', valor: formatarMoeda(painel.recebidoNoMes) },
        { label: 'A vencer', valor: formatarMoeda(painel.emAberto) },
        { label: 'Vencido', valor: formatarMoeda(painel.vencido), alerta: painel.vencido > 0 },
        {
            label: 'Instituições com pendência',
            valor: String(painel.instituicoesEmAtraso + painel.instituicoesSomenteLeitura),
            detalhe: `${painel.instituicoesEmAtraso} em atraso · ${painel.instituicoesSomenteLeitura} em somente leitura`,
            alerta: painel.instituicoesSomenteLeitura > 0,
        },
    ];

    return (
        <>
            {!painel.asaasConfigurado && (
                <div className="aviso-financeiro aviso-atencao">
                    <strong>Asaas não configurado.</strong> As faturas são geradas só no sistema, sem boleto/PIX, e a baixa
                    precisa ser manual. Configure <code>Asaas:ApiKey</code> e <code>Asaas:WebhookToken</code> (user-secrets ou
                    variáveis de ambiente) e cadastre o webhook <code>/api/webhooks/asaas</code> no painel do Asaas.
                </div>
            )}
            {painel.asaasConfigurado && !painel.notaFiscalHabilitada && (
                <div className="aviso-financeiro aviso-info">
                    A emissão automática da sua NFS-e está desativada (<code>Faturamento:NotaFiscal:Habilitada</code>).
                </div>
            )}

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={resultado} type="success" />

            <div className="kpi-grid">
                {indicadores.map(i => (
                    <div key={i.label} className={`kpi-card${i.alerta ? ' kpi-alerta' : ''}`}>
                        <span className="kpi-label">{i.label}</span>
                        <strong className="kpi-valor">{i.valor}</strong>
                        {i.detalhe && <span className="kpi-detalhe">{i.detalhe}</span>}
                    </div>
                ))}
            </div>

            <div className="chamada-barra">
                <h3 className="secao-titulo">Faturas vencidas</h3>
                <button type="button" className="btn btn-secondary btn-sm" onClick={handleExecutarRotina} disabled={executando}>
                    {executando ? 'Executando...' : 'Executar rotina agora'}
                </button>
            </div>
            <p className="field-hint">
                A rotina roda sozinha várias vezes ao dia: gera as faturas {painel.diasAntecedenciaFatura} dias antes do vencimento, reenvia cobranças
                que falharam e aplica a régua ({painel.diasAtrasoSomenteLeitura} dias de atraso = somente leitura).
            </p>

            {painel.faturasVencidas.length === 0 ? (
                <div className="empty-state"><strong>Nenhuma fatura vencida. 🎉</strong></div>
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Instituição</th>
                                <th>Competência</th>
                                <th>Vencimento</th>
                                <th>Dias em atraso</th>
                                <th>Valor</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {painel.faturasVencidas.map(f => (
                                <tr key={f.id} className={f.diasEmAtraso >= painel.diasAtrasoSomenteLeitura ? 'frequencia-baixa' : undefined}>
                                    <td>{f.instituicao}</td>
                                    <td>{formatarCompetencia(f.competencia)}</td>
                                    <td>{formatarData(f.vencimento)}</td>
                                    <td>
                                        <span className={`status-pill ${f.diasEmAtraso >= painel.diasAtrasoSomenteLeitura ? 'situacao-pill-falta' : 'situacao-pill-falta_justificada'}`}>
                                            {f.diasEmAtraso} {f.diasEmAtraso === 1 ? 'dia' : 'dias'}
                                        </span>
                                    </td>
                                    <td>{formatarMoeda(f.valor)}</td>
                                    <td>
                                        <button type="button" className="table-action-button" onClick={() => onAbrirFaturas(f.tenantId)}>
                                            Ver faturas
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </>
    );
}

export default PainelFaturamento;
