namespace DiarioX.Server.Application.DTOs.Dashboard;

/// <summary>
/// Painel da página inicial. Cada bloco só vem preenchido quando o usuário pode ver o módulo de
/// origem dos dados (nulo = sem permissão); tudo respeita a instituição e as escolas do usuário.
/// </summary>
public record DashboardResponse(
    DashboardAlunos? Alunos,
    DashboardTurmas? Turmas,
    DashboardFrequencia? Frequencia,
    DashboardProfessores? Professores,
    IReadOnlyList<DashboardAlerta> Alertas,
    IReadOnlyList<DashboardChamadaRecente>? UltimasChamadas,
    IReadOnlyList<DashboardEvento> Agenda
);

/// <summary>Alunos (não inativos) enturmados hoje e a variação em relação a 30 dias atrás.</summary>
public record DashboardAlunos(int Ativos, decimal? VariacaoPercentual);

public record DashboardTurmas(int Ativas, int Escolas);

/// <summary>Frequência dos últimos dias (aulas ponderadas). Percentual nulo = nenhuma chamada no período.</summary>
public record DashboardFrequencia(decimal? Percentual, decimal Meta, int Dias);

public record DashboardProfessores(int Alocados, int Ativos);

public static class TipoAlerta
{
    public const string Atencao = "atencao";
    public const string Aviso = "aviso";
    public const string Info = "info";
}

/// <summary>Alerta da home. Pagina é o id da página do frontend que resolve o alerta (ex.: "chamada").</summary>
public record DashboardAlerta(string Tipo, string Mensagem, string? Pagina);

public record DashboardChamadaRecente(
    int Id,
    string Turma,
    string Disciplina,
    string? RegistradoPor,
    DateOnly Data,
    int Presentes,
    int Alunos
);

public static class TipoEvento
{
    public const string InicioPeriodo = "inicio-periodo";
    public const string FimPeriodo = "fim-periodo";
    public const string InicioAno = "inicio-ano";
    public const string FimAno = "fim-ano";
}

/// <summary>Marco do calendário letivo (início/fim de ano letivo e de período avaliativo).</summary>
public record DashboardEvento(DateOnly Data, string Tipo, string Titulo, string Descricao);
