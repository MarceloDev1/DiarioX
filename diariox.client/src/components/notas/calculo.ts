import type { Avaliacao, RegraResumo } from './tipos';

/**
 * Prévia da nota do período enquanto o professor digita. Repete as regras de
 * DiarioX.Server/Application/Notas/CalculoNotas.cs — mantenha os dois em sincronia. O valor gravado
 * e exibido após salvar é sempre o calculado pelo servidor.
 */

export interface ResultadoPeriodo {
    media: number | null;
    recuperacao: number | null;
    nota: number | null;
    pendentes: number;
    /** RN02: a recuperação elevou a nota do período acima da média. */
    recuperacaoAplicada: boolean;
}

/** Arredondamento comercial (meio para cima), como MidpointRounding.AwayFromZero. */
export function arredondar(valor: number, casas: number): number {
    const fator = 10 ** casas;
    return Math.round((valor + Number.EPSILON) * fator) / fator;
}

/** Na regra de soma, cada avaliação vale até os seus pontos; nas demais (e na recuperação), até a nota máxima. */
export function notaMaximaDa(avaliacao: Pick<Avaliacao, 'recuperacao' | 'valorMaximo'>, regra: RegraResumo): number {
    return regra.calculoNotaPeriodo === 'SOMA' && !avaliacao.recuperacao && avaliacao.valorMaximo !== null
        ? avaliacao.valorMaximo
        : regra.notaMaxima;
}

export function calcularPeriodo(
    regra: RegraResumo,
    avaliacoes: Avaliacao[],
    valorDe: (avaliacaoId: number) => number | null,
): ResultadoPeriodo {
    const regulares = avaliacoes.filter(a => !a.recuperacao);
    const lancadas = regulares
        .map(a => ({ peso: a.peso, valor: valorDe(a.id) }))
        .filter((a): a is { peso: number; valor: number } => a.valor !== null);

    let media: number | null = null;
    if (lancadas.length > 0) {
        media = regra.calculoNotaPeriodo === 'SOMA'
            ? Math.min(lancadas.reduce((soma, a) => soma + a.valor, 0), regra.notaMaxima)
            : lancadas.reduce((soma, a) => soma + a.valor * a.peso, 0) / lancadas.reduce((soma, a) => soma + a.peso, 0);
    }

    const avaliacaoRecuperacao = regra.permiteRecuperacao ? avaliacoes.find(a => a.recuperacao) : undefined;
    const recuperacao = avaliacaoRecuperacao ? valorDe(avaliacaoRecuperacao.id) : null;

    // RN02: recuperação superior à média aplica a regra de substituição da rede; senão vale a média.
    const nota = media !== null && recuperacao !== null && recuperacao > media
        ? aplicarSubstituicao(regra, media, recuperacao)
        : media ?? recuperacao;

    return {
        media: media === null ? null : arredondar(media, regra.casasDecimais),
        recuperacao,
        nota: nota === null ? null : arredondar(nota, regra.casasDecimais),
        pendentes: regulares.length - lancadas.length,
        recuperacaoAplicada: media !== null && nota !== null && nota > media,
    };
}

function aplicarSubstituicao(regra: RegraResumo, media: number, recuperacao: number): number {
    switch (regra.substituicaoRecuperacao) {
        case 'MEDIA_COM_RECUPERACAO':
            return (media + recuperacao) / 2;
        case 'LIMITADA_A_MEDIA_APROVACAO':
            return Math.max(media, Math.min(recuperacao, regra.mediaAprovacao));
        default:
            return recuperacao;
    }
}

export const abaixoDaMedia = (regra: RegraResumo, nota: number | null) => nota !== null && nota < regra.mediaAprovacao;
