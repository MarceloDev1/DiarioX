namespace DiarioX.Server.Application.DTOs.Dashboard;

/// <summary>
/// Home do professor (usuário vinculado a um cadastro de professor): aulas do dia, pendências do mês,
/// diários, gráficos de registros do ano e pendências de notas por período avaliativo.
/// </summary>
/// <param name="SemLotacao">EX02: mensagem exibida nos cards quando não há diário no ano letivo vigente.</param>
/// <param name="EscolaId">Escola filtrada (nulo = todas as escolas do professor).</param>
/// <param name="Notas">Nulo quando o perfil não pode ver o módulo de notas.</param>
public record PainelProfessorResponse(
    string ProfessorNome,
    int AnoReferencia,
    DateOnly? AnoLetivoInicio,
    DateOnly? AnoLetivoTermino,
    string? SemLotacao,
    IReadOnlyList<EscolaPainelResponse> Escolas,
    int? EscolaId,
    AulasDoDiaResponse Hoje,
    PendenciasMesResponse? Pendencias,
    IReadOnlyList<DiarioPainelResponse> Diarios,
    GraficoRegistrosResponse? RegistrosAula,
    GraficoRegistrosResponse? RegistrosFrequencia,
    IReadOnlyList<PeriodoNotasResponse>? Notas
);

public record EscolaPainelResponse(int Id, string Nome);

/// <param name="Evento">Evento do Calendário Letivo no dia (feriado, recesso...), se houver.</param>
/// <param name="Mensagem">EX01/EX02: texto exibido quando não há aulas.</param>
public record AulasDoDiaResponse(
    DateOnly Data,
    bool DiaLetivo,
    string? Evento,
    string? Mensagem,
    IReadOnlyList<AulaDoDiaResponse> Aulas
);

/// <param name="DisciplinaId">Nulo na frequência diária (Anos Iniciais): a aula é da turma no dia.</param>
/// <param name="Tempos">Tempos de aula da grade (1º, 2º...); vazio na frequência diária.</param>
public record AulaDoDiaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int? DisciplinaId,
    string DisciplinaNome,
    IReadOnlyList<int> Tempos,
    bool FrequenciaRegistrada,
    bool AulaRegistrada
);

public static class SituacaoDia
{
    /// <summary>Fora do ano letivo.</summary>
    public const string ForaDoAno = "fora-do-ano";
    public const string NaoLetivo = "nao-letivo";
    /// <summary>Dia letivo sem aula do professor na grade.</summary>
    public const string SemAula = "sem-aula";
    public const string EmDia = "em-dia";
    /// <summary>RN03: dia letivo passado com frequência ou aula sem registro (vermelho no calendário).</summary>
    public const string Pendente = "pendente";
    public const string Hoje = "hoje";
    public const string Futuro = "futuro";
}

public record PendenciasMesResponse(int Ano, int Mes, IReadOnlyList<DiaPendenciaResponse> Dias);

public record DiaPendenciaResponse(
    DateOnly Data,
    string Situacao,
    string? Evento,
    IReadOnlyList<PendenciaAulaResponse> Pendencias
);

public record PendenciaAulaResponse(
    int TurmaId,
    string TurmaNome,
    int? DisciplinaId,
    string DisciplinaNome,
    bool SemFrequencia,
    bool SemAula
);

/// <param name="Diaria">Frequência diária (Anos Iniciais): a chamada é da turma, sem disciplina.</param>
/// <param name="SemGrade">Frequência por aula sem nenhum tempo na grade de horários: não há aulas previstas.</param>
public record DiarioPainelResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    string EtapaNome,
    bool Diaria,
    bool SemGrade,
    IReadOnlyList<DiarioDisciplinaPainelResponse> Disciplinas
);

/// <param name="AulasSemanais">Tempos semanais da disciplina na grade da turma.</param>
public record DiarioDisciplinaPainelResponse(int Id, string Nome, int AulasSemanais);

/// <summary>
/// RN02: fatias do gráfico. Aulas: registradas / previstas no ano letivo (Pendentes = dias passados sem
/// registro; AFazer = de hoje em diante). Frequência: realizadas / dias letivos decorridos (AFazer = 0).
/// </summary>
public record GraficoRegistrosResponse(int Registrados, int Pendentes, int AFazer, decimal? Percentual);

/// <summary>RN01: só o período atual e os anteriores.</summary>
public record PeriodoNotasResponse(
    int Id,
    string Nome,
    int Numero,
    DateOnly DataInicio,
    DateOnly DataTermino,
    DateOnly? PrazoLancamentoNotas,
    bool PrazoEncerrado,
    bool Atual,
    IReadOnlyList<PendenciaNotaResponse> Pendencias
);

/// <param name="NotasFaltantes">Notas não lançadas para alunos enturmados nas avaliações já realizadas.</param>
public record PendenciaNotaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int DisciplinaId,
    string DisciplinaNome,
    int Avaliacoes,
    int NotasFaltantes,
    string Descricao
);
