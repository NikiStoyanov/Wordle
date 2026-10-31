namespace Wordle.Server.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces.Repositories;
using Data;

public class DailyChallengeRepository : IDailyChallengeRepository
{
    private readonly WordleDbContext _context;

    public DailyChallengeRepository(WordleDbContext context)
    {
        _context = context;
    }

    public async Task<DailyChallenge?> GetByDateAsync(DateTime date)
    {
        return await GetByDateAndLengthAsync(date, 5);
    }

    public async Task<DailyChallenge?> GetByDateAndLengthAsync(DateTime date, int length)
    {
        return await _context.DailyChallenges
            .Include(c => c.Word)
            .FirstOrDefaultAsync(c => c.Date.Date == date.Date && c.Length == length);
    }

    public async Task<List<DailyChallenge>> GetAllByDateAsync(DateTime date)
    {
        return await _context.DailyChallenges
            .Include(c => c.Word)
            .Where(c => c.Date.Date == date.Date)
            .OrderBy(c => c.Length)
            .ToListAsync();
    }

    public async Task AddAsync(DailyChallenge challenge)
    {
        await _context.DailyChallenges.AddAsync(challenge);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
