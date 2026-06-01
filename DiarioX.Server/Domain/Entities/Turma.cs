namespace DiarioX.Server.Domain.Entities;

public class Turma
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public const string TurnoManha = "MANHA";
    public const string TurnoTarde = "TARDE";
    public const string TurnoNoite = "NOITE";
    public const string TurnoIntegral = "INTEGRAL";

    public int Id { get; set; }
    public int AnoLetivoId { get; set; }
    public AnoLetivo AnoLetivo { get; set; } = null!;
    public int EscolaId { get; set; }
    public Escola Escola { get; set; } = null!;
    public int ModalidadeEnsinoId { get; set; }
    public ModalidadeEnsino ModalidadeEnsino { get; set; } = null!;
    public int EtapaEnsinoId { get; set; }
    public EtapaEnsino EtapaEnsino { get; set; } = null!;
    public string NomeIdentificador { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string Turno { get; set; } = TurnoManha;
    public int VagasOfertadas { get; set; }
    public string Status { get; set; } = StatusAtivo;
}