using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI;
using OpenAI.Realtime;
using GptRealtime.Api.Models;
using Microsoft.Extensions.Options;

#pragma warning disable OPENAI002 // OpenAI realtime features are in preview

namespace GptRealtime.Api.Services;

public class TokenService : ITokenService
{
    private const string AzurePreviewApiVersion = "2025-04-01-preview";

    private readonly HttpClient _httpClient;
    private readonly OpenAIClient _openAIClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        HttpClient httpClient,
        OpenAIClient openAIClient,
        IOptions<OpenAISettings> settings,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _openAIClient = openAIClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SessionToken> CreateSessionTokenAsync(CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var sessionEndpoint = BuildSessionEndpoint();

        try
        {
            var sessionConfig = CreateSessionConfiguration();
            var response = await SendSessionRequest(sessionEndpoint, sessionConfig, cancellationToken);
            
            return ParseSessionResponse(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while creating session token");
            throw new InvalidOperationException("Failed to create session token due to network error", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse session response");
            throw new InvalidOperationException("Failed to parse session response", ex);
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("Azure OpenAI base URL is required.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI API key is required.");
        }
    }

    private Uri BuildSessionEndpoint()
    {
        var baseUrl = _settings.BaseUrl!.TrimEnd('/');

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid Azure OpenAI base URL: '{baseUrl}'.");
        }

        // Build the session endpoint URL for Azure OpenAI realtime API
        return new Uri(
            $"{baseUri.Scheme}://{baseUri.Authority}{CombinePath(NormalizePath(baseUri.AbsolutePath, "/openai"), "realtimeapi/sessions")}?api-version={AzurePreviewApiVersion}");
    }

    private async Task<JsonElement> SendSessionRequest(Uri endpoint, object sessionConfig, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        
        // Set headers using OpenAI SDK patterns
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Headers.Add("OpenAI-Beta", "realtime=v1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false 
        };

        var content = JsonSerializer.Serialize(sessionConfig, jsonOptions);
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to create realtime session. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                errorBody);
            throw new InvalidOperationException($"Failed to create session token. Status: {response.StatusCode}, Response: {errorBody}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var jsonDocument = await JsonSerializer.DeserializeAsync<JsonDocument>(responseStream, cancellationToken: cancellationToken);
        
        return jsonDocument?.RootElement ?? throw new InvalidOperationException("Empty response received");
    }

    private object CreateSessionConfiguration()
    {
        return new SessionConfigurationRequest
        {
            Model = _settings.Model,
            Voice = _settings.Voice,
            Modalities = new[] { "text", "audio" },
            Instructions = _settings.SystemInstructions,
            TurnDetection = new TurnDetectionConfig
            {
                Type = "server_vad",
                Threshold = 0.5,
                PrefixPaddingMs = 300,
                SilenceDurationMs = 200
            },
            InputAudioFormat = "pcm16",
            OutputAudioFormat = "pcm16",
            Temperature = 0.7,
            MaxResponseOutputTokens = 4096
        };
    }

    // Internal classes for better type safety and extensibility
    private class SessionConfigurationRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        public string Voice { get; set; } = string.Empty;

        [JsonPropertyName("modalities")]
        public string[] Modalities { get; set; } = Array.Empty<string>();

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [JsonPropertyName("turn_detection")]
        public TurnDetectionConfig TurnDetection { get; set; } = new();

        [JsonPropertyName("input_audio_format")]
        public string InputAudioFormat { get; set; } = string.Empty;

        [JsonPropertyName("output_audio_format")]
        public string OutputAudioFormat { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_response_output_tokens")]
        public int MaxResponseOutputTokens { get; set; } = 4096;
    }

    private class TurnDetectionConfig
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("threshold")]
        public double Threshold { get; set; }

        [JsonPropertyName("prefix_padding_ms")]
        public int PrefixPaddingMs { get; set; }

