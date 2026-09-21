using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoRepository
{
    Task<Aluno?> GetByIdAsync(int id);
    Task<IEnumerable<Aluno>> GetAllAsync();
    Task<bool> ExistsByCpfAsync(string cpf, int? excludeId = null);
    Task<bool> ExistsByNomeDataNascimentoResponsavelAsync(string nome, DateTime dataNascimento, string responsavelNome1, int? excludeId = null);
    Task<bool> ExistsByMatriculaAsync(string matricula);
    Task<int> GetMaxSequencialMatriculaAsync(int ano);
    Task<Aluno> AddAsync(Aluno aluno);
    Task UpdateAsync(Aluno aluno);
    Task DeleteAsync(Aluno aluno);
}
