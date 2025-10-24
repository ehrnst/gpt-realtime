using System.Security.Cryptography;
using GptRealtime.Api.Models;

namespace GptRealtime.Api.Services;

public class TokenService : ITokenService
{
    private readonly Dictionary<string, DateTime> _activeTokens = new();
    private readonly object _lock = new();
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(5);

    public SessionToken GenerateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(_tokenLifetime);

        lock (_lock)
        {
            CleanupExpiredTokens();
            _activeTokens[token] = expiresAt;
        }

        return new SessionToken
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public bool ValidateToken(string token)
    {
        lock (_lock)
        {
            CleanupExpiredTokens();
            return _activeTokens.ContainsKey(token) && _activeTokens[token] > DateTime.UtcNow;
        }
    }

    private void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _activeTokens.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
        foreach (var token in expiredTokens)
        {
            _activeTokens.Remove(token);
        }
    }
}
