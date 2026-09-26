export interface Plano {
    id: number;
    nome: string;
    descricao: string | null;
    valorFixo: number;
    valorPorAluno: number;
    valorMinimo: number;
    ativo: boolean;
    assinaturas: number;
}

export type SituacaoAssinatura = 'TESTE' | 'ATIVA' | 'CANCELADA';
export type SituacaoFinanceira = 'REGULAR' | 'EM_ATRASO' | 'SOMENTE_LEITURA';
export type SituacaoFatura = 'PENDENTE' | 'PAGA' | 'VENCIDA' | 'CANCELADA' | 'ESTORNADA';

export interface AssinaturaResumo {
    tenantId: number;
    instituicao: string;
    slug: string;
    statusInstituicao: string;
    situacaoFinanceira: SituacaoFinanceira;
    assinaturaId: number | null;
    planoNome: string | null;
    situacao: SituacaoAssinatura | null;
    diaVencimento: number | null;
    testeAte: string | null;
    alunosAtivos: number;
    valorEstimado: number | null;
    faturasEmAberto: number;
    valorEmAberto: number;
}

export interface Assinatura {
    id: number;
    tenantId: number;
    instituicao: string;
    planoId: number;
    planoNome: string;
    situacao: SituacaoAssinatura;
    dataInicio: string;
    testeAte: string | null;
    diaVencimento: number;
    descontoPercentual: number;
    razaoSocial: string;
    cpfCnpj: string;
    email: string;
    telefone: string | null;
    cep: string | null;
    endereco: string | null;
    numero: string | null;
    complemento: string | null;
    bairro: string | null;
    clienteNoAsaas: boolean;
}

export interface Fatura {
    id: number;
    tenantId: number;
    instituicao: string;
    competencia: string;
    vencimento: string;
    alunosAtivos: number;
    valorFixo: number;
    valorPorAluno: number;
    valorMinimo: number;
    valorCalculado: number;
    descontoPercentual: number;
    valor: number;
    situacao: SituacaoFatura;
    diasEmAtraso: number;
    asaasCobrancaId: string | null;
    linkPagamento: string | null;
    pagaEm: string | null;
    valorPago: number | null;
    formaPagamento: string | null;
    notaFiscalSituacao: string | null;
    notaFiscalNumero: string | null;
    notaFiscalPdfUrl: string | null;
    notaFiscalErro: string | null;
    observacao: string | null;
    createdAt: string;
}

export interface Painel {
    asaasConfigurado: boolean;
    notaFiscalHabilitada: boolean;
    diasAntecedenciaFatura: number;
    diasAtrasoSomenteLeitura: number;
    assinaturasAtivas: number;
    assinaturasEmTeste: number;
    receitaMensalEstimada: number;
    faturadoNoMes: number;
    recebidoNoMes: number;
    emAberto: number;
    vencido: number;
    instituicoesEmAtraso: number;
    instituicoesSomenteLeitura: number;
    faturasVencidas: Fatura[];
}

export interface RotinaResultado {
    faturasGeradas: number;
    cobrancasReenviadas: number;
    faturasVencidas: number;
    instituicoesAtualizadas: number;
    erros: string[];
}

/** Respostas de gravação do faturamento: { message, data }. */
export interface Resultado<T> {
    message: string;
    data: T;
}

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export const formatarMoeda = (valor: number) => moeda.format(valor);

/** yyyy-mm-dd → dd/mm/yyyy, sem passar por Date (evita deslocamento de fuso). */
export function formatarData(iso: string | null): string {
    if (!iso) return '—';
    const [ano, mes, dia] = iso.slice(0, 10).split('-');
    return `${dia}/${mes}/${ano}`;
}

/** yyyy-mm-01 → mm/yyyy */
export function formatarCompetencia(iso: string): string {
    const [ano, mes] = iso.split('-');
    return `${mes}/${ano}`;
}

export const situacaoFaturaLabels: Record<SituacaoFatura, string> = {
    PENDENTE: 'Pendente',
    PAGA: 'Paga',
    VENCIDA: 'Vencida',
    CANCELADA: 'Cancelada',
    ESTORNADA: 'Estornada',
};

export const situacaoAssinaturaLabels: Record<SituacaoAssinatura, string> = {
    TESTE: 'Em teste',
    ATIVA: 'Ativa',
    CANCELADA: 'Cancelada',
};

export const situacaoFinanceiraLabels: Record<SituacaoFinanceira, string> = {
    REGULAR: 'Regular',
    EM_ATRASO: 'Em atraso',
    SOMENTE_LEITURA: 'Somente leitura',
};

export const notaFiscalLabels: Record<string, string> = {
    AGENDADA: 'NF agendada',
    EMITIDA: 'NF emitida',
    CANCELADA: 'NF cancelada',
    ERRO: 'Erro na NF',
};

/** Classe do selo de cada situação (reaproveita as cores da chamada). */
export function classeSituacao(situacao: string): string {
    switch (situacao) {
        case 'PAGA':
        case 'ATIVA':
        case 'REGULAR':
        case 'EMITIDA':
            return 'situacao-pill-presente';
        case 'VENCIDA':
        case 'SOMENTE_LEITURA':
        case 'ERRO':
            return 'situacao-pill-falta';
        case 'PENDENTE':
        case 'EM_ATRASO':
        case 'TESTE':
        case 'AGENDADA':
            return 'situacao-pill-falta_justificada';
        default:
            return 'status-inactive';
    }
}

/** Valor do plano para uma quantidade de alunos (mesma fórmula do servidor). */
export function calcularValorPlano(plano: Pick<Plano, 'valorFixo' | 'valorPorAluno' | 'valorMinimo'>, alunos: number): number {
    const valor = Math.max(plano.valorMinimo, plano.valorFixo + plano.valorPorAluno * alunos);
    return Math.round(valor * 100) / 100;
}

export function descreverPlano(plano: Pick<Plano, 'valorFixo' | 'valorPorAluno' | 'valorMinimo'>): string {
    const partes: string[] = [];
    if (plano.valorFixo > 0) partes.push(formatarMoeda(plano.valorFixo));
    if (plano.valorPorAluno > 0) partes.push(`${formatarMoeda(plano.valorPorAluno)} por aluno`);
    const base = partes.length > 0 ? partes.join(' + ') : formatarMoeda(0);
    return plano.valorMinimo > 0 ? `${base} (mínimo ${formatarMoeda(plano.valorMinimo)})` : base;
}
