namespace Wordle.Server.Core.DTOs.Game;

public class GuessResultDto
{
    public bool IsWon { get; set; }

    public LetterEvaluationDto[] Evaluations { get; set; } = Array.Empty<LetterEvaluationDto>();
}
