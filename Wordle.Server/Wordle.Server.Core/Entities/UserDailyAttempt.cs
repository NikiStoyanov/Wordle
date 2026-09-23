namespace Wordle.Server.Core.Entities;

public class UserDailyAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int DailyChallengeId { get; set; }

    public DailyChallenge DailyChallenge { get; set; } = null!;

    public int AttemptsCount { get; set; }

    public bool IsSolved { get; set; }

    public string? GuessesState { get; set; }
}
