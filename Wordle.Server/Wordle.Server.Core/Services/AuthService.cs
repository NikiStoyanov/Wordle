namespace Wordle.Server.Core.Services;

using System.Security.Cryptography;
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
        var cleanUsername = dto.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cleanUsername) || cleanUsername.Length < 3)
        {
            throw new ArgumentException("Username must be at least 3 characters long.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long.");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(cleanUsername);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this name already exists.");
        }

        string? pendingEmail = null;
        string? verificationCode = null;
        DateTime? verificationCodeExpiry = null;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var cleanEmail = dto.Email.Trim().ToLower();
            var existingWithEmail = await _userRepository.GetByEmailAsync(cleanEmail);
            if (existingWithEmail != null)
            {
                throw new InvalidOperationException("This email is already used by another profile.");
            }

            pendingEmail = cleanEmail;
            verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            verificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // When a user registers, we do not directly assign an unverified email as their official Email.
        // If an email is provided during registration, it is stored as PendingEmail until verified.
        var newUser = new User
        {
            Username = cleanUsername,
            Email = null,
            PendingEmail = pendingEmail,
            EmailVerificationCode = verificationCode,
            EmailVerificationCodeExpiry = verificationCodeExpiry,
            PasswordHash = passwordHash,
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        return _tokenService.GenerateJwtToken(newUser);
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var cleanUsername = dto.Username?.Trim() ?? string.Empty;
        var user = await _userRepository.GetByUsernameAsync(cleanUsername);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return _tokenService.GenerateJwtToken(user);
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return MapToDto(user);
    }

    public async Task<EmailVerificationInitiatedDto> RequestEmailChangeAsync(int userId, UpdateEmailDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var cleanEmail = dto.Email.Trim().ToLower();

        // 1. Check if email is already assigned to this profile
        if (string.Equals(user.Email, cleanEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("This email is already assigned to your profile.");
        }

        // 2. Check if email is already in use by another profile
        var existingWithEmail = await _userRepository.GetByEmailAsync(cleanEmail);
        if (existingWithEmail != null && existingWithEmail.Id != userId)
        {
            throw new InvalidOperationException("This email is already used by another profile.");
        }

        // Generate 6-digit numeric verification code
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.PendingEmail = cleanEmail;
        user.EmailVerificationCode = code;
        user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);

        await _userRepository.SaveChangesAsync();

        return new EmailVerificationInitiatedDto
        {
            PendingEmail = cleanEmail,
            Message = "Verification code generated. Please verify to assign this email.",
            VerificationCode = code // Returned for user preview / mock delivery
        };
    }

    public async Task<UserDto> VerifyEmailAsync(int userId, VerifyEmailDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.PendingEmail) || string.IsNullOrWhiteSpace(user.EmailVerificationCode))
        {
            throw new InvalidOperationException("No pending email verification found.");
        }

        if (user.EmailVerificationCodeExpiry == null || DateTime.UtcNow > user.EmailVerificationCodeExpiry.Value)
        {
            throw new InvalidOperationException("Verification code has expired. Please request a new one.");
        }

        if (!string.Equals(user.EmailVerificationCode, dto.Code.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid verification code.");
        }

        // Re-check that the email hasn't been claimed by someone else in the meantime
        var existingWithEmail = await _userRepository.GetByEmailAsync(user.PendingEmail);
        if (existingWithEmail != null && existingWithEmail.Id != userId)
        {
            user.PendingEmail = null;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiry = null;
            await _userRepository.SaveChangesAsync();
            throw new InvalidOperationException("This email has already been claimed by another profile.");
        }

        // Successfully verified: assign official email and clear pending state
        user.Email = user.PendingEmail;
        user.PendingEmail = null;
        user.EmailVerificationCode = null;
        user.EmailVerificationCodeExpiry = null;

        await _userRepository.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<string> UpdateUsernameAsync(int userId, UpdateUsernameDto dto)
    {
        var cleanUsername = dto.Username.Trim();
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!string.Equals(user.Username, cleanUsername, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepository.GetByUsernameAsync(cleanUsername);
            if (existing != null && existing.Id != userId)
            {
                throw new InvalidOperationException("Username is already taken.");
            }
        }

        user.Username = cleanUsername;
        await _userRepository.SaveChangesAsync();

        // Return a new JWT token reflecting the updated username
        return _tokenService.GenerateJwtToken(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        bool isCurrentValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid)
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            throw new ArgumentException("New password must be at least 6 characters long.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.SaveChangesAsync();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PendingEmail = user.PendingEmail,
            CurrentStreak = user.CurrentStreak,
            MaxStreak = user.MaxStreak,
            TotalGamesPlayed = user.TotalGamesPlayed,
            TotalGamesWon = user.TotalGamesWon
        };
    }
}
