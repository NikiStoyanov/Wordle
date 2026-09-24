using Wordle.Server.Core.Enums;

namespace Wordle.Server.Core.DTOs.Game;

public class LetterEvaluationDto
{
    public char Letter { get; set; }

    public LetterState State { get; set; }

    public LetterEvaluationDto(char letter, LetterState state)
    {
        Letter = letter;
        State = state;
    }
}
