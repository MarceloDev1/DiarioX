using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Notas;

/// <summary>Nota de um aluno em uma avaliação do período (Valor nulo = ainda não lançada).</summary>
public record ItemNotaPeriodo(decimal Peso, decimal? Valor, bool Recuperacao);

/// <param name="Media">Resultado das avaliações regulares (média ponderada ou soma), antes da recuperação.</param>
/// <param name="Nota">Nota do período: a média, ou o resultado da regra de substituição quando a recuperação é superior a ela.</param>
/// <param name="Pendentes">Avaliações regulares ainda sem nota para o aluno.</param>
/// <param name="RecuperacaoAplicada">A recuperação elevou a nota do período acima da média (RN02).</param>
public record ResultadoPeriodo(decimal? Media, decimal? Recuperacao, decimal? Nota, int Pendentes, bool RecuperacaoAplicada = false);

public record ResultadoFinal(decimal? Media, bool Completa, string Situacao);

/// <summary>
/// Regras de cálculo das notas. O frontend repete estas regras (notas/calculo.ts) para mostrar a
/// prévia enquanto o professor digita; mantenha os dois em sincronia.
/// </summary>
public static class CalculoNotas
{
    public const string SituacaoSemNotas = "SEM_NOTAS";

    /// <summary>Há períodos sem nota: a média exibida é parcial.</summary>
    public const string SituacaoEmAndamento = "EM_ANDAMENTO";

    public const string SituacaoMediaAtingida = "MEDIA_ATINGIDA";
    public const string SituacaoAbaixoDaMedia = "ABAIXO_DA_MEDIA";

    public static decimal Arredondar(decimal valor, int casas)
        => Math.Round(valor, casas, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Enquanto o período está aberto, a média considera só as avaliações com nota lançada; as demais
    /// aparecem como pendentes.
    /// </summary>
    public static ResultadoPeriodo CalcularPeriodo(RegraAvaliacao regra, IReadOnlyCollection<ItemNotaPeriodo> itens)
    {
        var regulares = itens.Where(i => !i.Recuperacao).ToList();
        var lancadas = regulares.Where(i => i.Valor is not null).ToList();

        decimal? media = null;
        if (lancadas.Count > 0)
        {
            media = regra.CalculoNotaPeriodo == RegraAvaliacao.CalculoSoma
                ? Math.Min(lancadas.Sum(i => i.Valor!.Value), regra.NotaMaxima)
                : lancadas.Sum(i => i.Valor!.Value * i.Peso) / lancadas.Sum(i => i.Peso);
        }

        var recuperacao = regra.PermiteRecuperacao
            ? itens.FirstOrDefault(i => i.Recuperacao && i.Valor is not null)?.Valor
            : null;

        // RN02: recuperação superior à média aplica a regra de substituição da rede; senão vale a média.
        var nota = media is decimal m && recuperacao is decimal r && r > m
            ? AplicarSubstituicao(regra, m, r)
            : media ?? recuperacao;

        return new ResultadoPeriodo(
            media is null ? null : Arredondar(media.Value, regra.CasasDecimais),
            recuperacao,
            nota is null ? null : Arredondar(nota.Value, regra.CasasDecimais),
            regulares.Count - lancadas.Count,
            media is not null && nota > media);
    }

    private static decimal AplicarSubstituicao(RegraAvaliacao regra, decimal media, decimal recuperacao)
        => regra.SubstituicaoRecuperacao switch
        {
            RegraAvaliacao.MediaComRecuperacao => (media + recuperacao) / 2,
            RegraAvaliacao.LimitadaAMediaAprovacao => Math.Max(media, Math.Min(recuperacao, regra.MediaAprovacao)),
            _ => recuperacao,
        };

    /// <summary>Média aritmética das notas dos períodos (as notas já arredondadas, como no boletim).</summary>
    public static ResultadoFinal CalcularFinal(RegraAvaliacao regra, IReadOnlyCollection<decimal?> notasDosPeriodos)
    {
        var lancadas = notasDosPeriodos.Where(n => n is not null).Select(n => n!.Value).ToList();
        if (lancadas.Count == 0)
            return new ResultadoFinal(null, false, SituacaoSemNotas);

        var media = Arredondar(lancadas.Average(), regra.CasasDecimais);
        var completa = lancadas.Count == notasDosPeriodos.Count;
        var situacao = !completa
            ? SituacaoEmAndamento
            : media >= regra.MediaAprovacao ? SituacaoMediaAtingida : SituacaoAbaixoDaMedia;

        return new ResultadoFinal(media, completa, situacao);
    }

    public static bool AbaixoDaMedia(RegraAvaliacao regra, decimal? nota)
        => nota is not null && nota < regra.MediaAprovacao;
}
