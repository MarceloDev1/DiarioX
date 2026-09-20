using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IProfessorRepository
{
    Task<Professor?> GetByIdAsync(int id);
    Task<Professor?> GetByCpfAsync(string cpf);
    Task<Professor?> GetByMatriculaAsync(string matricula);
    Task<IEnumerable<Professor>> GetAllAsync();
    Task<IEnumerable<Professor>> GetByEscolaIdAsync(int escolaId);
    Task<bool> ExistsByCpfAsync(string cpf);
    Task<bool> ExistsByMatriculaAsync(string matricula);
    Task<bool> ExistsByEmailAsync(string email);
    Task<Professor> AddAsync(Professor professor);
    Task UpdateAsync(Professor professor);
    Task DeleteAsync(Professor professor);
    Task AddDisciplinaAsync(int professorId, int disciplinaId);
    Task RemoveDisciplinasAsync(int professorId);
}
