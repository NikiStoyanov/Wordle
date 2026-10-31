using Wordle.Server.Core.DTOs.Game;

namespace Wordle.Server.Core.Interfaces.Services;

public interface IDailyChallengeService
{
    Task<DailyChallengeStatusDto> GetTodayChallengeStatusAsync(int userId, int length = 5);
    Task<DailyChallengeResultDto> SubmitGuessAsync(int userId, string guessText, int? length = null);
    Task<UserStatsDto> GetUserStatsAsync(int userId);
    Task EnsureDailyChallengesAsync(DateTime date);
}
