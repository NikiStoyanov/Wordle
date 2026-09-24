namespace Wordle.Server.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces.Repositories;
using Wordle.Server.Infrastructure.Data;

public class UserDailyAttemptRepository : IUserDailyAttemptRepository
{
    private readonly WordleDbContext _context;

    public UserDailyAttemptRepository(WordleDbContext context)
    {
        _context = context;
    }

    public async Task<UserDailyAttempt?> GetAttemptAsync(int userId, int challengeId)
    {
        return await _context.UserDailyAttempts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.DailyChallengeId == challengeId);
    }

    public async Task AddAsync(UserDailyAttempt attempt)
    {
        await _context.UserDailyAttempts.AddAsync(attempt);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
