/**
 * Turma, disciplina, data e período pedidos na URL pelos atalhos da home do professor
 * (ex.: /chamada?turmaId=3&disciplinaId=5&data=2026-10-07). Valores inválidos viram nulo.
 */
export interface SelecaoDiario {
    turmaId: number | null;
    disciplinaId: number | null;
    data: string | null;
    periodoId: number | null;
    aba: string | null;
}

export function lerSelecaoDiario(params: URLSearchParams): SelecaoDiario {
    const inteiro = (chave: string) => Number(params.get(chave)) || null;
    const data = params.get('data');
    return {
        turmaId: inteiro('turmaId'),
        disciplinaId: inteiro('disciplinaId'),
        data: data && /^\d{4}-\d{2}-\d{2}$/.test(data) ? data : null,
        periodoId: inteiro('periodoId'),
        aba: params.get('aba'),
    };
}

/** Turma pedida na URL (se o usuário tiver acesso) e a disciplina pedida, ou a única da turma. */
export function turmaPedida<T extends { turmaId: number; disciplinas: { id: number }[] }>(
    turmas: T[], selecao: SelecaoDiario,
): { turma: T; disciplinaId: number | null } | null {
    const turma = turmas.find(t => t.turmaId === selecao.turmaId);
    if (!turma) return null;

    const disciplinaId = turma.disciplinas.some(d => d.id === selecao.disciplinaId)
        ? selecao.disciplinaId
        : turma.disciplinas.length === 1 ? turma.disciplinas[0].id : null;
    return { turma, disciplinaId };
}
