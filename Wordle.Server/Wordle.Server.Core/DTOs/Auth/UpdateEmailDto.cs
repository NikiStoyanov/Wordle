namespace Wordle.Server.Core.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class UpdateEmailDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;
}
