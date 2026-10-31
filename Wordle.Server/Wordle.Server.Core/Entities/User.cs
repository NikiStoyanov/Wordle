namespace Wordle.Server.Core.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PendingEmail { get; set; }

    public string? EmailVerificationCode { get; set; }

    public DateTime? EmailVerificationCodeExpiry { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public int CurrentStreak { get; set; }

    public int MaxStreak { get; set; }

    public int TotalGamesPlayed { get; set; }

    public int TotalGamesWon { get; set; }

    public bool IsDeleted { get; set; } = false;
}
