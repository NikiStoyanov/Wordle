namespace Wordle.Server.Core.DTOs.Game;

public class UserStatsDto
{
    public int CurrentStreak { get; set; }
    public int MaxStreak { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int TotalGamesWon { get; set; }
    public double WinPercentage { get; set; }
    public Dictionary<int, int> GuessDistribution { get; set; } = new();
}
