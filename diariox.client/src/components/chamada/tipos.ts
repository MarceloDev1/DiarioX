export type Situacao = 'PRESENTE' | 'FALTA' | 'FALTA_JUSTIFICADA';

export type TipoFrequencia = 'POR_AULA' | 'DIARIA';

export interface ChamadaDisciplina {
    id: number;
    nome: string;
}

export interface ChamadaPeriodo {
    id: number;
    nome: string;
    dataInicio: string;
    dataTermino: string;
    /** RF017 EX02: período encerrado pela coordenação; a chamada dele só pode ser consultada. */
    encerrado: boolean;
}

export interface ChamadaTurma {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    anoReferencia: number;
    turno: string;
    anoLetivoInicio: string;
    anoLetivoTermino: string;
    disciplinas: ChamadaDisciplina[];
    periodos: ChamadaPeriodo[];
    /** RF017 RN02: POR_AULA (por disciplina) ou DIARIA (uma por dia, sem disciplina). */
    tipoFrequencia: TipoFrequencia;
}

export interface ChamadaAluno {
    alunoId: number;
    matricula: string;
    nome: string;
    situacao: Situacao | null;
    justificativa: string | null;
    /** RF014: aluno transferido; o registro fica congelado. */
    transferido: boolean;
}

export interface Chamada {
    chamadaId: number | null;
    turmaId: number;
    /** Nulo na frequência diária (Anos Iniciais). */
    disciplinaId: number | null;
    data: string;
    quantidadeAulas: number;
    registradoPor: string | null;
    registradoEm: string | null;
    atualizadoPor: string | null;
    atualizadoEm: string | null;
    alunos: ChamadaAluno[];
    /** RF005A: motivo do bloqueio da data pelo Calendário Letivo; nulo = lançamentos permitidos. */
    bloqueio: string | null;
    /** RF017 EX02: o período avaliativo da data está encerrado; só consulta. */
    periodoEncerrado: boolean;
}

export interface ChamadaResumo {
    id: number;
    data: string;
    quantidadeAulas: number;
    presentes: number;
    faltas: number;
    faltasJustificadas: number;
    registradoPor: string | null;
    registradoEm: string;
}

export interface FrequenciaAluno {
    alunoId: number;
    matricula: string;
    nome: string;
    aulas: number;
    faltas: number;
    faltasJustificadas: number;
    percentualFrequencia: number | null;
    abaixoDoMinimo: boolean;
}

export interface Frequencia {
    de: string;
    ate: string;
    aulasDadas: number;
    frequenciaMinima: number;
    alunos: FrequenciaAluno[];
}

export const MAX_QUANTIDADE_AULAS = 6;

export const situacoes: { value: Situacao; sigla: string; label: string }[] = [
    { value: 'PRESENTE', sigla: 'P', label: 'Presente' },
    { value: 'FALTA', sigla: 'F', label: 'Falta' },
    { value: 'FALTA_JUSTIFICADA', sigla: 'FJ', label: 'Falta justificada' },
];

/** Data local de hoje no formato do input date (yyyy-mm-dd). */
export function hojeIso(): string {
    const hoje = new Date();
    const mes = String(hoje.getMonth() + 1).padStart(2, '0');
    const dia = String(hoje.getDate()).padStart(2, '0');
    return `${hoje.getFullYear()}-${mes}-${dia}`;
}

/** yyyy-mm-dd → dd/mm/yyyy, sem passar por Date (evita deslocamento de fuso). */
export function formatarData(iso: string): string {
    const [ano, mes, dia] = iso.slice(0, 10).split('-');
    return `${dia}/${mes}/${ano}`;
}

export function formatarDataHora(iso: string): string {
    return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

export const MENSAGEM_PERIODO_ENCERRADO = 'Este período letivo está encerrado para alterações. Contate a coordenação pedagógica.';

/** Parâmetros de consulta da chamada; a frequência diária não leva disciplina. */
export function paramsDaChamada(turmaId: number, disciplinaId: number | null, extra: Record<string, string> = {}): URLSearchParams {
    const params = new URLSearchParams({ turmaId: String(turmaId), ...extra });
    if (disciplinaId !== null) params.set('disciplinaId', String(disciplinaId));
    return params;
}
