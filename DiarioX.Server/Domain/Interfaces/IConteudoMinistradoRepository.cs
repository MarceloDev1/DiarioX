using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IConteudoMinistradoRepository
{
    Task<ConteudoMinistrado?> GetByIdAsync(int id);
    Task<ConteudoMinistrado?> GetAsync(int turmaId, int disciplinaId, DateOnly data);

    /// <summary>
    /// Conteúdos da turma (com disciplina e habilidades), opcionalmente de uma disciplina e dentro de um
    /// intervalo de datas.
    /// </summary>
    Task<IReadOnlyList<ConteudoMinistrado>> ListAsync(int turmaId, int? disciplinaId, DateOnly? de = null, DateOnly? ate = null);

    Task<ConteudoMinistrado> AddAsync(ConteudoMinistrado conteudo);
    Task UpdateAsync(ConteudoMinistrado conteudo, IEnumerable<int> habilidadeIds);
    Task DeleteAsync(int id);
}
