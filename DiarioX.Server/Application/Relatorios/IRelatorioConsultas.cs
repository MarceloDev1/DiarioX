namespace DiarioX.Server.Application.Relatorios;

/// <summary>
/// Consultas agregadas dos relatórios, resolvidas no banco (sem carregar tabelas inteiras).
/// Respeitam a instituição e as escolas do usuário da requisição.
/// </summary>
public interface IRelatorioConsultas
{
    Task<OpcoesRelatorioResponse> ObterOpcoesAsync();
    Task<string?> ObterNomeEscolaAsync(int escolaId);
    Task<int?> ObterAnoReferenciaAsync(int anoLetivoId);
    Task<TurmaDoRelatorio?> ObterTurmaAsync(int turmaId);

    /// <summary>Turmas ativas com as vagas e os alunos (não inativos) enturmados na data.</summary>
    Task<IReadOnlyList<OcupacaoTurmaLinha>> ListarOcupacaoDasTurmasAsync(int? anoLetivoId, int? escolaId, string? turno, DateOnly data);

    /// <summary>Alunos (não inativos) enturmados na turma na data.</summary>
    Task<IReadOnlyList<AlunoDaTurmaLinha>> ListarAlunosDaTurmaAsync(int turmaId, DateOnly data);

    Task<IReadOnlyList<AlunoAguardandoLinha>> ListarAlunosAguardandoEnturmacaoAsync(int? escolaId);
}

public record TurmaDoRelatorio(int Id, string Nome, string Escola, int AnoReferencia, string Turno);

public record OcupacaoTurmaLinha(int TurmaId, string Turma, string Escola, string Etapa, string Turno, int Vagas, int Enturmados);

public record AlunoDaTurmaLinha(
    string Matricula,
    string Nome,
    DateTime DataNascimento,
    string Sexo,
    bool NecessidadeEspecial,
    string Responsavel,
    string TelefoneResponsavel,
    DateOnly EnturmadoEm
);

public record AlunoAguardandoLinha(string Matricula, string Nome, string Escola, DateTime DataNascimento, DateTime CadastradoEm);
