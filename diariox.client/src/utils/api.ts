const TOKEN_KEY = 'diariox_token';

/** Disparado quando a API responde 401 (token ausente, expirado ou inválido). */
export const UNAUTHORIZED_EVENT = 'diariox:unauthorized';

export interface LoginResponse {
    token: string;
    email: string;
    tenantId?: number | null;
    tenantNome?: string | null;
    isGlobalAdmin?: boolean;
}

export interface Session {
    email: string;
    isGlobalAdmin: boolean;
    /** Instituição em que o usuário está atuando; nulo para o Administrador global antes de escolher uma. */
    tenantId: number | null;
    tenantNome: string | null;
}

/** Guarda o token do login (ou da troca de instituição) e devolve a sessão correspondente. */
export function startSession(data: LoginResponse): Session {
    sessionStorage.setItem(TOKEN_KEY, data.token);
    return {
        email: data.email,
        isGlobalAdmin: data.isGlobalAdmin ?? false,
        tenantId: data.tenantId ?? null,
        tenantNome: data.tenantNome ?? null,
    };
}

export function clearSession() {
    sessionStorage.removeItem(TOKEN_KEY);
}

/** fetch autenticado: envia o token da sessão e avisa a aplicação quando a API responde 401. */
export async function apiFetch(input: string, init: RequestInit = {}): Promise<Response> {
    const headers = new Headers(init.headers);
    const token = sessionStorage.getItem(TOKEN_KEY);
    if (token) headers.set('Authorization', `Bearer ${token}`);

    const response = await fetch(input, { ...init, headers });
    if (response.status === 401) window.dispatchEvent(new Event(UNAUTHORIZED_EVENT));
    return response;
}

export async function readApiError(response: Response): Promise<string> {
    try {
        const payload = (await response.json()) as { message?: string };
        return payload.message ?? `Erro ${response.status}`;
    } catch {
        return `Erro ${response.status}`;
    }
}
