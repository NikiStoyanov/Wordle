namespace Wordle.Server.Core.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
    public string Code { get; set; } = string.Empty;
}
