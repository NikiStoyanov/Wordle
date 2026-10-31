namespace Wordle.Server.Core.DTOs.Game;

public class PreviousGuessDto
{
    public string Guess { get; set; } = string.Empty;
    public LetterEvaluationDto[] Evaluations { get; set; } = Array.Empty<LetterEvaluationDto>();
}
