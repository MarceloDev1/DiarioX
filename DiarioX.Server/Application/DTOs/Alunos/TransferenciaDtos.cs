namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed class TransferenciaRequest
{
    /// <summary>Data do desligamento: entre o início do ano letivo e hoje.</summary>
    public DateOnly DataTransferencia { get; set; }

    /// <summary>OUTRA_REDE, ENTRE_ESCOLAS_DA_REDE ou MUDANCA_MUNICIPIO_ESTADO.</summary>
    public string? Tipo { get; set; }

    /// <summary>Nome da escola de destino, para transferências para fora da rede.</summary>
    public string? EscolaDestino { get; set; }

    /// <summary>Escola de destino na transferência entre escolas da rede.</summary>
    public int? EscolaDestinoId { get; set; }

    public string? Motivo { get; set; }
}

public sealed record EscolaDestinoResponse(int Id, string Nome);

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
    DateTime CreatedAt,
    int? EscolaDestinoId = null);

/// <summary>
/// Linha da lista de alunos transferidos. Modalidade, etapa, turma e turno são nulos quando o aluno
/// aguardava enturmação ao ser transferido.
/// </summary>
public sealed record TransferenciaListaItemResponse(
    int Id,
    int AlunoId,
    string Matricula,
    string AlunoNome,
    int EscolaId,
    string EscolaNome,
    int? ModalidadeEnsinoId,
    string? ModalidadeEnsinoNome,
    int? EtapaEnsinoId,
    string? EtapaEnsinoNome,
    int? TurmaId,
    string? TurmaNomeIdentificador,
    string? TurmaNomeCompleto,
    string? Turno,
    DateOnly DataTransferencia);

public sealed record TransferenciaResult(
    bool Success,
    string Message,
    AlunoResultError? Error = null,
    TransferenciaResponse? Transferencia = null);

public sealed record DeclaracaoTransferenciaArquivo(byte[] Conteudo, string NomeArquivo);
