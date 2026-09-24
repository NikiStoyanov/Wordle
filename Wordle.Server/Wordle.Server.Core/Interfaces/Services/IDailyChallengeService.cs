using Wordle.Server.Core.DTOs.Game;

namespace Wordle.Server.Core.Interfaces.Services;

public interface IDailyChallengeService
{
    Task<DailyChallengeResultDto> SubmitGuessAsync(int userId, string guessText);
}
