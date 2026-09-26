import { createContext, useContext } from 'react';

export interface PermissoesState {
    isLoading: boolean;
    /** Indica se o usuário tem a permissão ("modulo.acao") na instituição atual. */
    can: (permissao: string) => boolean;
    /** Recarrega as permissões (ex.: depois de alterar a matriz do próprio perfil). */
    reload: () => Promise<void>;
}

export const PermissoesContext = createContext<PermissoesState>({
    isLoading: true,
    can: () => false,
    reload: async () => {},
});

export function usePermissoes() {
    return useContext(PermissoesContext);
}
