export async function readApiError(response: Response): Promise<string> {
    try {
        const payload = (await response.json()) as { message?: string };
        return payload.message ?? `Erro ${response.status}`;
    } catch {
        return `Erro ${response.status}`;
    }
}
