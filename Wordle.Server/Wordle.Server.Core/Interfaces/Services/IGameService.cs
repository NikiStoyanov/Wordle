namespace Wordle.Server.Core.Interfaces.Services;

using DTOs.Game;

public interface IGameService
{
    GuessResultDto EvaluateGuess(string guess, string targetWord);
}