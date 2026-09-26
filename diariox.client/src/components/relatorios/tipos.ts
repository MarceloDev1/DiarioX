export type TipoColuna = 'texto' | 'inteiro' | 'decimal' | 'percentual' | 'data';

/** Chaves de filtro aceitas pelos relatórios (mesmos nomes dos parâmetros da API). */
export type ChaveFiltro = 'anoLetivoId' | 'escolaId' | 'turmaId' | 'turno';

export interface FiltroDefinicao {
    chave: ChaveFiltro;
    rotulo: string;
    obrigatorio: boolean;
}

export interface RelatorioDefinicao {
    id: string;
    nome: string;
    descricao: string;
    categoria: string;
    filtros: FiltroDefinicao[];
}

export interface Opcoes {
    escolas: { id: number; nome: string }[];
    anosLetivos: { id: number; anoReferencia: number }[];
    turmas: { id: number; nome: string; escolaId: number; anoLetivoId: number; turno: string; ativa: boolean }[];
}

export interface ColunaRelatorio {
    titulo: string;
    tipo: TipoColuna;
}

export type ValorCelula = string | number | null;

export interface GraficoRelatorio {
    titulo: string;
    categorias: string[];
    series: { nome: string; valores: number[] }[];
    empilhado: boolean;
}

export interface RelatorioGerado {
    id: string;
    titulo: string;
    instituicao: string;
    geradoEm: string;
    filtros: { rotulo: string; valor: string }[];
    colunas: ColunaRelatorio[];
    linhas: ValorCelula[][];
    indicadores: { rotulo: string; valor: string }[];
    grafico: GraficoRelatorio | null;
}

export const turnos: { value: string; label: string }[] = [
    { value: 'MANHA', label: 'Manhã' },
    { value: 'TARDE', label: 'Tarde' },
    { value: 'NOITE', label: 'Noite' },
    { value: 'INTEGRAL', label: 'Integral' },
];

const numero = new Intl.NumberFormat('pt-BR');
const decimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const percentual = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

export function formatarValor(valor: ValorCelula, tipo: TipoColuna): string {
    if (valor === null || valor === '') return '—';
    switch (tipo) {
        case 'inteiro':
            return numero.format(Number(valor));
        case 'decimal':
            return decimal.format(Number(valor));
        case 'percentual':
            return `${percentual.format(Number(valor))}%`;
        case 'data': {
            const [ano, mes, dia] = String(valor).split('T')[0].split('-');
            return `${dia}/${mes}/${ano}`;
        }
        default:
            return String(valor);
    }
}

export const colunaNumerica = (tipo: TipoColuna) => tipo === 'inteiro' || tipo === 'decimal' || tipo === 'percentual';
