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
            logger.LogInformation("Database already have words. Skipping Seeding.");
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
}
