namespace Wordle.Server.Core.Services;

using DTOs.Auth;
using Entities;
using Interfaces.Repositories;
using Interfaces.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<string> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingUser != null)
        {
            throw new Exception("User with this name already exists.");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var newUser = new User
        {
            Username = dto.Username,
            PasswordHash = passwordHash,
        };

        await _userRepository.AddAsync(newUser);

        await _userRepository.SaveChangesAsync();

        return _tokenService.GenerateJwtToken(newUser);
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user == null)
        {
            throw new Exception("Invalid username or password.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new Exception("Invalid username or password.");
        }

        return _tokenService.GenerateJwtToken(user);
    }
}
