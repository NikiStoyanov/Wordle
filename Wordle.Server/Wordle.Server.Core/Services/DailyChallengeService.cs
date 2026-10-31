namespace Wordle.Server.Core.Services;

using System.Text.Json;
using Wordle.Server.Core.DTOs.Game;
using Wordle.Server.Core.Entities;
using Wordle.Server.Core.Interfaces.Repositories;
using Wordle.Server.Core.Interfaces.Services;

public class DailyChallengeService : IDailyChallengeService
{
    private readonly IWordRepository _wordRepository;
    private readonly IDailyChallengeRepository _challengeRepository;
    private readonly IUserDailyAttemptRepository _attemptRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameService _gameService;

    public DailyChallengeService(
        IWordRepository wordRepository,
        IDailyChallengeRepository challengeRepository,
        IUserDailyAttemptRepository attemptRepository,
        IUserRepository userRepository,
        IGameService gameService)
    {
        _wordRepository = wordRepository;
        _challengeRepository = challengeRepository;
        _attemptRepository = attemptRepository;
        _userRepository = userRepository;
        _gameService = gameService;
    }

    public async Task<DailyChallengeStatusDto> GetTodayChallengeStatusAsync(int userId, int length = 5)
    {
        if (length < 3 || length > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Word length must be between 3 and 8 letters.");
        }

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var today = DateTime.UtcNow.Date;
        var challenge = await GetOrCreateTodayChallengeAsync(today, length);
        var userAttempt = await _attemptRepository.GetAttemptAsync(userId, challenge.Id);

        var isSolved = userAttempt?.IsSolved == true;
        var isGameOver = isSolved || (userAttempt != null && userAttempt.AttemptsCount >= 6);

        if (!isGameOver)
        {
            var solvedYesterday = await CheckIfUserSolvedYesterdayAsync(userId, today, length);
            if (!solvedYesterday && user.CurrentStreak > 0)
            {
                user.CurrentStreak = 0;
                await _userRepository.SaveChangesAsync();
            }
        }

        var previousGuesses = new List<PreviousGuessDto>();
        if (!string.IsNullOrEmpty(userAttempt?.GuessesState))
        {
            var words = JsonSerializer.Deserialize<List<string>>(userAttempt.GuessesState) ?? new();
            foreach (var word in words)
            {
                var eval = _gameService.EvaluateGuess(word, challenge.Word.Text);
                previousGuesses.Add(new PreviousGuessDto
                {
                    Guess = word,
                    Evaluations = eval.Evaluations
                });
            }
        }

        var stats = await BuildUserStatsAsync(user);

        return new DailyChallengeStatusDto
        {
            Date = today,
            WordLength = length,
            HasPlayed = userAttempt != null && userAttempt.AttemptsCount > 0,
            IsSolved = isSolved,
            IsGameOver = isGameOver,
            AttemptsUsed = userAttempt?.AttemptsCount ?? 0,
            PreviousGuesses = previousGuesses,
            CorrectWord = isGameOver ? challenge.Word.Text : null,
            Stats = stats,
            NextChallengeUtc = today.AddDays(1)
        };
    }

    public async Task<DailyChallengeResultDto> SubmitGuessAsync(int userId, string guessText, int? length = null)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var cleanGuess = guessText?.Trim().ToUpper() ?? string.Empty;
        var targetLength = length ?? cleanGuess.Length;

        if (targetLength < 3 || targetLength > 8)
        {
            throw new ArgumentException("The word length must be between 3 and 8 letters.");
        }

        if (cleanGuess.Length != targetLength)
        {
            throw new ArgumentException($"The word must be exactly {targetLength} letters long.");
        }

        var isValidWord = await _wordRepository.ExistsAsync(cleanGuess);
        if (!isValidWord)
        {
            throw new ArgumentException("The word does not exist in the dictionary.");
        }

        var today = DateTime.UtcNow.Date;
        var challenge = await GetOrCreateTodayChallengeAsync(today, targetLength);
        var userAttempt = await _attemptRepository.GetAttemptAsync(userId, challenge.Id);

        var previousGuesses = new List<string>();

        if (userAttempt != null)
        {
            if (userAttempt.IsSolved)
            {
                throw new InvalidOperationException("You have already solved today's challenge.");
            }
            if (userAttempt.AttemptsCount >= 6)
            {
                throw new InvalidOperationException("You have no more attempts left for today.");
            }

            if (!string.IsNullOrEmpty(userAttempt.GuessesState))
            {
                previousGuesses = JsonSerializer.Deserialize<List<string>>(userAttempt.GuessesState) ?? new List<string>();
            }
        }
        else
        {
            userAttempt = new UserDailyAttempt
            {
                UserId = userId,
                DailyChallengeId = challenge.Id,
                AttemptsCount = 0,
                IsSolved = false,
                GuessesState = "[]"
            };
            await _attemptRepository.AddAsync(userAttempt);
        }

        var evaluation = _gameService.EvaluateGuess(cleanGuess, challenge.Word.Text);

        previousGuesses.Add(cleanGuess);
        userAttempt.GuessesState = JsonSerializer.Serialize(previousGuesses);
        userAttempt.AttemptsCount++;
        userAttempt.IsSolved = evaluation.IsWon;

