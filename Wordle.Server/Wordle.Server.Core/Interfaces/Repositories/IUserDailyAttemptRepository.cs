using Wordle.Server.Core.Entities;

namespace Wordle.Server.Core.Interfaces.Repositories;

public interface IUserDailyAttemptRepository
{
    Task<UserDailyAttempt?> GetAttemptAsync(int userId, int challengeId);

    Task<List<UserDailyAttempt>> GetAllAttemptsByUserIdAsync(int userId);

    Task AddAsync(UserDailyAttempt attempt);

    Task SaveChangesAsync();
}
