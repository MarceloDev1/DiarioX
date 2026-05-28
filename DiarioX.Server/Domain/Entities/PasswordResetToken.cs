namespace DiarioX.Server.Domain.Entities;

public class PasswordResetToken
{
    public const int ExpirationMinutes = 60;

    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public bool IsValid() => UsedAt is null && DateTime.UtcNow < ExpiresAt;

    public static PasswordResetToken Create(int userId, string tokenHash) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
        };
}
