namespace Wordle.Server.Core.Interfaces.Services;

using DTOs.Auth;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDto dto);

    Task<string> LoginAsync(LoginDto dto);

    Task<UserDto> GetCurrentUserAsync(int userId);

    Task<EmailVerificationInitiatedDto> RequestEmailChangeAsync(int userId, UpdateEmailDto dto);

    Task<UserDto> VerifyEmailAsync(int userId, VerifyEmailDto dto);

    Task<string> UpdateUsernameAsync(int userId, UpdateUsernameDto dto);

    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
