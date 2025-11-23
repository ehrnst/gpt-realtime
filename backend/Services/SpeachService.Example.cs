// REFERENCE IMPLEMENTATION - This file shows how SpeachService.cs should look after personas integration
// Compare this with your actual SpeachService.cs and apply the necessary changes

using Microsoft.Extensions.Options;
using backend.Models;
using System.Text;
using System.Text.Json;

namespace backend.Services;

public class SpeachService
{
    private readonly OpenAISettings _settings;
    private readonly PersonaSettings _personaSettings;  // ADDED: PersonaSettings field
    private readonly ILogger<SpeachService> _logger;
    private readonly HttpClient _httpClient;

    // UPDATED: Constructor now accepts PersonaSettings
    public SpeachService(
        IOptions<OpenAISettings> settings,
        IOptions<PersonaSettings> personaSettings,  // ADDED: PersonaSettings parameter
        ILogger<SpeachService> logger,
        HttpClient httpClient)
    {
        _settings = settings.Value;
        _personaSettings = personaSettings.Value;  // ADDED: Initialize PersonaSettings
        _logger = logger;
        _httpClient = httpClient;
    }

    // UPDATED: Method now accepts optional personaId parameter
    public async Task<SessionToken> CreateSessionTokenAsync(string? personaId = null)
    {
        // Validate settings
        if (string.IsNullOrEmpty(_settings.BaseUrl) || string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI settings are not configured properly");
        }

        // ADDED: Persona resolution logic
        // Determine which voice and instructions to use
        string voice = _settings.Voice;
        string systemInstructions = _settings.SystemInstructions;

        // Override with persona-specific settings if provided
        if (!string.IsNullOrEmpty(personaId))
        {
            var persona = _personaSettings.Personas?.FirstOrDefault(p => p.Id == personaId);
            if (persona != null)
            {
                voice = persona.Voice;
                systemInstructions = persona.SystemInstructions;
                _logger.LogInformation("Using persona: {PersonaName} (ID: {PersonaId})", persona.Name, personaId);
            }
            else
            {
                _logger.LogWarning("Persona {PersonaId} not found, using default settings", personaId);
            }
        }
        // END ADDED SECTION

        // Build session configuration - UPDATED to use determined voice and instructions
        var sessionConfig = new
        {
            model = _settings.Model,
            voice = voice,  // UPDATED: Use determined voice instead of _settings.Voice
            modalities = new[] { "text", "audio" },
            instructions = systemInstructions,  // UPDATED: Use determined instructions
            input_audio_format = "pcm16",
            output_audio_format = "pcm16",
            turn_detection = new
            {
                type = "server_vad",
                threshold = 0.5,
                prefix_padding_ms = 300,
                silence_duration_ms = 200
            }
        };

        // Create session endpoint
        var sessionUrl = $"{_settings.BaseUrl}/realtimeapi/sessions?api-version=2025-04-01-preview";
        
        var request = new HttpRequestMessage(HttpMethod.Post, sessionUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Headers.Add("OpenAI-Beta", "realtime=v1");
        request.Content = new StringContent(
            JsonSerializer.Serialize(sessionConfig),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        // Parse client secret (Azure format)
        string clientSecret;
        if (root.TryGetProperty("client_secret", out var secretElement))
        {
            if (secretElement.ValueKind == JsonValueKind.Object &&
                secretElement.TryGetProperty("value", out var valueElement))
            {
                clientSecret = valueElement.GetString() ?? throw new InvalidOperationException("Client secret value is null");
            }
            else
            {
                clientSecret = secretElement.GetString() ?? throw new InvalidOperationException("Client secret is null");
            }
        }
        else
        {
            throw new InvalidOperationException("Client secret not found in response");
        }

        // Calculate expiration
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Build WebRTC URL
        var region = _settings.Region ?? "swedencentral";
        var realtimeUrl = $"wss://{region}.realtimeapi-preview.ai.azure.com";

        return new SessionToken
        {
            ClientSecret = clientSecret,
            ExpiresAt = expiresAt,
            RealtimeUrl = realtimeUrl
        };
    }
}
