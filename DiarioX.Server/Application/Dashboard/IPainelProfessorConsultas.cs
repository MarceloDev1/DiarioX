using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Dashboard;

/// <summary>
/// Leituras do painel do professor. Respeitam a instituição e as escolas do usuário (filtros globais);
/// as listas são pequenas (as turmas de um professor num ano) e as regras ficam no serviço.
/// </summary>
public interface IPainelProfessorConsultas
{
    /// <summary>Cadastro de professor do usuário (o vinculado ou, sem vínculo, o de mesmo e-mail/CPF).</summary>
    Task<ProfessorPainelLinha?> ObterProfessorDoUsuarioAsync(int usuarioId);

    Task<bool> UsuarioTemPerfilProfessorAsync(int usuarioId);

    /// <summary>Ano letivo de referência informado, com os períodos avaliativos.</summary>
    Task<AnoLetivo?> ObterAnoLetivoAsync(int anoReferencia);

    /// <summary>Alocações ativas do professor em turmas ativas do ano letivo: um diário por turma e disciplina.</summary>
    Task<IReadOnlyList<DiarioLinha>> ListarDiariosAsync(int professorId, int anoLetivoId);

    Task<IReadOnlyList<HorarioAula>> ListarGradeAsync(IReadOnlyCollection<int> turmaIds);

    /// <summary>Calendários publicados do ano letivo (rede e escolas), com os eventos.</summary>
    Task<IReadOnlyList<CalendarioLetivo>> ListarCalendariosPublicadosAsync(int anoLetivoId);

    /// <summary>Chamadas das turmas no intervalo (DisciplinaId nulo = frequência diária).</summary>
    Task<IReadOnlyList<RegistroDiarioLinha>> ListarChamadasAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate);

    /// <summary>Conteúdos ministrados (registros de aula) das turmas no intervalo.</summary>
    Task<IReadOnlyList<RegistroDiarioLinha>> ListarConteudosAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate);

    /// <summary>Avaliações das turmas nos períodos informados, com os alunos que já têm nota.</summary>
    Task<IReadOnlyList<AvaliacaoPainelLinha>> ListarAvaliacoesAsync(IReadOnlyCollection<int> turmaIds, IReadOnlyCollection<int> periodoIds);

    /// <summary>Enturmações das turmas que tocam o intervalo.</summary>
    Task<IReadOnlyList<EnturmacaoLinha>> ListarEnturmacoesAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate);
}

public record ProfessorPainelLinha(int Id, string Nome);

public record DiarioLinha(
    int TurmaId,
    string TurmaNome,
    int EscolaId,
    string EscolaNome,
    string EtapaNome,
    string TipoFrequencia,
    int DisciplinaId,
    string DisciplinaNome
);

public record RegistroDiarioLinha(int TurmaId, int? DisciplinaId, DateOnly Data);

public record AvaliacaoPainelLinha(
    int Id,
    int TurmaId,
    int DisciplinaId,
    int PeriodoAvaliativoId,
    DateOnly? Data,
    bool Recuperacao,
    IReadOnlyList<int> AlunosComNota
);

public record EnturmacaoLinha(int TurmaId, int AlunoId, DateOnly DataInicio, DateOnly? DataFim);
