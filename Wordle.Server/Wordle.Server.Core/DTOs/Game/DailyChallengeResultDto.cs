namespace Wordle.Server.Core.DTOs.Game;

public class DailyChallengeResultDto
{
    public bool IsWon { get; set; }
    public bool IsGameOver { get; set; }
    public string? CorrectWord { get; set; }
    public LetterEvaluationDto[] Evaluations { get; set; } = Array.Empty<LetterEvaluationDto>();
    public int AttemptsUsed { get; set; }
}
