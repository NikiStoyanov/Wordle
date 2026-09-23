namespace Wordle.Server.Core.Interfaces.Repositories;

using Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByUsernameAsync(string username);

    Task AddAsync(User user);

    Task SaveChangesAsync();
}
