namespace Wordle.Server.Core.Services;

using DTOs.Game;
using Interfaces.Services;
using System.Text.Json;
using Wordle.Server.Core.Entities;
using Wordle.Server.Core.Interfaces.Repositories;

public class DailyChallengeService : IDailyChallengeService
{
    private readonly IWordRepository _wordRepository;
    private readonly IDailyChallengeRepository _challengeRepository;
    private readonly IUserDailyAttemptRepository _attemptRepository;
    private readonly IGameService _gameService;

    public DailyChallengeService(
        IWordRepository wordRepository,
        IDailyChallengeRepository challengeRepository,
        IUserDailyAttemptRepository attemptRepository,
        IGameService gameService)
    {
        _wordRepository = wordRepository;
        _challengeRepository = challengeRepository;
        _attemptRepository = attemptRepository;
        _gameService = gameService;
    }

    public async Task<DailyChallengeResultDto> SubmitGuessAsync(int userId, string guessText)
    {
        var cleanGuess = guessText.Trim().ToUpper();
        if (cleanGuess.Length != 5) throw new ArgumentException("Думата трябва да е точно 5 букви.");

        var isValidWord = await _wordRepository.ExistsAsync(cleanGuess);
        if (!isValidWord) throw new Exception("Думата не съществува в речника.");

        var today = DateTime.UtcNow.Date;
        var challenge = await _challengeRepository.GetByDateAsync(today);
        if (challenge == null)
        {
            var randomWord = await _wordRepository.GetRandomWordAsync(5);
            challenge = new DailyChallenge { Date = today, WordId = randomWord.Id, Word = randomWord };
            await _challengeRepository.AddAsync(challenge);
            await _challengeRepository.SaveChangesAsync();
        }

        var userAttempt = await _attemptRepository.GetAttemptAsync(userId, challenge.Id);

        List<string> previousGuesses = new List<string>();

        if (userAttempt != null)
        {
            if (userAttempt.IsSolved) throw new Exception("Вече сте познали днешната дума.");
            if (userAttempt.AttemptsCount >= 6) throw new Exception("Нямате повече право на опити за днес.");

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

        await _attemptRepository.SaveChangesAsync();

        var isGameOver = userAttempt.IsSolved || userAttempt.AttemptsCount >= 6;

        return new DailyChallengeResultDto
        {
            IsWon = evaluation.IsWon,
            IsGameOver = isGameOver,
            Evaluations = evaluation.Evaluations,
            AttemptsUsed = userAttempt.AttemptsCount,
            CorrectWord = isGameOver ? challenge.Word.Text : null
        };
    }
}
