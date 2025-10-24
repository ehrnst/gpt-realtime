using GptRealtime.Api.Models;

namespace GptRealtime.Api.Services;

public interface ITokenService
{
    SessionToken GenerateToken();
    bool ValidateToken(string token);
}
