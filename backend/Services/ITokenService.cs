using GptRealtime.Api.Models;

namespace GptRealtime.Api.Services;

public interface ITokenService
{
    Task<SessionToken> CreateSessionTokenAsync(CancellationToken cancellationToken = default);
}
