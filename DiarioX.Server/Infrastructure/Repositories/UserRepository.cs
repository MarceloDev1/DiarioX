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

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, username));
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user != null)
        {
            await DeleteAsync(user);
        }
    }

    public async Task<bool> ExistsAsync(string username)
    {
        return await _dbSet
            .AnyAsync(u => EF.Functions.ILike(u.Username, username));
    }
}
