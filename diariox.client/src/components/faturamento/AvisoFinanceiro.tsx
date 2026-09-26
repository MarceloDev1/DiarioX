import { useEffect, useState } from 'react';
import { apiFetch } from '../../utils/api';
import { formatarData, type SituacaoFinanceira } from './tipos';

interface Aviso {
    situacaoFinanceira: SituacaoFinanceira;
    diasEmAtraso: number;
    vencimentoMaisAntigo: string | null;
    somenteLeituraEm: string | null;
    podeVerFaturas: boolean;
}

interface AvisoFinanceiroProps {
    onVerFaturas: () => void;
}

/**
 * Faixa no topo do sistema quando a instituição tem fatura do Diário X vencida. O atraso só é
 * mostrado para quem gerencia a assinatura; o modo somente leitura é avisado a todos.
 */
function AvisoFinanceiro({ onVerFaturas }: AvisoFinanceiroProps) {
    const [aviso, setAviso] = useState<Aviso | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/assinatura/aviso');
                if (response.ok && !cancelled) setAviso((await response.json()) as Aviso);
            } catch {
                // O aviso é informativo: sem ele o sistema segue normalmente.
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    if (!aviso || aviso.situacaoFinanceira === 'REGULAR') return null;

    if (aviso.situacaoFinanceira === 'EM_ATRASO') {
        if (!aviso.podeVerFaturas) return null;
        return (
            <div className="aviso-financeiro aviso-atencao" role="status">
                <span>
                    A fatura do Diário X venceu em <strong>{formatarData(aviso.vencimentoMaisAntigo)}</strong>
                    {' '}({aviso.diasEmAtraso} {aviso.diasEmAtraso === 1 ? 'dia' : 'dias'} em atraso).
                    {' '}Se não for paga, o sistema entra em modo somente leitura em <strong>{formatarData(aviso.somenteLeituraEm)}</strong>.
                </span>
                <button type="button" className="btn btn-secondary btn-sm" onClick={onVerFaturas}>Ver faturas</button>
            </div>
        );
    }

    return (
        <div className="aviso-financeiro aviso-bloqueio" role="alert">
            <span>
                <strong>Modo somente leitura:</strong> a instituição tem pendência financeira com o Diário X. Consultas e
                chamadas continuam funcionando; cadastros e alterações voltam assim que o pagamento for confirmado.
                {!aviso.podeVerFaturas && ' Procure a gerência da instituição.'}
            </span>
            {aviso.podeVerFaturas && (
                <button type="button" className="btn btn-primary btn-sm" onClick={onVerFaturas}>Ver faturas e pagar</button>
            )}
        </div>
    );
}

export default AvisoFinanceiro;
