using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailOrCpfAsync(string login)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                EF.Functions.ILike(u.Email, login) ||
                EF.Functions.ILike(u.Cpf, login));
    }

    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user != null)
        {
            await DeleteAsync(user);
        }
    }

    public async Task<bool> ExistsByEmailOrCpfAsync(string email, string cpf)
    {
        return await _dbSet
            .AnyAsync(u =>
                EF.Functions.ILike(u.Email, email) ||
                EF.Functions.ILike(u.Cpf, cpf));
    }
}
