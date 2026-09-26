import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { PermissoesContext, type PermissoesState } from '../hooks/usePermissoes';
import { apiFetch } from '../utils/api';

async function buscarPermissoes(): Promise<Set<string>> {
    try {
        const response = await apiFetch('/api/permissoes/minhas');
        return response.ok ? new Set((await response.json()) as string[]) : new Set();
    } catch {
        return new Set();
    }
}

/**
 * Carrega as permissões do usuário na instituição atual. Montado só depois de escolhida a
 * instituição (use key={tenantId} para recarregar ao trocar de instituição).
 */
function PermissoesProvider({ children }: { children: ReactNode }) {
    const [permissoes, setPermissoes] = useState<Set<string>>(new Set());
    const [isLoading, setIsLoading] = useState(true);

    const fetchPermissoes = useCallback(async () => {
        const carregadas = await buscarPermissoes();
        setPermissoes(carregadas);
        setIsLoading(false);
    }, []);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            const carregadas = await buscarPermissoes();
            if (cancelled) return;
            setPermissoes(carregadas);
            setIsLoading(false);
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    const value = useMemo<PermissoesState>(() => ({
        isLoading,
        can: permissao => permissoes.has(permissao),
        reload: fetchPermissoes,
    }), [isLoading, permissoes, fetchPermissoes]);

    return <PermissoesContext.Provider value={value}>{children}</PermissoesContext.Provider>;
}

export default PermissoesProvider;
