using System.ComponentModel.DataAnnotations;

namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed class RemanejamentoAlunoRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaDestinoId { get; set; }

    public DateOnly DataMovimentacao { get; set; }

    /// <summary>Opcional; fica registrado no vínculo encerrado.</summary>
    public string? Motivo { get; set; }
}

public sealed class RemanejamentoLoteRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaOrigemId { get; set; }

    [Range(1, int.MaxValue)]
    public int TurmaDestinoId { get; set; }

    public DateOnly DataMovimentacao { get; set; }

    public List<int> AlunoIds { get; set; } = [];

    public string? Motivo { get; set; }
}

public sealed class EnturmacaoAlunoRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaId { get; set; }

    public DateOnly DataInicio { get; set; }
}

public sealed class EnturmacaoLoteRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaId { get; set; }

    public DateOnly DataInicio { get; set; }

    public List<int> AlunoIds { get; set; } = [];
}

public sealed class DesenturmacaoRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaId { get; set; }

    public List<int> AlunoIds { get; set; } = [];

    /// <summary>Um de <c>AlunoTurma.MotivosDesenturmacao</c>.</summary>
    public string? Motivo { get; set; }

    /// <summary>Obrigatória quando o motivo é OUTROS.</summary>
    public string? Observacao { get; set; }
}
