export type CalculoNotaPeriodo = 'MEDIA_PONDERADA' | 'SOMA';

export type TipoAvaliacao = 'PROVA' | 'TRABALHO' | 'ATIVIDADE' | 'PARTICIPACAO' | 'OUTRO' | 'RECUPERACAO';

export type SituacaoMedia = 'SEM_NOTAS' | 'EM_ANDAMENTO' | 'MEDIA_ATINGIDA' | 'ABAIXO_DA_MEDIA';

export interface RegraResumo {
    /** Nulo = regra padrão do sistema (etapa sem regra própria). */
    id: number | null;
    nome: string;
    notaMaxima: number;
    mediaAprovacao: number;
    casasDecimais: number;
    calculoNotaPeriodo: CalculoNotaPeriodo;
    permiteRecuperacao: boolean;
}

export interface NotaDisciplina {
    id: number;
    nome: string;
}

export interface NotaPeriodo {
    id: number;
    nome: string;
    numero: number;
    dataInicio: string;
    dataTermino: string;
}

export interface NotaTurma {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    anoReferencia: number;
    turno: string;
    ativa: boolean;
    disciplinas: NotaDisciplina[];
    periodos: NotaPeriodo[];
    regra: RegraResumo;
}

export interface Avaliacao {
    id: number;
    periodoAvaliativoId: number;
    nome: string;
    tipo: TipoAvaliacao;
    data: string | null;
    peso: number;
    valorMaximo: number | null;
    recuperacao: boolean;
    notasLancadas: number;
}

export interface NotaValor {
    avaliacaoId: number;
    valor: number;
}

export interface AlunoNotasPeriodo {
    alunoId: number;
    matricula: string;
    nome: string;
    saiuEm: string | null;
    inativo: boolean;
    notas: NotaValor[];
    media: number | null;
    recuperacao: number | null;
    notaPeriodo: number | null;
    pendentes: number;
    abaixoDaMedia: boolean;
}

export interface NotasPeriodo {
    turmaId: number;
    disciplinaId: number;
    periodo: NotaPeriodo;
    turmaAtiva: boolean;
    regra: RegraResumo;
    avaliacoes: Avaliacao[];
    alunos: AlunoNotasPeriodo[];
}

export interface NotaDoPeriodo {
    periodoId: number;
    nota: number | null;
    pendentes: number;
}

export interface AlunoMedia {
    alunoId: number;
    matricula: string;
    nome: string;
    inativo: boolean;
    periodos: NotaDoPeriodo[];
    mediaFinal: number | null;
    completa: boolean;
    situacao: SituacaoMedia;
}

export interface Medias {
    turmaId: number;
    disciplinaId: number;
    regra: RegraResumo;
    periodos: NotaPeriodo[];
    alunos: AlunoMedia[];
}

export const tiposAvaliacao: { value: TipoAvaliacao; label: string }[] = [
    { value: 'PROVA', label: 'Prova' },
    { value: 'TRABALHO', label: 'Trabalho' },
    { value: 'ATIVIDADE', label: 'Atividade' },
    { value: 'PARTICIPACAO', label: 'Participação' },
    { value: 'OUTRO', label: 'Outro' },
    { value: 'RECUPERACAO', label: 'Recuperação' },
];

export const tipoAvaliacaoLabel = (tipo: TipoAvaliacao) => tiposAvaliacao.find(t => t.value === tipo)?.label ?? tipo;

export const situacaoMediaLabels: Record<SituacaoMedia, string> = {
    SEM_NOTAS: 'Sem notas',
    EM_ANDAMENTO: 'Em andamento',
    MEDIA_ATINGIDA: 'Média atingida',
    ABAIXO_DA_MEDIA: 'Abaixo da média',
};

export const calculoLabels: Record<CalculoNotaPeriodo, string> = {
    MEDIA_PONDERADA: 'Média ponderada das avaliações',
    SOMA: 'Soma dos pontos das avaliações',
};

/** Número em pt-BR com até 2 casas (ex.: 7,5). */
export function formatarNumero(valor: number | null | undefined, casas?: number): string {
    if (valor === null || valor === undefined) return '—';
    // Sem separador de milhar: o texto também preenche os campos de nota, que o lerNota precisa entender.
    return valor.toLocaleString('pt-BR', {
        minimumFractionDigits: casas ?? 0,
        maximumFractionDigits: casas ?? 2,
        useGrouping: false,
    });
}

/** Texto digitado ("7,5", "7.5", "") → número; nulo quando vazio; NaN quando inválido. */
export function lerNota(texto: string): number | null {
    const limpo = texto.trim().replace(',', '.');
    if (limpo === '') return null;
    return /^\d+(\.\d{0,2})?$/.test(limpo) ? Number(limpo) : Number.NaN;
}

/** yyyy-mm-dd → dd/mm/yyyy, sem passar por Date (evita deslocamento de fuso). */
export function formatarData(iso: string): string {
    const [ano, mes, dia] = iso.slice(0, 10).split('-');
    return `${dia}/${mes}/${ano}`;
}

/** Descrição curta da regra (ex.: "Notas de 0 a 10 · média 6 · média ponderada"). */
export function descreverRegra(regra: RegraResumo): string {
    const calculo = regra.calculoNotaPeriodo === 'SOMA' ? 'soma de pontos' : 'média ponderada';
    const recuperacao = regra.permiteRecuperacao ? ' · com recuperação' : '';
    return `Notas de 0 a ${formatarNumero(regra.notaMaxima)} · média ${formatarNumero(regra.mediaAprovacao)} · ${calculo}${recuperacao}`;
}
