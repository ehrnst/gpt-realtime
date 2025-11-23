using GptRealtime.Api.Models;
using OpenAI.Realtime;

#pragma warning disable OPENAI002 // OpenAI realtime features are in preview

namespace GptRealtime.Api.Services;

public interface ITokenService
{
    Task<SessionToken> CreateSessionTokenAsync(string? personaId = null, CancellationToken cancellationToken = default);
    RealtimeClient GetRealtimeClient();
    RealtimeClient GetRealtimeClient(SessionToken sessionToken);
}
