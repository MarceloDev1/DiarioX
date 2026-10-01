import { apiFetch, readApiError } from '../../utils/api';

export interface DesenturmacaoResultado {
    ok: boolean;
    message: string;
    /** Alunos que impediram a operação (nada é gravado), com o motivo de cada um. */
    falhas: Record<number, string>;
}

/** Chama o RF013 no servidor: tudo ou nada. */
export async function desenturmar(turmaId: number, alunoIds: number[], motivo: string, observacao: string | null): Promise<DesenturmacaoResultado> {
    try {
        const response = await apiFetch('/api/alunos/desenturmacoes', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ turmaId, alunoIds, motivo, observacao }),
        });

        if (!response.ok) {
            const payload = (await response.clone().json().catch(() => null)) as { falhas?: { alunoId: number; motivo: string }[] } | null;
            return {
                ok: false,
                message: await readApiError(response),
                falhas: Object.fromEntries((payload?.falhas ?? []).map(falha => [falha.alunoId, falha.motivo])),
            };
        }

        const result = (await response.json()) as { message: string };
        return { ok: true, message: result.message, falhas: {} };
    } catch (reason) {
        return { ok: false, message: reason instanceof Error ? reason.message : 'Falha ao realizar a desenturmação.', falhas: {} };
    }
}
