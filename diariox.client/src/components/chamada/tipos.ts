export type Situacao = 'PRESENTE' | 'FALTA' | 'FALTA_JUSTIFICADA';

export interface ChamadaDisciplina {
    id: number;
    nome: string;
}

export interface ChamadaPeriodo {
    id: number;
    nome: string;
    dataInicio: string;
    dataTermino: string;
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
}

export interface ChamadaAluno {
    alunoId: number;
    matricula: string;
    nome: string;
    situacao: Situacao | null;
    justificativa: string | null;
}

export interface Chamada {
    chamadaId: number | null;
    turmaId: number;
    disciplinaId: number;
    data: string;
    quantidadeAulas: number;
    conteudo: string | null;
    registradoPor: string | null;
    registradoEm: string | null;
    atualizadoPor: string | null;
    atualizadoEm: string | null;
    alunos: ChamadaAluno[];
}

export interface ChamadaResumo {
    id: number;
    data: string;
    quantidadeAulas: number;
    presentes: number;
    faltas: number;
    faltasJustificadas: number;
    conteudo: string | null;
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
