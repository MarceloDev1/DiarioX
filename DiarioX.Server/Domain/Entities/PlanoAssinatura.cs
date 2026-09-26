namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Plano de assinatura do Diário X (entidade global, fora das instituições).
/// Valor mensal = máx(ValorMinimo, ValorFixo + ValorPorAluno × alunos ativos), o que cobre
/// plano fixo (ValorPorAluno = 0), por aluno (ValorFixo = 0), híbrido e piso mínimo.
/// </summary>
public class PlanoAssinatura
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal ValorFixo { get; set; }
    public decimal ValorPorAluno { get; set; }
    public decimal ValorMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public decimal CalcularValor(int alunosAtivos)
        => Math.Round(Math.Max(ValorMinimo, ValorFixo + ValorPorAluno * alunosAtivos), 2, MidpointRounding.AwayFromZero);
}
