namespace Wordle.Server.Core.DTOs.Auth;

public class EmailVerificationInitiatedDto
{
    public string PendingEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? VerificationCode { get; set; }
}
