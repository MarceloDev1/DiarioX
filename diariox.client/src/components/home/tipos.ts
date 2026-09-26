export type TipoAlerta = 'atencao' | 'aviso' | 'info';
export type TipoEvento = 'inicio-periodo' | 'fim-periodo' | 'inicio-ano' | 'fim-ano';

/** Painel da home (GET /api/dashboard). Blocos nulos = o perfil não pode ver o módulo de origem. */
export interface Dashboard {
    alunos: { ativos: number; variacaoPercentual: number | null } | null;
    turmas: { ativas: number; escolas: number } | null;
    frequencia: { percentual: number | null; meta: number; dias: number } | null;
    professores: { alocados: number; ativos: number } | null;
    alertas: { tipo: TipoAlerta; mensagem: string; pagina: string | null }[];
    ultimasChamadas: ChamadaRecente[] | null;
    agenda: EventoAgenda[];
}

export interface ChamadaRecente {
    id: number;
    turma: string;
    disciplina: string;
    registradoPor: string | null;
    data: string;
    presentes: number;
    alunos: number;
}

export interface EventoAgenda {
    data: string;
    tipo: TipoEvento;
    titulo: string;
    descricao: string;
}

const MESES = ['JAN', 'FEV', 'MAR', 'ABR', 'MAI', 'JUN', 'JUL', 'AGO', 'SET', 'OUT', 'NOV', 'DEZ'];

const inteiro = new Intl.NumberFormat('pt-BR');
const umaCasa = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

export const formatarInteiro = (valor: number) => inteiro.format(valor);
export const formatarPercentual = (valor: number) => `${umaCasa.format(valor)}%`;

/** "+3,2%" / "-1,0%". */
export const formatarVariacao = (valor: number) => `${valor > 0 ? '+' : ''}${umaCasa.format(valor)}%`;

/** "2026-09-28" → "28/09/2026". */
export function formatarData(iso: string): string {
    const [ano, mes, dia] = iso.split('T')[0].split('-');
    return `${dia}/${mes}/${ano}`;
}

/** "2026-09-28" → { mes: "SET", dia: "28" }. */
export function partesDaData(iso: string): { mes: string; dia: string } {
    const [, mes, dia] = iso.split('T')[0].split('-');
    return { mes: MESES[Number(mes) - 1], dia };
}
