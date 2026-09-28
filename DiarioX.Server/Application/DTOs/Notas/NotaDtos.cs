namespace DiarioX.Server.Application.DTOs.Notas;

/// <summary>Regra aplicada à turma. Id nulo = regra padrão do sistema (etapa sem regra própria).</summary>
public record RegraAvaliacaoResumoResponse(
    int? Id,
    string Nome,
    decimal NotaMaxima,
    decimal MediaAprovacao,
    int CasasDecimais,
    string CalculoNotaPeriodo,
    bool PermiteRecuperacao
);

// ---------- Seleção de turma/disciplina ----------

public record NotaDisciplinaResponse(int Id, string Nome);

public record NotaPeriodoResponse(int Id, string Nome, int Numero, DateOnly DataInicio, DateOnly DataTermino);

public record NotaTurmaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int AnoReferencia,
    string Turno,
    bool Ativa,
    IReadOnlyList<NotaDisciplinaResponse> Disciplinas,
    IReadOnlyList<NotaPeriodoResponse> Periodos,
    RegraAvaliacaoResumoResponse Regra
);

// ---------- Lançamento do período ----------

public record AvaliacaoResponse(
    int Id,
    int PeriodoAvaliativoId,
    string Nome,
    string Tipo,
    DateOnly? Data,
    decimal Peso,
    decimal? ValorMaximo,
    bool Recuperacao,
    int NotasLancadas
);

public record NotaValorResponse(int AvaliacaoId, decimal Valor);

/// <param name="SaiuEm">Fim da enturmação quando o aluno deixou a turma durante o período.</param>
public record AlunoNotasPeriodoResponse(
    int AlunoId,
    string Matricula,
    string Nome,
    DateOnly? SaiuEm,
    bool Inativo,
    IReadOnlyList<NotaValorResponse> Notas,
    decimal? Media,
    decimal? Recuperacao,
    decimal? NotaPeriodo,
    int Pendentes,
    bool AbaixoDaMedia
);

public record NotasPeriodoResponse(
    int TurmaId,
    int DisciplinaId,
    NotaPeriodoResponse Periodo,
    bool TurmaAtiva,
    RegraAvaliacaoResumoResponse Regra,
    IReadOnlyList<AvaliacaoResponse> Avaliacoes,
    IReadOnlyList<AlunoNotasPeriodoResponse> Alunos
);

public class AvaliacaoRequest
{
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public int PeriodoAvaliativoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateOnly? Data { get; set; }
    public decimal? Peso { get; set; }
    public decimal? ValorMaximo { get; set; }
}

public class NotaLancamentoRequest
{
    public int AvaliacaoId { get; set; }
    public int AlunoId { get; set; }

    /// <summary>Nulo apaga a nota lançada.</summary>
    public decimal? Valor { get; set; }
}

public class LancamentoNotasRequest
{
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public int PeriodoAvaliativoId { get; set; }
    public List<NotaLancamentoRequest> Notas { get; set; } = new();
}

// ---------- Médias do ano ----------

public record NotaDoPeriodoResponse(int PeriodoId, decimal? Nota, int Pendentes);

public record AlunoMediaResponse(
    int AlunoId,
    string Matricula,
    string Nome,
    bool Inativo,
    IReadOnlyList<NotaDoPeriodoResponse> Periodos,
    decimal? MediaFinal,
    bool Completa,
    string Situacao
);

public record MediasResponse(
    int TurmaId,
    int DisciplinaId,
    RegraAvaliacaoResumoResponse Regra,
    IReadOnlyList<NotaPeriodoResponse> Periodos,
    IReadOnlyList<AlunoMediaResponse> Alunos
);

// ---------- Mapa de notas da turma (relatório) ----------

public record MapaNotasAluno(string Matricula, string Nome, IReadOnlyList<decimal?> Medias, int AbaixoDaMedia);

/// <summary>Média atual de cada aluno em cada disciplina (na ordem de Disciplinas).</summary>
public record MapaNotasResponse(
    RegraAvaliacaoResumoResponse Regra,
    IReadOnlyList<NotaDisciplinaResponse> Disciplinas,
    IReadOnlyList<MapaNotasAluno> Alunos
);

// ---------- Resultado ----------

public enum NotaResultError
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public record NotaResult<T>(T? Value, string Message = "", NotaResultError Error = NotaResultError.None)
{
    public bool Success => Error == NotaResultError.None;
}
