import { useState } from 'react';
import PainelFaturamento from './PainelFaturamento';
import AssinaturasFaturamento from './AssinaturasFaturamento';
import FaturasFaturamento from './FaturasFaturamento';
import PlanosFaturamento from './PlanosFaturamento';

type Aba = 'painel' | 'assinaturas' | 'faturas' | 'planos';

const abas: { id: Aba; label: string }[] = [
    { id: 'painel', label: 'Painel' },
    { id: 'assinaturas', label: 'Assinaturas' },
    { id: 'faturas', label: 'Faturas' },
    { id: 'planos', label: 'Planos' },
];

/** Financeiro da plataforma: cobrança das instituições pelo uso do Diário X (Administrador global). */
function FinanceiroPlataformaPage() {
    const [aba, setAba] = useState<Aba>('painel');
    // Filtro de instituição aplicado ao abrir as faturas a partir de uma assinatura.
    const [tenantFaturas, setTenantFaturas] = useState<number | null>(null);

    const abrirFaturas = (tenantId: number | null) => {
        setTenantFaturas(tenantId);
        setAba('faturas');
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Financeiro da plataforma</h1>
            </div>
            <p className="page-intro">
                Planos, assinaturas e faturas das instituições que usam o Diário X. As cobranças são emitidas pelo
                Asaas e a baixa acontece automaticamente quando o pagamento é confirmado.
            </p>

            <div className="form-grid">
                <div className="form-tabs" role="tablist" aria-label="Financeiro da plataforma">
                    {abas.map(a => (
                        <button
                            key={a.id}
                            type="button"
                            role="tab"
                            aria-selected={aba === a.id}
                            className={`form-tab${aba === a.id ? ' active' : ''}`}
                            onClick={() => (a.id === 'faturas' ? abrirFaturas(null) : setAba(a.id))}
                        >
                            {a.label}
                        </button>
                    ))}
                </div>

                <div className="chamada-painel" role="tabpanel">
                    {aba === 'painel' && <PainelFaturamento onAbrirFaturas={abrirFaturas} />}
                    {aba === 'assinaturas' && <AssinaturasFaturamento onAbrirFaturas={abrirFaturas} />}
                    {aba === 'faturas' && <FaturasFaturamento key={tenantFaturas ?? 'todas'} tenantInicial={tenantFaturas} />}
                    {aba === 'planos' && <PlanosFaturamento />}
                </div>
            </div>
        </div>
    );
}

export default FinanceiroPlataformaPage;
