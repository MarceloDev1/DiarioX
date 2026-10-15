namespace DiarioX.Server.Application.DTOs.Alunos;

/// <summary>
/// Histórico de movimentação do aluno: a situação dele em cada escola da rede por onde passou e as
/// movimentações em ordem cronológica.
/// </summary>
public record HistoricoAlunoResponse(
    int AlunoId,
    string Nome,
    string Matricula,
    IReadOnlyList<SituacaoEscolaResponse> Escolas,
    IReadOnlyList<MovimentacaoAlunoResponse> Movimentacoes);

/// <summary>Situação (código de status do aluno) numa escola; Atual = escola a que o aluno pertence hoje.</summary>
public record SituacaoEscolaResponse(int EscolaId, string Escola, string Situacao, bool Atual);

/// <summary>
/// Uma movimentação. Tipo: CADASTRO, ENTURMACAO, REMANEJAMENTO, DESENTURMACAO, TRANSFERENCIA_SAIDA ou
/// TRANSFERENCIA_ENTRADA. Situacao é o status do aluno na escola depois da movimentação.
/// </summary>
public record MovimentacaoAlunoResponse(
    DateOnly Data,
    string Tipo,
    string Descricao,
    string? Escola,
    string? Turma,
    string? Situacao,
    string? Observacao);
