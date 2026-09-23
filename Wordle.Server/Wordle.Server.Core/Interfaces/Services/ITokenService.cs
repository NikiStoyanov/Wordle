namespace Wordle.Server.Core.Interfaces.Services;

using Entities;

public interface ITokenService
{
    string GenerateJwtToken(User user);
}
