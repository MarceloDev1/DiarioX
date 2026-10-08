namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Sistema de avaliação aplicado às etapas de ensino vinculadas: escala das notas, média para
/// aprovação, arredondamento, forma de compor a nota do período e recuperação paralela.
/// Etapas sem regra usam <see cref="PadraoDoSistema"/>.
/// </summary>
public class RegraAvaliacao : ITenantEntity
{
    /// <summary>Nota do período = média das avaliações, ponderada pelos pesos.</summary>
    public const string CalculoMediaPonderada = "MEDIA_PONDERADA";

    /// <summary>Nota do período = soma dos pontos das avaliações (cada uma vale até o seu valor máximo).</summary>
    public const string CalculoSoma = "SOMA";

    public static readonly IReadOnlySet<string> Calculos = new HashSet<string> { CalculoMediaPonderada, CalculoSoma };

    /// <summary>A nota da recuperação substitui a média do período.</summary>
    public const string SubstituiMedia = "SUBSTITUI_MEDIA";

    /// <summary>A nota do período passa a ser a média aritmética entre a média das avaliações e a recuperação.</summary>
    public const string MediaComRecuperacao = "MEDIA_COM_RECUPERACAO";

    /// <summary>A recuperação substitui a média, limitada à média de aprovação.</summary>
    public const string LimitadaAMediaAprovacao = "LIMITADA_A_MEDIA_APROVACAO";

    public static readonly IReadOnlySet<string> Substituicoes =
        new HashSet<string> { SubstituiMedia, MediaComRecuperacao, LimitadaAMediaAprovacao };

    public const decimal NotaMaximaLimite = 1000m;
    public const int CasasDecimaisMaximo = 2;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal NotaMaxima { get; set; } = 10m;
    public decimal MediaAprovacao { get; set; } = 6m;

    /// <summary>Casas decimais das notas de período e médias calculadas (arredondamento comercial).</summary>
    public int CasasDecimais { get; set; } = 1;

    public string CalculoNotaPeriodo { get; set; } = CalculoMediaPonderada;

    /// <summary>
    /// Permite uma avaliação de recuperação por período: a nota do período passa a ser a maior entre
    /// a média das avaliações e a nota da recuperação.
    /// </summary>
    public bool PermiteRecuperacao { get; set; } = true;

    /// <summary>
    /// Como a recuperação entra na nota do período quando é superior à média das avaliações (RN02). Quando não é
    /// superior, a nota do período continua sendo a média.
    /// </summary>
    public string SubstituicaoRecuperacao { get; set; } = SubstituiMedia;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EtapaEnsino> Etapas { get; set; } = new List<EtapaEnsino>();

    public static RegraAvaliacao PadraoDoSistema() => new()
    {
        Nome = "Padrão do sistema",
        NotaMaxima = 10m,
        MediaAprovacao = 6m,
        CasasDecimais = 1,
        CalculoNotaPeriodo = CalculoMediaPonderada,
        PermiteRecuperacao = true,
        SubstituicaoRecuperacao = SubstituiMedia,
    };
}
