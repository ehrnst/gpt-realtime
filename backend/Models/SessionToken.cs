namespace GptRealtime.Api.Models;

public class SessionToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
