namespace Wordle.Server.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Core.Entities;
using Core.Interfaces.Repositories;

public class WordRepository : IWordRepository
{
    WordleDbContext _context;

    public WordRepository(WordleDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Word word)
    {
        word.Length = word.Text.Length;
        word.Text = word.Text.ToUpper();

        await _context.Words.AddAsync(word);
    }

    public async Task<bool> ExistsAsync(string text)
    {
        return await _context.Words.AnyAsync(w => w.Text == text.ToUpper());
    }

    public async Task<Word?> GetRandomWordAsync(int length)
    {
        return await _context.Words
            .Where(w => w.Length == length && w.IsActive)
            .OrderBy(w => Guid.NewGuid())
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
