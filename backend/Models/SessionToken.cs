namespace GptRealtime.Api.Models;

public class SessionToken
{
    public string ClientSecret { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RealtimeUrl { get; set; } = string.Empty;
}
