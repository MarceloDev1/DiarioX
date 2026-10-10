/** Home do professor (GET /api/dashboard/professor). */
export interface PainelProfessor {
    professorNome: string;
    anoReferencia: number;
    anoLetivoInicio: string | null;
    anoLetivoTermino: string | null;
    /** EX02: sem diário no ano letivo vigente; os cards mostram esta mensagem. */
    semLotacao: string | null;
    escolas: { id: number; nome: string }[];
    escolaId: number | null;
    hoje: AulasDoDia;
    pendencias: PendenciasMes | null;
    diarios: DiarioPainel[];
    registrosAula: GraficoRegistros | null;
    registrosFrequencia: GraficoRegistros | null;
    /** Nulo quando o perfil não vê o módulo de notas. */
    notas: PeriodoNotas[] | null;
}

export interface AulasDoDia {
    data: string;
    diaLetivo: boolean;
    evento: string | null;
    /** EX01/EX02. */
    mensagem: string | null;
    aulas: AulaDoDia[];
}

export interface AulaDoDia {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    /** Nulo na frequência diária (Anos Iniciais). */
    disciplinaId: number | null;
    disciplinaNome: string;
    tempos: number[];
    frequenciaRegistrada: boolean;
    aulaRegistrada: boolean;
}

export type SituacaoDia = 'fora-do-ano' | 'nao-letivo' | 'sem-aula' | 'em-dia' | 'pendente' | 'hoje' | 'futuro';

export interface PendenciasMes {
    ano: number;
    mes: number;
    dias: DiaPendencia[];
}

export interface DiaPendencia {
    data: string;
    situacao: SituacaoDia;
    evento: string | null;
    pendencias: PendenciaAula[];
}

export interface PendenciaAula {
    turmaId: number;
    turmaNome: string;
    disciplinaId: number | null;
    disciplinaNome: string;
    semFrequencia: boolean;
    semAula: boolean;
}

export interface DiarioPainel {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    etapaNome: string;
    diaria: boolean;
    semGrade: boolean;
    disciplinas: { id: number; nome: string; aulasSemanais: number }[];
}

export interface GraficoRegistros {
    registrados: number;
    pendentes: number;
    aFazer: number;
    percentual: number | null;
}

export interface PeriodoNotas {
    id: number;
    nome: string;
    numero: number;
    dataInicio: string;
    dataTermino: string;
    prazoLancamentoNotas: string | null;
    prazoEncerrado: boolean;
    atual: boolean;
    pendencias: PendenciaNota[];
}

export interface PendenciaNota {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    disciplinaId: number;
    disciplinaNome: string;
    avaliacoes: number;
    notasFaltantes: number;
    descricao: string;
}

/** Links das telas do diário já abertas na turma/disciplina/data. */
export function linkChamada(turmaId: number, disciplinaId: number | null, data?: string): string {
    return comParametros('/chamada', { turmaId, disciplinaId, data });
}

export function linkConteudo(turmaId: number, disciplinaId: number | null, data?: string): string {
    return comParametros('/conteudo-ministrado', { turmaId, disciplinaId, data, aba: data ? 'registrar' : undefined });
}

export function linkNotas(turmaId: number, disciplinaId: number, periodoId?: number): string {
    return comParametros('/notas', { turmaId, disciplinaId, periodoId });
}

function comParametros(caminho: string, parametros: Record<string, string | number | null | undefined>): string {
    const busca = new URLSearchParams();
    for (const [chave, valor] of Object.entries(parametros)) {
        if (valor !== null && valor !== undefined) busca.set(chave, String(valor));
    }
    const texto = busca.toString();
    return texto ? `${caminho}?${texto}` : caminho;
}

/** [1, 2] → "1º e 2º tempos"; [3] → "3º tempo". */
export function formatarTempos(tempos: number[]): string {
    if (tempos.length === 0) return '';
    const ordinais = tempos.map(t => `${t}º`);
    const lista = ordinais.length === 1 ? ordinais[0] : `${ordinais.slice(0, -1).join(', ')} e ${ordinais[ordinais.length - 1]}`;
    return `${lista} ${tempos.length === 1 ? 'tempo' : 'tempos'}`;
}

const MESES_EXTENSO = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];

export const nomeDoMes = (mes: number) => MESES_EXTENSO[mes - 1];
