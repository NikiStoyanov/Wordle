namespace Wordle.Server.Core.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class UpdateUsernameDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens, and underscores.")]
    public string Username { get; set; } = string.Empty;
}
