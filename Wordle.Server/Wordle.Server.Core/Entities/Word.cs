namespace Wordle.Server.Core.Entities;

public class Word 
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Length { get; set; }

    public bool IsActive { get; set; } = true;
}
