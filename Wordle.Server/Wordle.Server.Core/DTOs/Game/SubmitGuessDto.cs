namespace Wordle.Server.Core.DTOs.Game;

public class SubmitGuessDto
{
    public string Guess { get; set; } = string.Empty;
    public int? Length { get; set; }
}
