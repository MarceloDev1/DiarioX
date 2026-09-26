namespace DiarioX.Server.Application.Dashboard;

/// <summary>
/// Consultas agregadas da página inicial, resolvidas no banco. Respeitam a instituição e as escolas
/// do usuário (filtros globais). Quando professorId é informado, os dados de chamada ficam restritos
/// às turmas/disciplinas em que o professor está alocado.
/// Cada ida ao banco custa a latência da rede: os indicadores saem numa única consulta.
/// </summary>
public interface IDashboardConsultas
{
    Task<int?> ObterProfessorIdDoUsuarioAsync(int usuarioId);

    /// <summary>Todos os contadores do painel numa única consulta.</summary>
    Task<IndicadoresLinha> ObterIndicadoresAsync(ParametrosIndicadores parametros);

    /// <summary>Chamadas mais recentes; RegistradoPor é o nome do professor ou, sem professor vinculado, o e-mail do usuário.</summary>
    Task<IReadOnlyList<ChamadaRecenteLinha>> ListarUltimasChamadasAsync(int quantidade, int? professorId);

    /// <summary>Períodos avaliativos e anos letivos que começam ou terminam no intervalo.</summary>
    Task<IReadOnlyList<MarcoCalendarioLinha>> ListarMarcosDoCalendarioAsync(DateOnly de, DateOnly ate);
}

/// <param name="Hoje">Data de referência dos alunos ativos e das turmas vigentes.</param>
/// <param name="ComparacaoAlunos">Data com que os alunos ativos são comparados.</param>
/// <param name="FrequenciaDe">Início da janela de frequência (o fim é Hoje).</param>
/// <param name="SemChamadaDesde">Turma/disciplina sem nenhuma chamada a partir desta data conta como pendente.</param>
/// <param name="FrequenciaMinima">Percentual abaixo do qual o aluno é infrequente.</param>
/// <param name="MinimoAulasInfrequencia">Aulas registradas mínimas para o aluno entrar na conta de infrequentes.</param>
/// <param name="ProfessorId">Restringe os dados de chamada às alocações do professor.</param>
public record ParametrosIndicadores(
    DateOnly Hoje,
    DateOnly ComparacaoAlunos,
    DateOnly FrequenciaDe,
    DateOnly SemChamadaDesde,
    decimal FrequenciaMinima,
    int MinimoAulasInfrequencia,
    int? ProfessorId
);

public record IndicadoresLinha(
    int AlunosAtivos,
    int AlunosAtivosNaComparacao,
    int AlunosAguardandoEnturmacao,
    int TurmasAtivas,
    int EscolasComTurmas,
    int ProfessoresAlocados,
    int ProfessoresAtivos,
    int AulasRegistradas,
    int Presencas,
    int AlunosInfrequentes,
    int DisciplinasSemChamada,
    int TurmasSemChamada
);

public record ChamadaRecenteLinha(
    int Id,
    string Turma,
    string Disciplina,
    string? RegistradoPor,
    DateOnly Data,
    int Presentes,
    int Alunos
);

/// <summary>Período avaliativo (PeriodoNome preenchido) ou ano letivo, com suas datas.</summary>
public record MarcoCalendarioLinha(int AnoReferencia, string? PeriodoNome, DateOnly Inicio, DateOnly Termino);
