namespace Wordle.Server.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Core.DTOs.Game;
using Core.Interfaces.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IDailyChallengeService _dailyChallengeService;

    public GameController(IDailyChallengeService dailyChallengeService)
    {
        _dailyChallengeService = dailyChallengeService;
    }

    [HttpPost("guess")]
    public async Task<IActionResult> SubmitGuess([FromBody] SubmitGuessDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { Message = "Невалиден или липсващ токен." });
            }

            var result = await _dailyChallengeService.SubmitGuessAsync(userId, dto.Guess);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}