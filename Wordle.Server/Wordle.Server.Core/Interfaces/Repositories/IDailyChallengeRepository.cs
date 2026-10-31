using Wordle.Server.Core.Entities;

namespace Wordle.Server.Core.Interfaces.Repositories;

public interface IDailyChallengeRepository
{
    Task<DailyChallenge?> GetByDateAsync(DateTime date);

    Task<DailyChallenge?> GetByDateAndLengthAsync(DateTime date, int length);

    Task<List<DailyChallenge>> GetAllByDateAsync(DateTime date);

    Task AddAsync(DailyChallenge challenge);

    Task SaveChangesAsync();
}
