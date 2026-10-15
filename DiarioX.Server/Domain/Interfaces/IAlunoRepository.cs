using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoRepository
{
    Task<Aluno?> GetByIdAsync(int id);
    Task<IReadOnlyList<Aluno>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    Task<IEnumerable<Aluno>> GetAllAsync();

    /// <summary>Alunos que pertencem hoje à escola.</summary>
    Task<IReadOnlyList<Aluno>> GetByEscolaAsync(int escolaId);

    /// <summary>
    /// Alunos que saíram da escola por transferência e hoje pertencem a outra escola da rede. Ignora o
    /// escopo de escolas do usuário: quem chama garante que a escola informada está no escopo.
    /// </summary>
    Task<IReadOnlyList<Aluno>> GetTransferidosDaEscolaAsync(int escolaId);
    Task<bool> ExistsByCpfAsync(string cpf, int? excludeId = null);
    Task<bool> ExistsByNomeDataNascimentoResponsavelAsync(string nome, DateTime dataNascimento, string responsavelNome1, int? excludeId = null);
    Task<bool> ExistsByMatriculaAsync(string matricula);
    Task<int> GetMaxSequencialMatriculaAsync(int ano);
    Task<Aluno> AddAsync(Aluno aluno);
    Task UpdateAsync(Aluno aluno);
    Task DeleteAsync(Aluno aluno);
}
