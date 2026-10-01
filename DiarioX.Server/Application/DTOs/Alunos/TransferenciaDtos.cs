namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed class TransferenciaRequest
{
    /// <summary>Data do desligamento: entre o início do ano letivo e hoje.</summary>
    public DateOnly DataTransferencia { get; set; }

    /// <summary>OUTRA_REDE, ENTRE_ESCOLAS_DA_REDE ou MUDANCA_MUNICIPIO_ESTADO.</summary>
    public string? Tipo { get; set; }

    public string? EscolaDestino { get; set; }
    public string? Motivo { get; set; }
}

public sealed record TransferenciaResponse(
    int Id,
    int AlunoId,
    string AlunoNome,
    string Matricula,
    int EscolaOrigemId,
    string EscolaOrigemNome,
    int? TurmaId,
    string? TurmaNome,
    int AnoReferencia,
    DateOnly DataTransferencia,
    string Tipo,
    string TipoDescricao,
    string EscolaDestino,
    string? Motivo,
    DateTime CreatedAt);

public sealed record TransferenciaResult(
    bool Success,
    string Message,
    AlunoResultError? Error = null,
    TransferenciaResponse? Transferencia = null);

public sealed record DeclaracaoTransferenciaArquivo(byte[] Conteudo, string NomeArquivo);
