namespace Wordle.Server.Core.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int CurrentStreak { get; set; }

    public int MaxStreak { get; set; }

    public int TotalGamesPlayed { get; set; }

    public int TotalGamesWon { get; set; }

    public bool IsDeleted { get; set; } = false;
}
