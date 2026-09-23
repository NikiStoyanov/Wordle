namespace Wordle.Server.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Core.Entities;
using Core.Interfaces.Repositories;

public class UserRepository : IUserRepository
{
    private WordleDbContext _context;

    public UserRepository(WordleDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
