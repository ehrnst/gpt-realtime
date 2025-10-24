namespace GptRealtime.Api.Models;

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "gpt-4o-realtime-preview-2024-10-01";
    public string Voice { get; set; } = "alloy";
}
