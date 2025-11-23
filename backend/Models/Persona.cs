namespace GptRealtime.Api.Models;

public class Persona
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Voice { get; set; } = "alloy";
    public string SystemInstructions { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎙️";
}
