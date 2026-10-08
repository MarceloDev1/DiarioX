using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public record NotaInformada(int AvaliacaoId, int AlunoId, decimal? Valor);

public interface IAvaliacaoRepository
{
    /// <summary>Avaliação com as notas (sem rastreamento).</summary>
    Task<Avaliacao?> GetByIdAsync(int id);

    /// <summary>Avaliações da turma/disciplina com as notas; sem período, as do ano todo.</summary>
    Task<IReadOnlyList<Avaliacao>> ListAsync(int turmaId, int disciplinaId, int? periodoId = null);

    /// <summary>Avaliações da turma em todas as disciplinas, com as notas.</summary>
    Task<IReadOnlyList<Avaliacao>> ListByTurmaAsync(int turmaId);

    /// <summary>Enturmações da turma que se sobrepõem ao intervalo, com o aluno.</summary>
    Task<IReadOnlyList<AlunoTurma>> GetEnturmacoesAsync(int turmaId, DateOnly de, DateOnly ate);

    Task<Avaliacao> AddAsync(Avaliacao avaliacao);
    Task UpdateAsync(Avaliacao avaliacao);

    /// <summary>Exclui a avaliação e as notas lançadas nela.</summary>
    Task DeleteAsync(int id);

    /// <summary>Inclui, altera ou remove (valor nulo) as notas informadas.</summary>
    Task SalvarNotasAsync(IReadOnlyCollection<NotaInformada> notas, int usuarioId);
}