        [JsonPropertyName("silence_duration_ms")]
        public int SilenceDurationMs { get; set; }
    }

    private SessionToken ParseSessionResponse(JsonElement json)
    {
        // Azure returns client_secret.value, not just client_secret
        string clientSecret;
        if (json.TryGetProperty("client_secret", out var clientSecretElement))
        {
            if (clientSecretElement.ValueKind == JsonValueKind.String)
            {
                // Direct string value
                clientSecret = clientSecretElement.GetString() ?? string.Empty;
            }
            else if (clientSecretElement.ValueKind == JsonValueKind.Object &&
                     clientSecretElement.TryGetProperty("value", out var valueElement) &&
                     valueElement.ValueKind == JsonValueKind.String)
            {
                // Nested object with value property (Azure format)
                clientSecret = valueElement.GetString() ?? string.Empty;
            }
            else
            {
                throw new InvalidOperationException("Realtime session response client_secret was not in expected format.");
            }
        }
        else
        {
            throw new InvalidOperationException("Realtime session response did not contain a client_secret.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(1);
        if (json.TryGetProperty("expires_at", out var expiresAtElement))
        {
            expiresAt = ParseExpiresAt(expiresAtElement);
        }

        var realtimeUrl = TryGetRealtimeUrlFromResponse(json)
            ?? GetWebRtcUrlForRegion(_settings.Region); // Use configured region

        return new SessionToken
        {
            ClientSecret = clientSecret,
            ExpiresAt = expiresAt,
            RealtimeUrl = realtimeUrl
        };
    }

    private static string? TryGetRealtimeUrlFromResponse(JsonElement json)
    {
        if (json.TryGetProperty("webrtc_url", out var webrtcUrlElement) &&
            webrtcUrlElement.ValueKind == JsonValueKind.String)
        {
            return webrtcUrlElement.GetString();
        }

        if (json.TryGetProperty("realtime_url", out var realtimeUrlElement) &&
            realtimeUrlElement.ValueKind == JsonValueKind.String)
        {
            return realtimeUrlElement.GetString();
        }

        if (json.TryGetProperty("url", out var urlElement) && urlElement.ValueKind == JsonValueKind.String)
        {
            return urlElement.GetString();
        }

        return null;
    }

    private static string GetWebRtcUrlForRegion(string region)
    {
        // Azure WebRTC URLs are region-specific
        return $"https://{region}.realtimeapi-preview.ai.azure.com/v1/realtimertc";
    }

    private static string NormalizePath(string path, string fallback)
    {
        var trimmed = string.IsNullOrWhiteSpace(path) ? string.Empty : path.TrimEnd('/');

        if (string.IsNullOrEmpty(trimmed) || trimmed == "/")
        {
            return fallback;
        }

        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }

    private static string CombinePath(string basePath, string relativePath)
    {
        var prefix = NormalizePath(basePath, "/");
        var normalizedRelative = relativePath.TrimStart('/');

        return prefix == "/"
            ? "/" + normalizedRelative
            : $"{prefix}/{normalizedRelative}";
    }

    private static DateTime ParseExpiresAt(JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt64(out var unixSeconds)
                    => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime,
                JsonValueKind.String when DateTime.TryParse(
                    element.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var parsed)
                    => parsed.ToUniversalTime(),
                _ => DateTime.UtcNow.AddMinutes(1)
            };
        }
        catch
        {
            return DateTime.UtcNow.AddMinutes(1);
        }
    }

    /// <summary>
    /// Gets the OpenAI RealtimeClient configured for this service.
    /// This client can be used for direct WebSocket connections to the realtime API.
    /// </summary>
    /// <returns>A configured RealtimeClient instance</returns>
    public RealtimeClient GetRealtimeClient()
    {
        return _openAIClient.GetRealtimeClient();
    }

    /// <summary>
    /// Creates a RealtimeClient with custom options for advanced scenarios.
    /// </summary>
    /// <param name="sessionToken">The session token to use for authentication</param>
    /// <returns>A configured RealtimeClient instance</returns>
    public RealtimeClient GetRealtimeClient(SessionToken sessionToken)
    {
        // For future use when the SDK supports session token authentication directly
        // Currently, the session token is used by the frontend for WebSocket connections
        return _openAIClient.GetRealtimeClient();
    }
}
