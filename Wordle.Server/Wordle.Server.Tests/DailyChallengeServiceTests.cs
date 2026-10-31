namespace Wordle.Server.Tests;

using Moq;
using System.Text.Json;
using Wordle.Server.Core.Entities;
using Wordle.Server.Core.Enums;
using Wordle.Server.Core.Interfaces.Repositories;
using Wordle.Server.Core.Interfaces.Services;
using Wordle.Server.Core.Services;
using Wordle.Server.Core.DTOs.Game;

public class DailyChallengeServiceTests
{
    private readonly Mock<IWordRepository> _wordRepoMock = new();
    private readonly Mock<IDailyChallengeRepository> _challengeRepoMock = new();
    private readonly Mock<IUserDailyAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IGameService> _gameServiceMock = new();
    private readonly DailyChallengeService _sut;

    public DailyChallengeServiceTests()
    {
        _sut = new DailyChallengeService(
            _wordRepoMock.Object,
            _challengeRepoMock.Object,
            _attemptRepoMock.Object,
            _userRepoMock.Object,
            _gameServiceMock.Object
        );
    }

    [Fact]
    public async Task SubmitGuessAsync_WhenWonAndYesterdayWon_IncrementsStreak()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            CurrentStreak = 3,
            MaxStreak = 5,
            TotalGamesPlayed = 10,
            TotalGamesWon = 8
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            WordId = 1,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        var yesterdayChallenge = new DailyChallenge
        {
            Id = 99,
            Date = yesterday,
            WordId = 2,
            Word = new Word { Id = 2, Text = "BERRY", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(yesterday)).ReturnsAsync(yesterdayChallenge);

        var yesterdayAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 99,
            IsSolved = true,
            AttemptsCount = 3
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 99)).ReturnsAsync(yesterdayAttempt);

        _wordRepoMock.Setup(r => r.ExistsAsync("APPLE")).ReturnsAsync(true);

        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync((UserDailyAttempt?)null);

        _gameServiceMock.Setup(g => g.EvaluateGuess("APPLE", "APPLE"))
            .Returns(new GuessResultDto
            {
                IsWon = true,
                Evaluations = new LetterEvaluationDto[]
                {
                    new('A', LetterState.Correct),
                    new('P', LetterState.Correct),
                    new('P', LetterState.Correct),
                    new('L', LetterState.Correct),
                    new('E', LetterState.Correct)
                }
            });

        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId))
            .ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var result = await _sut.SubmitGuessAsync(userId, "APPLE");

        // Assert
        Assert.True(result.IsWon);
        Assert.True(result.IsGameOver);
        Assert.Equal(4, user.CurrentStreak);
        Assert.Equal(5, user.MaxStreak);
        Assert.Equal(11, user.TotalGamesPlayed);
        Assert.Equal(9, user.TotalGamesWon);
        Assert.Equal("APPLE", result.CorrectWord);
    }

    [Fact]
    public async Task SubmitGuessAsync_WhenWonAndYesterdayNotWon_ResetsStreakToOne()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            CurrentStreak = 5,
            MaxStreak = 5,
            TotalGamesPlayed = 10,
            TotalGamesWon = 8
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            WordId = 1,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        // Yesterday challenge exists, but user didn't solve it
        var yesterdayChallenge = new DailyChallenge
        {
            Id = 99,
            Date = yesterday,
            WordId = 2,
            Word = new Word { Id = 2, Text = "BERRY", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(yesterday)).ReturnsAsync(yesterdayChallenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 99)).ReturnsAsync((UserDailyAttempt?)null);

        _wordRepoMock.Setup(r => r.ExistsAsync("APPLE")).ReturnsAsync(true);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync((UserDailyAttempt?)null);

        _gameServiceMock.Setup(g => g.EvaluateGuess("APPLE", "APPLE"))
            .Returns(new GuessResultDto { IsWon = true, Evaluations = Array.Empty<LetterEvaluationDto>() });

        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId))
            .ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var result = await _sut.SubmitGuessAsync(userId, "APPLE");

        // Assert
        Assert.True(result.IsWon);
        Assert.Equal(1, user.CurrentStreak);
        Assert.Equal(5, user.MaxStreak);
        Assert.Equal(11, user.TotalGamesPlayed);
        Assert.Equal(9, user.TotalGamesWon);
    }

    [Fact]
    public async Task SubmitGuessAsync_WhenLostAtAttempt6_ResetsStreakToZero()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            CurrentStreak = 4,
            MaxStreak = 6,
            TotalGamesPlayed = 10,
            TotalGamesWon = 8
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            WordId = 1,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        var existingAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 100,
            AttemptsCount = 5,
            IsSolved = false,
            GuessesState = JsonSerializer.Serialize(new List<string> { "PLANT", "CRANE", "GHOST", "TIGER", "LEMON" })
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync(existingAttempt);

        _wordRepoMock.Setup(r => r.ExistsAsync("WATER")).ReturnsAsync(true);

        _gameServiceMock.Setup(g => g.EvaluateGuess("WATER", "APPLE"))
            .Returns(new GuessResultDto { IsWon = false, Evaluations = Array.Empty<LetterEvaluationDto>() });

        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId))
            .ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var result = await _sut.SubmitGuessAsync(userId, "WATER");

        // Assert
        Assert.False(result.IsWon);
        Assert.True(result.IsGameOver);
        Assert.Equal(6, result.AttemptsUsed);
        Assert.Equal(0, user.CurrentStreak);
        Assert.Equal(6, user.MaxStreak);
        Assert.Equal(11, user.TotalGamesPlayed);
        Assert.Equal(8, user.TotalGamesWon);
        Assert.Equal("APPLE", result.CorrectWord);
    }

    [Fact]
    public async Task SubmitGuessAsync_ThrowsWhenWordLengthInvalid()
    {
        var userId = 1;
        var user = new User { Id = userId };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitGuessAsync(userId, "FOUR"));
    }

    [Fact]
    public async Task SubmitGuessAsync_ThrowsWhenWordNotInDictionary()
    {
        var userId = 1;
        var user = new User { Id = userId };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _wordRepoMock.Setup(r => r.ExistsAsync("ABCDE")).ReturnsAsync(false);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitGuessAsync(userId, "ABCDE"));
    }

    [Fact]
    public async Task GetTodayChallengeStatusAsync_LapsedStreak_ResetsStreakToZero()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            CurrentStreak = 5,
            MaxStreak = 5
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            WordId = 1,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        // Yesterday challenge exists, but user didn't play/solve
        var yesterdayChallenge = new DailyChallenge
        {
            Id = 99,
            Date = yesterday,
            WordId = 2,
            Word = new Word { Id = 2, Text = "BERRY", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(yesterday)).ReturnsAsync(yesterdayChallenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 99)).ReturnsAsync((UserDailyAttempt?)null);

        // No attempt today yet
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync((UserDailyAttempt?)null);
        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var status = await _sut.GetTodayChallengeStatusAsync(userId);

        // Assert
        Assert.False(status.HasPlayed);
        Assert.False(status.IsSolved);
        Assert.False(status.IsGameOver);
        Assert.Null(status.CorrectWord); // word must remain hidden!
        Assert.Equal(0, user.CurrentStreak); // streak was reset because yesterday wasn't won
        _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTodayChallengeStatusAsync_RestoresPreviousGuessesAndEvaluations()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var user = new User
        {
            Id = userId,
            Username = "testuser",
            CurrentStreak = 2,
            MaxStreak = 2
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            WordId = 1,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        var todayAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 100,
            AttemptsCount = 2,
            IsSolved = false,
            GuessesState = JsonSerializer.Serialize(new List<string> { "CRANE", "PLANT" })
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync(todayAttempt);

        _gameServiceMock.Setup(g => g.EvaluateGuess("CRANE", "APPLE"))
            .Returns(new GuessResultDto { IsWon = false, Evaluations = new LetterEvaluationDto[] { new('C', LetterState.Absent) } });
        _gameServiceMock.Setup(g => g.EvaluateGuess("PLANT", "APPLE"))
            .Returns(new GuessResultDto { IsWon = false, Evaluations = new LetterEvaluationDto[] { new('P', LetterState.Correct) } });

        // Yesterday was won
        var yesterdayChallenge = new DailyChallenge { Id = 99, Date = yesterday };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(yesterday)).ReturnsAsync(yesterdayChallenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 99))
            .ReturnsAsync(new UserDailyAttempt { IsSolved = true });

        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var status = await _sut.GetTodayChallengeStatusAsync(userId);

        // Assert
        Assert.True(status.HasPlayed);
        Assert.False(status.IsSolved);
        Assert.False(status.IsGameOver);
        Assert.Null(status.CorrectWord); // Still hidden!
        Assert.Equal(2, status.AttemptsUsed);
        Assert.Equal(2, status.PreviousGuesses.Count);
        Assert.Equal("CRANE", status.PreviousGuesses[0].Guess);
        Assert.Equal("PLANT", status.PreviousGuesses[1].Guess);
        Assert.Equal(2, status.Stats.CurrentStreak); // streak preserved
    }

    [Fact]
    public async Task SubmitGuessAsync_ThrowsWhenAlreadySolved()
    {
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User { Id = userId };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);
        _wordRepoMock.Setup(r => r.ExistsAsync("APPLE")).ReturnsAsync(true);

        var solvedAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 100,
            IsSolved = true,
            AttemptsCount = 3
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync(solvedAttempt);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitGuessAsync(userId, "APPLE"));
    }

    [Fact]
    public async Task SubmitGuessAsync_ThrowsWhenAttemptsExceeded()
    {
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User { Id = userId };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);
        _wordRepoMock.Setup(r => r.ExistsAsync("CRANE")).ReturnsAsync(true);

        var maxAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 100,
            IsSolved = false,
            AttemptsCount = 6
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync(maxAttempt);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitGuessAsync(userId, "CRANE"));
    }

    [Fact]
    public async Task GetTodayChallengeStatusAsync_GameOverRevealsCorrectWord()
    {
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User { Id = userId, CurrentStreak = 1 };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge
        {
            Id = 100,
            Date = today,
            Word = new Word { Id = 1, Text = "APPLE", Length = 5 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);

        var solvedAttempt = new UserDailyAttempt
        {
            UserId = userId,
            DailyChallengeId = 100,
            IsSolved = true,
            AttemptsCount = 4,
            GuessesState = "[\"CRANE\", \"PLANT\", \"MAPLE\", \"APPLE\"]"
        };
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100)).ReturnsAsync(solvedAttempt);
        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(new List<UserDailyAttempt> { solvedAttempt });
        _gameServiceMock.Setup(g => g.EvaluateGuess(It.IsAny<string>(), "APPLE"))
            .Returns(new GuessResultDto { IsWon = false, Evaluations = Array.Empty<LetterEvaluationDto>() });

        var status = await _sut.GetTodayChallengeStatusAsync(userId);

        Assert.True(status.IsSolved);
        Assert.True(status.IsGameOver);
        Assert.Equal("APPLE", status.CorrectWord);
    }

    [Fact]
    public async Task GetUserStatsAsync_CalculatesWinPercentageAndDistributionCorrectly()
    {
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User
        {
            Id = userId,
            CurrentStreak = 3,
            MaxStreak = 5,
            TotalGamesPlayed = 10,
            TotalGamesWon = 8
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var todayChallenge = new DailyChallenge { Id = 100, Date = today };
        _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync(todayChallenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 100))
            .ReturnsAsync(new UserDailyAttempt { IsSolved = true });

        var attempts = new List<UserDailyAttempt>
        {
            new() { IsSolved = true, AttemptsCount = 2 },
            new() { IsSolved = true, AttemptsCount = 3 },
            new() { IsSolved = true, AttemptsCount = 3 },
            new() { IsSolved = true, AttemptsCount = 4 },
            new() { IsSolved = false, AttemptsCount = 6 }
        };
        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(attempts);

        var stats = await _sut.GetUserStatsAsync(userId);

        Assert.Equal(3, stats.CurrentStreak);
        Assert.Equal(5, stats.MaxStreak);
        Assert.Equal(10, stats.TotalGamesPlayed);
        Assert.Equal(8, stats.TotalGamesWon);
        Assert.Equal(80.0, stats.WinPercentage);
        Assert.Equal(1, stats.GuessDistribution[2]);
        Assert.Equal(2, stats.GuessDistribution[3]);
        Assert.Equal(1, stats.GuessDistribution[4]);
        Assert.Equal(0, stats.GuessDistribution[1]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public async Task GetTodayChallengeStatusAsync_InvalidLength_ThrowsArgumentOutOfRangeException(int invalidLength)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetTodayChallengeStatusAsync(1, invalidLength));
    }

    [Theory]
    [InlineData(3, "CAT")]
    [InlineData(6, "PLANET")]
    [InlineData(8, "KEYBOARD")]
    public async Task GetTodayChallengeStatusAsync_ValidCustomLengths_ReturnsCorrectLength(int length, string sampleWord)
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User { Id = userId, Username = "player" };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var challenge = new DailyChallenge
        {
            Id = 200 + length,
            Date = today,
            Length = length,
            WordId = 50 + length,
            Word = new Word { Id = 50 + length, Text = sampleWord, Length = length }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAndLengthAsync(today, length)).ReturnsAsync(challenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, challenge.Id)).ReturnsAsync((UserDailyAttempt?)null);
        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var result = await _sut.GetTodayChallengeStatusAsync(userId, length);

        // Assert
        Assert.Equal(length, result.WordLength);
        Assert.Equal(today, result.Date);
        Assert.False(result.IsGameOver);
    }

    [Fact]
    public async Task SubmitGuessAsync_DifferentLengthWords_EvaluatesAndMatchesChallenge()
    {
        // Arrange
        var userId = 1;
        var today = DateTime.UtcNow.Date;
        var user = new User { Id = userId, Username = "player" };
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var challenge = new DailyChallenge
        {
            Id = 300,
            Date = today,
            Length = 6,
            WordId = 77,
            Word = new Word { Id = 77, Text = "CODING", Length = 6 }
        };
        _challengeRepoMock.Setup(r => r.GetByDateAndLengthAsync(today, 6)).ReturnsAsync(challenge);
        _attemptRepoMock.Setup(r => r.GetAttemptAsync(userId, 300)).ReturnsAsync((UserDailyAttempt?)null);
        _wordRepoMock.Setup(r => r.ExistsAsync("CODING")).ReturnsAsync(true);
        _gameServiceMock.Setup(g => g.EvaluateGuess("CODING", "CODING")).Returns(new GuessResultDto
        {
            IsWon = true,
            Evaluations = new LetterEvaluationDto[]
            {
                new('C', LetterState.Correct),
                new('O', LetterState.Correct),
                new('D', LetterState.Correct),
                new('I', LetterState.Correct),
                new('N', LetterState.Correct),
                new('G', LetterState.Correct),
            }
        });
        _attemptRepoMock.Setup(r => r.GetAllAttemptsByUserIdAsync(userId)).ReturnsAsync(new List<UserDailyAttempt>());

        // Act
        var result = await _sut.SubmitGuessAsync(userId, "CODING");

        // Assert
        Assert.True(result.IsWon);
        Assert.True(result.IsGameOver);
        Assert.Equal(6, result.WordLength);
        Assert.Equal(6, result.Evaluations.Length);
        _attemptRepoMock.Verify(r => r.AddAsync(It.Is<UserDailyAttempt>(a => a.DailyChallengeId == 300 && a.IsSolved)), Times.Once);
    }

    [Fact]
    public async Task EnsureDailyChallengesAsync_CreatesChallengesForLengths3To8()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        for (int l = 3; l <= 8; l++)
        {
            int len = l;
            _challengeRepoMock.Setup(r => r.GetByDateAndLengthAsync(today, len)).ReturnsAsync((DailyChallenge?)null);
            _challengeRepoMock.Setup(r => r.GetByDateAsync(today)).ReturnsAsync((DailyChallenge?)null);
            _wordRepoMock.Setup(r => r.GetRandomWordAsync(len)).ReturnsAsync(new Word { Id = len, Text = new string('A', len), Length = len });
        }

        // Act
        await _sut.EnsureDailyChallengesAsync(today);

        // Assert
        _challengeRepoMock.Verify(r => r.AddAsync(It.IsAny<DailyChallenge>()), Times.Exactly(6));
        _challengeRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(6));
    }
}
