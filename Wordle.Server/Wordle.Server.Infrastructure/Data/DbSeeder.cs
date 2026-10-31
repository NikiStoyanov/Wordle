namespace Wordle.Server.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Entities;

public static class DbSeeder
{
    public static async Task SeedWordsAsync(WordleDbContext context, string dictionariesPath, ILogger logger)
    {
        await context.Database.MigrateAsync();

        if (await context.Words.AnyAsync())
        {
            logger.LogInformation("Database already has words. Skipping Seeding.");
            return;
        }

        var fileNames = new[] { "english.txt" };
        var uniqueStrings = new HashSet<string>();

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Combine(dictionariesPath, fileName);

            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);

                foreach (var line in lines)
                {
                    var cleanWord = line.Trim().ToUpper();

                    if (!string.IsNullOrWhiteSpace(cleanWord))
                    {
                        uniqueStrings.Add(cleanWord);
                    }
                }
                logger.LogInformation($"Successfully read file: {fileName} with {lines.Length} line.");
            }
            else
            {
                logger.LogWarning($"File was not found and would be skipped: {filePath}.");
            }
        }

        if (uniqueStrings.Count > 0)
        {
            var wordsToAdd = uniqueStrings.Select(w => new Word
            {
                Text = w,
                Length = w.Length
            });

            await context.Words.AddRangeAsync(wordsToAdd);
            await context.SaveChangesAsync();

            logger.LogInformation($"Successfully added {uniqueStrings.Count} unique words to the database.");
        }
    }

    public static async Task SeedDailyChallengesAsync(WordleDbContext context, DateTime date, ILogger logger)
    {
        for (int length = 3; length <= 8; length++)
        {
            var exists = await context.DailyChallenges.AnyAsync(c => c.Date.Date == date.Date && c.Length == length);
            if (!exists)
            {
                var randomWord = await context.Words
                    .Where(w => w.Length == length && w.IsActive)
                    .OrderBy(w => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (randomWord != null)
                {
                    context.DailyChallenges.Add(new DailyChallenge
                    {
                        Date = date.Date,
                        Length = length,
                        WordId = randomWord.Id
                    });
                    logger.LogInformation($"Created daily challenge for date {date:yyyy-MM-dd} with length {length} ({randomWord.Text}).");
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
