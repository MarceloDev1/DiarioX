const TOKEN_KEY = 'diariox_token';
const SESSION_KEY = 'diariox_session';

/** Disparado quando a API responde 401 (token ausente, expirado ou inválido). */
export const UNAUTHORIZED_EVENT = 'diariox:unauthorized';

export interface LoginResponse {
    token: string;
    email: string;
    expiresAt?: string;
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
    /** Validade do token (ISO); a sessão restaurada é descartada depois dela. */
    expiresAt: string | null;
}

/** Guarda o token do login (ou da troca de instituição) e devolve a sessão correspondente. */
export function startSession(data: LoginResponse): Session {
    sessionStorage.setItem(TOKEN_KEY, data.token);
    const session: Session = {
        email: data.email,
        isGlobalAdmin: data.isGlobalAdmin ?? false,
        tenantId: data.tenantId ?? null,
        tenantNome: data.tenantNome ?? null,
        expiresAt: data.expiresAt ?? null,
    };
    saveSession(session);
    return session;
}

/** Persiste os dados da sessão para que sobrevivam ao recarregamento da página. */
export function saveSession(session: Session) {
    try {
        sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    } catch {
        // Armazenamento indisponível: a sessão continua válida apenas em memória.
    }
}

/** Recupera a sessão salva, desde que exista token e ele ainda não tenha expirado. */
export function restoreSession(): Session | null {
    try {
        const token = sessionStorage.getItem(TOKEN_KEY);
        const raw = sessionStorage.getItem(SESSION_KEY);
        if (!token || !raw) return null;

        const session = JSON.parse(raw) as Session;
        if (session.expiresAt && new Date(session.expiresAt).getTime() <= Date.now()) {
            clearSession();
            return null;
        }

        return session;
    } catch {
        clearSession();
        return null;
    }
}

export function clearSession() {
    try {
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem(SESSION_KEY);
    } catch {
        // Armazenamento indisponível: nada a limpar.
    }
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
