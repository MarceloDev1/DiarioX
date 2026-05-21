using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailOrCpfAsync(string login);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ExistsByEmailOrCpfAsync(string email, string cpf);
}
