export type CalculoNotaPeriodo = 'MEDIA_PONDERADA' | 'SOMA';

export type TipoAvaliacao = 'PROVA' | 'TRABALHO' | 'ATIVIDADE' | 'PARTICIPACAO' | 'OUTRO' | 'RECUPERACAO';

/** RN02: como a recuperação entra na nota do período quando é superior à média das avaliações. */
export type SubstituicaoRecuperacao = 'SUBSTITUI_MEDIA' | 'MEDIA_COM_RECUPERACAO' | 'LIMITADA_A_MEDIA_APROVACAO';

export type StatusSaida = 'TRANSFERIDO' | 'REMANEJADO' | 'DESENTURMADO';

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
    substituicaoRecuperacao: SubstituicaoRecuperacao;
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
    /** EX02: último dia para lançar avaliações e notas do período; nulo = sem prazo. */
    prazoLancamento: string | null;
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
    /** RN03: aluno que não está mais na turma; as notas salvas ficam somente leitura. */
    statusSaida: StatusSaida | null;
    somenteLeitura: boolean;
    /** RN02: a recuperação elevou a nota do período acima da média das avaliações. */
    recuperacaoAplicada: boolean;
}

export interface NotasPeriodo {
    turmaId: number;
    disciplinaId: number;
    periodo: NotaPeriodo;
    turmaAtiva: boolean;
    regra: RegraResumo;
    avaliacoes: Avaliacao[];
    alunos: AlunoNotasPeriodo[];
    /** EX02: mensagem de prazo expirado; as avaliações e notas do período ficam só para consulta. */
    bloqueio: string | null;
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

export const statusSaidaLabels: Record<StatusSaida, string> = {
    TRANSFERIDO: 'Transferido',
    REMANEJADO: 'Remanejado',
    DESENTURMADO: 'Desenturmado',
};

export const substituicaoLabels: Record<SubstituicaoRecuperacao, string> = {
    SUBSTITUI_MEDIA: 'Substitui a média (a nota da recuperação passa a ser a do período)',
    MEDIA_COM_RECUPERACAO: 'Média entre as avaliações e a recuperação',
    LIMITADA_A_MEDIA_APROVACAO: 'Substitui a média, limitada à média de aprovação',
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

/** Valor máximo como na mensagem de EX01: uma casa decimal no mínimo (10.0). */
export function formatarMaximo(valor: number): string {
    return Number.isInteger(valor * 10) ? valor.toFixed(1) : valor.toFixed(2);
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
