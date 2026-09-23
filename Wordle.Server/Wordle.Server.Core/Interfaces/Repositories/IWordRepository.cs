namespace Wordle.Server.Core.Interfaces.Repositories;

using Entities;

public interface IWordRepository
{
    Task<bool> ExistsAsync(string text);

    Task<Word?> GetRandomWordAsync(int length);

    Task AddAsync(Word word);

    Task SaveChangesAsync();
}
