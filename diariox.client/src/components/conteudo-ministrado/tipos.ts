export type TipoFrequencia = 'POR_AULA' | 'DIARIA';

export interface ConteudoDisciplina {
    id: number;
    nome: string;
}

export interface ConteudoPeriodo {
    id: number;
    nome: string;
    dataInicio: string;
    dataTermino: string;
}

export interface ConteudoTurma {
    turmaId: number;
    turmaNome: string;
    escolaNome: string;
    anoReferencia: number;
    anoLetivoInicio: string;
    anoLetivoTermino: string;
    disciplinas: ConteudoDisciplina[];
    periodos: ConteudoPeriodo[];
    /** POR_AULA: o diário confere a frequência da disciplina. DIARIA: a frequência é da turma no dia. */
    tipoFrequencia: TipoFrequencia;
}

export interface HabilidadeResumo {
    id: number;
    codigo: string;
    descricao: string;
    ativa: boolean;
}

/** Conteúdo de uma data: o registrado (conteudoId preenchido) ou o formulário em branco. */
export interface ConteudoMinistrado {
    conteudoId: number | null;
    turmaId: number;
    disciplinaId: number;
    disciplinaNome: string;
    data: string;
    descricao: string | null;
    habilidades: HabilidadeResumo[];
    registradoPor: string | null;
    registradoEm: string | null;
    atualizadoPor: string | null;
    atualizadoEm: string | null;
    /** EX01: motivo do bloqueio da data pelo Calendário Letivo; nulo = registro permitido. */
    bloqueio: string | null;
}

export interface DiarioFrequencia {
    chamadaId: number;
    quantidadeAulas: number;
    presentes: number;
    faltas: number;
    faltasJustificadas: number;
}

export interface DiarioConteudo {
    id: number;
    disciplinaId: number;
    disciplinaNome: string;
    descricao: string;
    habilidades: HabilidadeResumo[];
}

export type Pendencia = 'SEM_CONTEUDO' | 'SEM_FREQUENCIA';

export interface DiarioDia {
    data: string;
    frequencia: DiarioFrequencia | null;
    conteudos: DiarioConteudo[];
    /** RN01: divergência entre a frequência lançada e o conteúdo ministrado do dia. */
    pendencia: Pendencia | null;
    alerta: string | null;
}

export interface Diario {
    tipoFrequencia: TipoFrequencia;
    de: string;
    ate: string;
    totalPendencias: number;
    dias: DiarioDia[];
}

export const MAX_DESCRICAO = 2000;
