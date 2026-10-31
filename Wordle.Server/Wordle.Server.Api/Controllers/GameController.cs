namespace Wordle.Server.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wordle.Server.Core.DTOs.Game;
using Wordle.Server.Core.Interfaces.Services;

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

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayChallenge([FromQuery] int length = 5)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized(new { Message = "Invalid or missing token claims." });
        }

        if (length < 3 || length > 8)
        {
            return BadRequest(new { Message = "Word length must be between 3 and 8 letters." });
        }

        try
        {
            var status = await _dailyChallengeService.GetTodayChallengeStatusAsync(userId, length);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("guess")]
    public async Task<IActionResult> SubmitGuess([FromBody] SubmitGuessDto dto)
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized(new { Message = "Invalid or missing token claims." });
        }

        try
        {
            var result = await _dailyChallengeService.SubmitGuessAsync(userId, dto.Guess, dto.Length);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!TryGetUserId(out int userId))
        {
            return Unauthorized(new { Message = "Invalid or missing token claims." });
        }

        try
        {
            var stats = await _dailyChallengeService.GetUserStatsAsync(userId);
            return Ok(stats);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("lengths")]
    public IActionResult GetAvailableLengths()
    {
        return Ok(new[] { 3, 4, 5, 6, 7, 8 });
    }

    private bool TryGetUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
        {
            return true;
        }

        userId = 0;
        return false;
    }
}
