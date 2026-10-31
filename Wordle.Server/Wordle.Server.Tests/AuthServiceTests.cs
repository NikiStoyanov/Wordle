namespace Wordle.Server.Tests;

using Moq;
using Wordle.Server.Core.DTOs.Auth;
using Wordle.Server.Core.Entities;
using Wordle.Server.Core.Interfaces.Repositories;
using Wordle.Server.Core.Interfaces.Services;
using Wordle.Server.Core.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepoMock.Object, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidDto_CreatesUserWithNullEmailAndReturnsToken()
    {
        // Arrange
        var dto = new RegisterDto { Username = "newuser", Password = "password123", Email = "unverified@example.com" };
        _userRepoMock.Setup(r => r.GetByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("unverified@example.com")).ReturnsAsync((User?)null);
        _tokenServiceMock.Setup(t => t.GenerateJwtToken(It.IsAny<User>())).Returns("fake_jwt_token");

        // Act
        var token = await _sut.RegisterAsync(dto);

        // Assert
        Assert.Equal("fake_jwt_token", token);
        // Email must not be assigned on register without verification, but set to PendingEmail
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "newuser" && u.Email == null && u.PendingEmail == "unverified@example.com")), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyUsedByAnotherProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new RegisterDto { Username = "newuser", Password = "password123", Email = "taken@example.com" };
        _userRepoMock.Setup(r => r.GetByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("taken@example.com")).ReturnsAsync(new User { Id = 2, Username = "existing", Email = "taken@example.com" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(dto));
        Assert.Contains("already used by another profile", ex.Message);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_NoEmail_CreatesUserWithNullEmailAndNullPending()
    {
        // Arrange
        var dto = new RegisterDto { Username = "noemailuser", Password = "password123" };
        _userRepoMock.Setup(r => r.GetByUsernameAsync("noemailuser")).ReturnsAsync((User?)null);
        _tokenServiceMock.Setup(t => t.GenerateJwtToken(It.IsAny<User>())).Returns("fake_jwt_token");

        // Act
        var token = await _sut.RegisterAsync(dto);

        // Assert
        Assert.Equal("fake_jwt_token", token);
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "noemailuser" && u.Email == null && u.PendingEmail == null)), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new RegisterDto { Username = "existinguser", Password = "password123" };
        _userRepoMock.Setup(r => r.GetByUsernameAsync("existinguser"))
            .ReturnsAsync(new User { Id = 1, Username = "existinguser" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(dto));
        Assert.Contains("already exists", ex.Message);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_EmailAlreadyUsedByAnotherProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice", Email = "alice@old.com" };
        var otherUser = new User { Id = 2, Username = "bob", Email = "bob@example.com" };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByEmailAsync("bob@example.com")).ReturnsAsync(otherUser);

        var dto = new UpdateEmailDto { Email = "bob@example.com" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RequestEmailChangeAsync(1, dto));
        Assert.Contains("already used by another profile", ex.Message);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_SameEmailAlreadyAssigned_ThrowsArgumentException()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice", Email = "alice@example.com" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new UpdateEmailDto { Email = "alice@example.com" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.RequestEmailChangeAsync(1, dto));
        Assert.Contains("already assigned", ex.Message);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RequestEmailChangeAsync_ValidNewEmail_SetsPendingEmailAndReturnsVerificationCode()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice", Email = null };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@new.com")).ReturnsAsync((User?)null);

        var dto = new UpdateEmailDto { Email = "alice@new.com" };

        // Act
        var result = await _sut.RequestEmailChangeAsync(1, dto);

        // Assert
        Assert.Equal("alice@new.com", result.PendingEmail);
        Assert.NotNull(result.VerificationCode);
        Assert.Equal(6, result.VerificationCode!.Length);
        Assert.Equal("alice@new.com", user.PendingEmail);
        Assert.Equal(result.VerificationCode, user.EmailVerificationCode);
        Assert.NotNull(user.EmailVerificationCodeExpiry);
        // Official Email is still not assigned
        Assert.Null(user.Email);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidCode_AssignsOfficialEmailAndClearsPending()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = null,
            PendingEmail = "alice@verified.com",
            EmailVerificationCode = "123456",
            EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@verified.com")).ReturnsAsync((User?)null);

        var dto = new VerifyEmailDto { Code = "123456" };

        // Act
        var result = await _sut.VerifyEmailAsync(1, dto);

        // Assert
        Assert.Equal("alice@verified.com", result.Email);
        Assert.Equal("alice@verified.com", user.Email);
        Assert.Null(user.PendingEmail);
        Assert.Null(user.EmailVerificationCode);
        Assert.Null(user.EmailVerificationCodeExpiry);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WrongCode_ThrowsArgumentExceptionAndDoesNotAssign()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = null,
            PendingEmail = "alice@verified.com",
            EmailVerificationCode = "123456",
            EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new VerifyEmailDto { Code = "999999" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.VerifyEmailAsync(1, dto));
        Assert.Contains("Invalid verification code", ex.Message);
        Assert.Null(user.Email);
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = null,
            PendingEmail = "alice@verified.com",
            EmailVerificationCode = "123456",
            EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-5) // Expired
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new VerifyEmailDto { Code = "123456" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.VerifyEmailAsync(1, dto));
        Assert.Contains("expired", ex.Message);
        Assert.Null(user.Email);
    }

    [Fact]
    public async Task UpdateUsernameAsync_AvailableUsername_UpdatesAndReturnsNewToken()
    {
        // Arrange
        var user = new User { Id = 1, Username = "oldname" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByUsernameAsync("newname")).ReturnsAsync((User?)null);
        _tokenServiceMock.Setup(t => t.GenerateJwtToken(user)).Returns("new_token_jwt");

        var dto = new UpdateUsernameDto { Username = "newname" };

        // Act
        var token = await _sut.UpdateUsernameAsync(1, dto);

        // Assert
        Assert.Equal("new_token_jwt", token);
        Assert.Equal("newname", user.Username);
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_HashesAndSavesNewPassword()
    {
        // Arrange
        var oldHash = BCrypt.Net.BCrypt.HashPassword("oldpassword123");
        var user = new User { Id = 1, Username = "testuser", PasswordHash = oldHash };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new ChangePasswordDto { CurrentPassword = "oldpassword123", NewPassword = "newpassword456" };

        // Act
        await _sut.ChangePasswordAsync(1, dto);

        // Assert
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword456", user.PasswordHash));
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
