using System.ComponentModel.DataAnnotations;

namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed class RemanejamentoAlunoRequest
{
    [Range(1, int.MaxValue)]
    public int TurmaDestinoId { get; set; }

    public DateOnly DataMovimentacao { get; set; }
}