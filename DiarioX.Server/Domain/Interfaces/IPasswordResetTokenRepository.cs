using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task AddAsync(PasswordResetToken token);
    Task UpdateAsync(PasswordResetToken token);
    Task InvalidatePreviousTokensAsync(int userId);
}
