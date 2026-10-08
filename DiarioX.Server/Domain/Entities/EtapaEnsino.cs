namespace DiarioX.Server.Domain.Entities;

public class EtapaEnsino : ITenantEntity
{
    /// <summary>Anos Finais (especialista): a frequência é lançada por disciplina, na aula/tempo de aula.</summary>
    public const string FrequenciaPorAula = "POR_AULA";

    /// <summary>Anos Iniciais (polivalente): uma única frequência por dia para a turma.</summary>
    public const string FrequenciaDiaria = "DIARIA";

    public static readonly IReadOnlyList<string> TiposFrequencia = [FrequenciaPorAula, FrequenciaDiaria];

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ModalidadeEnsinoId { get; set; }
    public ModalidadeEnsino ModalidadeEnsino { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public int OrdemCronologica { get; set; }
    public int? IdadeRecomendada { get; set; }

    /// <summary>RF017 RN02: como a frequência das turmas da etapa é registrada.</summary>
    public string TipoFrequencia { get; set; } = FrequenciaPorAula;
    /// <summary>Regra de avaliação das turmas da etapa; nula = regra padrão do sistema.</summary>
    public int? RegraAvaliacaoId { get; set; }
    public RegraAvaliacao? RegraAvaliacao { get; set; }
}
