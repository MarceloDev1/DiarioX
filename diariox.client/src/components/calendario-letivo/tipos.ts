export type TipoEvento =
    | 'FERIADO'
    | 'RECESSO'
    | 'PONTO_FACULTATIVO'
    | 'CONSELHO_CLASSE'
    | 'PLANTAO_PEDAGOGICO'
    | 'FORMACAO_CONTINUADA'
    | 'SABADO_LETIVO';

/** Categoria visual do dia (RN03). */
export type CategoriaDia = 'letivo' | 'fim-de-semana' | 'sem-aula' | 'pedagogico' | 'sabado-letivo';

export interface TipoEventoOpcao {
    value: TipoEvento;
    label: string;
    categoria: Exclude<CategoriaDia, 'letivo' | 'fim-de-semana'>;
    /** Sugestão para "Considerado dia letivo"; nulo = o usuário escolhe. */
    comAulaSugerido: boolean | null;
}

export const tiposEvento: TipoEventoOpcao[] = [
    { value: 'FERIADO', label: 'Feriado', categoria: 'sem-aula', comAulaSugerido: false },
    { value: 'RECESSO', label: 'Recesso Escolar', categoria: 'sem-aula', comAulaSugerido: false },
    { value: 'PONTO_FACULTATIVO', label: 'Ponto Facultativo', categoria: 'sem-aula', comAulaSugerido: false },
    { value: 'CONSELHO_CLASSE', label: 'Conselho de Classe', categoria: 'pedagogico', comAulaSugerido: null },
    { value: 'PLANTAO_PEDAGOGICO', label: 'Plantão Pedagógico', categoria: 'pedagogico', comAulaSugerido: null },
    { value: 'FORMACAO_CONTINUADA', label: 'Formação Continuada', categoria: 'pedagogico', comAulaSugerido: null },
    { value: 'SABADO_LETIVO', label: 'Dia Letivo Especial (Sábado Letivo)', categoria: 'sabado-letivo', comAulaSugerido: true },
];

export interface CalendarioOpcoes {
    anosLetivos: { id: number; anoReferencia: number; dataInicio: string; dataTermino: string }[];
    escolas: { id: number; nome: string }[];
    podeEditarRede: boolean;
}

export interface PeriodoCalendario {
    id: number;
    nome: string;
    numero: number;
    dataInicio: string;
    dataTermino: string;
}

export interface EventoCalendario {
    data: string;
    tipo: TipoEvento;
    tipoNome: string;
    descricao: string;
    comAula: boolean;
    /** Evento da rede herdado pela escola. */
    herdado: boolean;
}

export interface CalendarioLetivo {
    anoLetivoId: number;
    anoReferencia: number;
    dataInicio: string;
    dataTermino: string;
    tipoPeriodo: string;
    periodos: PeriodoCalendario[];
    escolaId: number | null;
    escolaNome: string | null;
    publicado: boolean;
    publicadoEm: string | null;
    redePublicada: boolean;
    diasLetivos: number;
    metaDiasLetivos: number;
    /** Escopo de escolas do usuário; a permissão calendario-letivo.editar é checada à parte. */
    podeEditar: boolean;
    eventos: EventoCalendario[];
}

export interface CalendarioResposta {
    message: string;
    calendario: CalendarioLetivo;
}

/** Cor de cada período (faixas azuis distintas, RN03). */
export const CORES_PERIODOS = ['#93c5fd', '#3b82f6', '#1d4ed8', '#1e3a8a'];

export function categoriaDoDia(data: string, evento: EventoCalendario | undefined): CategoriaDia {
    if (evento) return tiposEvento.find(t => t.value === evento.tipo)?.categoria ?? 'sem-aula';
    return fimDeSemana(data) ? 'fim-de-semana' : 'letivo';
}

export function descreverDia(data: string, evento: EventoCalendario | undefined): string {
    if (evento) {
        const aula = evento.comAula ? 'com aula' : 'sem aula';
        return `${evento.tipoNome}: ${evento.descricao} (${aula})${evento.herdado ? ' · calendário da rede' : ''}`;
    }
    return fimDeSemana(data) ? 'Fim de semana' : 'Dia letivo';
}

// ---------- Datas (yyyy-mm-dd, sempre em UTC para não sofrer com fuso) ----------

export const NOMES_MESES = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];

export const DIAS_SEMANA_CURTOS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
const DIAS_SEMANA = ['domingo', 'segunda-feira', 'terça-feira', 'quarta-feira', 'quinta-feira', 'sexta-feira', 'sábado'];

const paraData = (iso: string) => {
    const [ano, mes, dia] = iso.split('-').map(Number);
    return new Date(Date.UTC(ano, mes - 1, dia));
};

const paraIso = (data: Date) => data.toISOString().slice(0, 10);

export function isoDe(ano: number, mes: number, dia: number): string {
    return paraIso(new Date(Date.UTC(ano, mes, dia)));
}

export function adicionarDias(iso: string, dias: number): string {
    const data = paraData(iso);
    data.setUTCDate(data.getUTCDate() + dias);
    return paraIso(data);
}

export function diaDaSemana(iso: string): number {
    return paraData(iso).getUTCDay();
}

export function nomeDiaDaSemana(iso: string): string {
    return DIAS_SEMANA[diaDaSemana(iso)];
}

export function fimDeSemana(iso: string): boolean {
    const dia = diaDaSemana(iso);
    return dia === 0 || dia === 6;
}

export function diasNoIntervalo(de: string, ate: string): number {
    return Math.round((paraData(ate).getTime() - paraData(de).getTime()) / 86_400_000) + 1;
}

/** Meses (ano, mês 0-11) entre duas datas, inclusive. */
export function mesesEntre(de: string, ate: string): { ano: number; mes: number }[] {
    const meses: { ano: number; mes: number }[] = [];
    let ano = Number(de.slice(0, 4));
    let mes = Number(de.slice(5, 7)) - 1;
    const anoFim = Number(ate.slice(0, 4));
    const mesFim = Number(ate.slice(5, 7)) - 1;
    while (ano < anoFim || (ano === anoFim && mes <= mesFim)) {
        meses.push({ ano, mes });
        mes++;
        if (mes === 12) {
            mes = 0;
            ano++;
        }
    }
    return meses;
}

/** Semanas do mês (domingo a sábado); null nas posições fora do mês. */
export function semanasDoMes(ano: number, mes: number): (string | null)[][] {
    const primeiro = isoDe(ano, mes, 1);
    const totalDias = new Date(Date.UTC(ano, mes + 1, 0)).getUTCDate();
    const celulas: (string | null)[] = Array(diaDaSemana(primeiro)).fill(null);
    for (let dia = 1; dia <= totalDias; dia++)
        celulas.push(isoDe(ano, mes, dia));
    while (celulas.length % 7 !== 0)
        celulas.push(null);

    const semanas: (string | null)[][] = [];
    for (let i = 0; i < celulas.length; i += 7)
        semanas.push(celulas.slice(i, i + 7));
    return semanas;
}

export function formatarData(iso: string): string {
    const [ano, mes, dia] = iso.slice(0, 10).split('-');
    return `${dia}/${mes}/${ano}`;
}

export function formatarDataHora(iso: string): string {
    return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}
