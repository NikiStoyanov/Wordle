namespace Wordle.Server.Core.DTOs.Auth;

public class UserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PendingEmail { get; set; }

    public bool HasPendingEmailVerification => !string.IsNullOrEmpty(PendingEmail);

    public int CurrentStreak { get; set; }

    public int MaxStreak { get; set; }

    public int TotalGamesPlayed { get; set; }

    public int TotalGamesWon { get; set; }
}
