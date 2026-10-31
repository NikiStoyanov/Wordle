namespace Wordle.Server.Core.DTOs.Game;

public class DailyChallengeStatusDto
{
    public DateTime Date { get; set; }
    public int WordLength { get; set; } = 5;
    public bool HasPlayed { get; set; }
    public bool IsSolved { get; set; }
    public bool IsGameOver { get; set; }
    public int AttemptsUsed { get; set; }
    public List<PreviousGuessDto> PreviousGuesses { get; set; } = new();
    public string? CorrectWord { get; set; }
    public UserStatsDto Stats { get; set; } = new();
    public DateTime NextChallengeUtc { get; set; }
}
