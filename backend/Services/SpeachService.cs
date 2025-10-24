using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GptRealtime.Api.Models;
using Microsoft.Extensions.Options;

namespace GptRealtime.Api.Services;

public class TokenService : ITokenService
{
    private const string AzurePreviewApiVersion = "2025-04-01-preview";

    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        HttpClient httpClient,
        IOptions<OpenAISettings> settings,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SessionToken> CreateSessionTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("Azure OpenAI base URL is required.");
        }

        var baseUrl = _settings.BaseUrl.TrimEnd('/');

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid Azure OpenAI base URL: '{baseUrl}'.");
        }

        var sessionEndpoint = new Uri(
            $"{baseUri.Scheme}://{baseUri.Authority}{CombinePath(NormalizePath(baseUri.AbsolutePath, "/openai"), "realtimeapi/sessions")}?api-version={AzurePreviewApiVersion}");

        using var request = new HttpRequestMessage(HttpMethod.Post, sessionEndpoint);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Headers.Add("OpenAI-Beta", "realtime=v1");

        var requestPayload = new
        {
            model = _settings.Model,
            voice = _settings.Voice,
            modalities = new[] { "text", "audio" },
            instructions = _settings.SystemInstructions,
            turn_detection = new
            {
                type = "server_vad",
                threshold = 0.5,
                prefix_padding_ms = 300,
                silence_duration_ms = 200
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to create realtime session. Status: {StatusCode}. Response: {Response}",
                response.StatusCode,
                errorBody);
            response.EnsureSuccessStatusCode();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream, cancellationToken: cancellationToken);

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
            ?? GetWebRtcUrlForRegion("swedencentral"); // Since you mentioned Sweden Central

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
}
