using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(AppDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public new async Task AddAsync(PasswordResetToken token)
    {
        await _dbSet.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public new async Task UpdateAsync(PasswordResetToken token)
    {
        _dbSet.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task InvalidatePreviousTokensAsync(int userId)
    {
        var tokens = await _dbSet
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in tokens)
            token.UsedAt = DateTime.UtcNow;

        if (tokens.Count > 0)
            await _context.SaveChangesAsync();
    }
}
