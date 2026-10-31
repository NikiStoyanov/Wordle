namespace Wordle.Server.Core.Entities;

public class DailyChallenge
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int Length { get; set; } = 5;

    public int WordId { get; set; }

    public Word Word { get; set; } = null!;
}
