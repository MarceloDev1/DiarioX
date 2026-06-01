namespace DiarioX.Server.Application.DTOs.Turmas;

public class TurmaRequest
{
    public int AnoLetivoId { get; set; }
    public int EscolaId { get; set; }
    public int ModalidadeEnsinoId { get; set; }
    public int EtapaEnsinoId { get; set; }
    public string NomeIdentificador { get; set; } = string.Empty;
    public string Turno { get; set; } = string.Empty;
    public int VagasOfertadas { get; set; }
}