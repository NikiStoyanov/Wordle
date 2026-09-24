namespace Wordle.Server.Core.Services;

using DTOs.Game;
using Interfaces.Services;
using Wordle.Server.Core.Enums;

public class GameService : IGameService
{
    public GuessResultDto EvaluateGuess(string guess, string targetWord)
    {
        if (guess.Length != targetWord.Length)
        {
            throw new ArgumentException("Дължината на опита не съвпада с търсената дума.");
        }

        var guessUpper = guess.ToUpper();
        var targetUpper = targetWord.ToUpper();
        var length = targetUpper.Length;

        var result = new GuessResultDto
        {
            IsWon = guessUpper == targetUpper,
            Evaluations = new LetterEvaluationDto[length]
        };

        var letterCounts = new Dictionary<char, int>();
        foreach (var c in targetUpper)
        {
            if (!letterCounts.TryAdd(c, 1))
            {
                letterCounts[c]++;
            }
        }

        for (int i = 0; i < length; i++)
        {
            if (guessUpper[i] == targetUpper[i])
            {
                result.Evaluations[i] = new LetterEvaluationDto(guessUpper[i], LetterState.Correct);
                letterCounts[guessUpper[i]]--;
            }
        }

        for (int i = 0; i < length; i++)
        {
            if (result.Evaluations[i] != null)
            {
                continue;
            }

            var currentLetter = guessUpper[i];

            if (letterCounts.ContainsKey(currentLetter) && letterCounts[currentLetter] > 0)
            {
                result.Evaluations[i] = new LetterEvaluationDto(currentLetter, LetterState.Present);
                letterCounts[currentLetter]--;
            }
            else
            {
                result.Evaluations[i] = new LetterEvaluationDto(currentLetter, LetterState.Absent);
            }
        }

        return result;
    }
}
