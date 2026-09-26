import { useEffect, useState } from 'react';
import { FiBookOpen } from 'react-icons/fi';
import '../Login.css';
import { apiFetch, readApiError, startSession, type LoginResponse, type Session } from '../../utils/api';

interface Tenant {
    id: number;
    nome: string;
    slug: string;
    status: string;
}

interface TenantPickerProps {
    onSelect: (session: Session) => void;
    onManage: () => void;
    onFinanceiro: () => void;
    onLogout: () => void;
}

/** Tela do Administrador global para escolher em qual instituição vai atuar. */
function TenantPicker({ onSelect, onManage, onFinanceiro, onLogout }: TenantPickerProps) {
    const [tenants, setTenants] = useState<Tenant[]>([]);
    const [loading, setLoading] = useState(true);
    const [selectingId, setSelectingId] = useState<number | null>(null);
    const [error, setError] = useState('');

    useEffect(() => {
        let active = true;
        apiFetch('/api/tenants')
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                return (await response.json()) as Tenant[];
            })
            .then(data => {
                if (active) setTenants(data.filter(tenant => tenant.status === 'ATIVO'));
            })
            .catch(e => {
                if (active) setError(e instanceof Error ? e.message : 'Falha ao carregar instituições.');
            })
            .finally(() => {
                if (active) setLoading(false);
            });
        return () => {
            active = false;
        };
    }, []);

    async function handleSelect(tenant: Tenant) {
        setError('');
        setSelectingId(tenant.id);
        try {
            const response = await apiFetch('/api/auth/select-tenant', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ tenantId: tenant.id }),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            onSelect(startSession((await response.json()) as LoginResponse));
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao selecionar a instituição.');
            setSelectingId(null);
        }
    }

    return (
        <div className="login-container">
            <div className="login-card">
                <div className="login-logo">
                    <FiBookOpen />
                </div>
                <h1 className="login-title">Escolha a instituição</h1>
                <p className="login-description">Como administrador global, selecione em qual instituição deseja atuar.</p>

                {loading && <p className="login-description">Carregando instituições...</p>}
                {!loading && !error && tenants.length === 0 && (
                    <p className="login-description">Nenhuma instituição ativa cadastrada.</p>
                )}

                <div className="tenant-list">
                    {tenants.map(tenant => (
                        <button
                            key={tenant.id}
                            type="button"
                            className="secondary-button"
                            onClick={() => void handleSelect(tenant)}
                            disabled={selectingId !== null}
                        >
                            {selectingId === tenant.id ? 'Entrando...' : tenant.nome}
                        </button>
                    ))}
                </div>

                {error && <p className="login-error">{error}</p>}

                <div className="first-access">
                    <button type="button" className="first-access-link" onClick={onManage}>
                        Gerenciar instituições
                    </button>
                    {' · '}
                    <button type="button" className="first-access-link" onClick={onFinanceiro}>
                        Financeiro
                    </button>
                    {' · '}
                    <button type="button" className="first-access-link" onClick={onLogout}>
                        Sair
                    </button>
                </div>
            </div>
        </div>
    );
}

export default TenantPicker;
