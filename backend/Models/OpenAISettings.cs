namespace GptRealtime.Api.Models;

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "gpt-4o-realtime-mini";
    public string Voice { get; set; } = "alloy";
    public string SystemInstructions { get; set; } = "You are a helpful AI assistant for backend development and technical discussions.";
    public string Region { get; set; } = "swedencentral"; // Default region for WebRTC
}