        var isGameOver = userAttempt.IsSolved || userAttempt.AttemptsCount >= 6;

        if (isGameOver)
        {
            user.TotalGamesPlayed++;

            if (evaluation.IsWon)
            {
                user.TotalGamesWon++;
                var solvedYesterday = await CheckIfUserSolvedYesterdayAsync(userId, today, targetLength);
                if (solvedYesterday)
                {
                    user.CurrentStreak++;
                }
                else
                {
                    user.CurrentStreak = 1;
                }

                if (user.CurrentStreak > user.MaxStreak)
                {
                    user.MaxStreak = user.CurrentStreak;
                }
            }
            else
            {
                user.CurrentStreak = 0;
            }

            await _userRepository.SaveChangesAsync();
        }

        await _attemptRepository.SaveChangesAsync();

        var stats = await BuildUserStatsAsync(user);

        return new DailyChallengeResultDto
        {
            IsWon = evaluation.IsWon,
            IsGameOver = isGameOver,
            WordLength = targetLength,
            Evaluations = evaluation.Evaluations,
            AttemptsUsed = userAttempt.AttemptsCount,
            CorrectWord = isGameOver ? challenge.Word.Text : null,
            Stats = stats
        };
    }

    public async Task<UserStatsDto> GetUserStatsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var today = DateTime.UtcNow.Date;
        var challenge = await _challengeRepository.GetByDateAsync(today);

        var isTodaySolved = false;
        var isGameOverToday = false;

        if (challenge != null)
        {
            var todayAttempt = await _attemptRepository.GetAttemptAsync(userId, challenge.Id);
            isTodaySolved = todayAttempt?.IsSolved == true;
            isGameOverToday = isTodaySolved || (todayAttempt != null && todayAttempt.AttemptsCount >= 6);
        }

        if (!isGameOverToday && !isTodaySolved)
        {
            var solvedYesterday = await CheckIfUserSolvedYesterdayAsync(userId, today, 5);
            if (!solvedYesterday && user.CurrentStreak > 0)
            {
                user.CurrentStreak = 0;
                await _userRepository.SaveChangesAsync();
            }
        }

        return await BuildUserStatsAsync(user);
    }

    public async Task EnsureDailyChallengesAsync(DateTime date)
    {
        for (int length = 3; length <= 8; length++)
        {
            await GetOrCreateTodayChallengeAsync(date.Date, length);
        }
    }

    private async Task<DailyChallenge> GetOrCreateTodayChallengeAsync(DateTime today, int length)
    {
        var challenge = await GetChallengeByDateAndLengthAsync(today, length);
        if (challenge != null)
        {
            return challenge;
        }

        var randomWord = await _wordRepository.GetRandomWordAsync(length);
        if (randomWord == null)
        {
            throw new InvalidOperationException($"No {length}-letter words available in the database.");
        }

        challenge = new DailyChallenge
        {
            Date = today,
            Length = length,
            WordId = randomWord.Id,
            Word = randomWord
        };

        await _challengeRepository.AddAsync(challenge);
        await _challengeRepository.SaveChangesAsync();

        return challenge;
    }

    private async Task<DailyChallenge?> GetChallengeByDateAndLengthAsync(DateTime date, int length)
    {
        if (length == 5)
        {
            var legacyChallenge = await _challengeRepository.GetByDateAsync(date);
            if (legacyChallenge != null)
            {
                return legacyChallenge;
            }
        }

        return await _challengeRepository.GetByDateAndLengthAsync(date, length);
    }

    private async Task<bool> CheckIfUserSolvedYesterdayAsync(int userId, DateTime today, int length)
    {
        var yesterday = today.AddDays(-1);
        var yesterdayChallenge = await GetChallengeByDateAndLengthAsync(yesterday, length);
        if (yesterdayChallenge == null)
        {
            return false;
        }

        var yesterdayAttempt = await _attemptRepository.GetAttemptAsync(userId, yesterdayChallenge.Id);
        return yesterdayAttempt?.IsSolved == true;
    }

    private async Task<UserStatsDto> BuildUserStatsAsync(User user)
    {
        var attempts = await _attemptRepository.GetAllAttemptsByUserIdAsync(user.Id);

        var distribution = new Dictionary<int, int>
        {
            { 1, attempts.Count(a => a.IsSolved && a.AttemptsCount == 1) },
            { 2, attempts.Count(a => a.IsSolved && a.AttemptsCount == 2) },
            { 3, attempts.Count(a => a.IsSolved && a.AttemptsCount == 3) },
            { 4, attempts.Count(a => a.IsSolved && a.AttemptsCount == 4) },
            { 5, attempts.Count(a => a.IsSolved && a.AttemptsCount == 5) },
            { 6, attempts.Count(a => a.IsSolved && a.AttemptsCount == 6) }
        };

        var winPercentage = user.TotalGamesPlayed > 0
            ? Math.Round((double)user.TotalGamesWon / user.TotalGamesPlayed * 100, 1)
            : 0;

        return new UserStatsDto
        {
            CurrentStreak = user.CurrentStreak,
            MaxStreak = user.MaxStreak,
            TotalGamesPlayed = user.TotalGamesPlayed,
            TotalGamesWon = user.TotalGamesWon,
            WinPercentage = winPercentage,
            GuessDistribution = distribution
        };
    }
}
