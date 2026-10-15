namespace DiarioX.Server.Application.Alunos;

/// <summary>Dados brutos da movimentação do aluno, de todas as escolas da rede.</summary>
public interface IHistoricoAlunoConsultas
{
    /// <summary>
    /// Nulo se o aluno não existir ou não tiver passado por nenhuma escola do escopo do usuário (pertencer
    /// a ela, ter sido transferido dela ou ter sido enturmado nela).
    /// </summary>
    Task<HistoricoAlunoDados?> ObterAsync(int alunoId);
}

public record HistoricoAlunoDados(
    int AlunoId,
    string Nome,
    string Matricula,
    DateTime CreatedAt,
    int EscolaId,
    string EscolaNome,
    string Status,
    IReadOnlyList<VinculoHistoricoLinha> Vinculos,
    IReadOnlyList<TransferenciaHistoricoLinha> Transferencias);

public record VinculoHistoricoLinha(
    int Id,
    string Turma,
    int EscolaId,
    string Escola,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Motivo,
    string? Observacao);

public record TransferenciaHistoricoLinha(
    int Id,
    DateOnly Data,
    string Tipo,
    int EscolaOrigemId,
    string EscolaOrigem,
    int? EscolaDestinoId,
    string EscolaDestino,
    string? Turma,
    string? Motivo);
