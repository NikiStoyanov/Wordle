namespace Wordle.Server.Core.Interfaces.Services;

using DTOs.Auth;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDto dto);

    Task<string> LoginAsync(LoginDto dto);
}
