using Wordle.Server.Core.Entities;

namespace Wordle.Server.Core.Interfaces.Repositories;

public interface IDailyChallengeRepository
{
    Task<DailyChallenge?> GetByDateAsync(DateTime date);

    Task AddAsync(DailyChallenge challenge);

    Task SaveChangesAsync();
}
