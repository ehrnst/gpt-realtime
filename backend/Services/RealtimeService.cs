using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using GptRealtime.Api.Models;

namespace GptRealtime.Api.Services;

public class RealtimeService : IRealtimeService
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<RealtimeService> _logger;

    public RealtimeService(IOptions<OpenAISettings> settings, ILogger<RealtimeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task HandleRealtimeSession(
        Stream inputStream,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        ClientWebSocket? openAiWebSocket = null;
        try
        {
            var baseUrl = string.IsNullOrEmpty(_settings.BaseUrl) 
                ? "wss://api.openai.com/v1/realtime" 
                : _settings.BaseUrl.Replace("https://", "wss://").Replace("http://", "ws://") + "/realtime";
            
            var wsUrl = $"{baseUrl}?model={_settings.Model}";

            openAiWebSocket = new ClientWebSocket();
            openAiWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {_settings.ApiKey}");
            openAiWebSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            await openAiWebSocket.ConnectAsync(new Uri(wsUrl), cancellationToken);
            _logger.LogInformation("Connected to OpenAI Realtime API");

            // Send initial session configuration
            var sessionConfig = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions = "You are a helpful AI assistant. Speak naturally and conversationally.",
                    voice = _settings.Voice,
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = 0.5,
                        prefix_padding_ms = 300,
                        silence_duration_ms = 500
                    }
                }
            };

            var configJson = JsonSerializer.Serialize(sessionConfig);
            var configBytes = Encoding.UTF8.GetBytes(configJson);
            await openAiWebSocket.SendAsync(
                new ArraySegment<byte>(configBytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            var receiveTask = RelayFromClientToOpenAI(inputStream, openAiWebSocket, cancellationToken);
            var sendTask = RelayFromOpenAIToClient(openAiWebSocket, outputStream, cancellationToken);

            await Task.WhenAny(receiveTask, sendTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling realtime session");
        }
        finally
        {
            if (openAiWebSocket?.State == WebSocketState.Open)
            {
                await openAiWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
            }
            openAiWebSocket?.Dispose();
        }
    }

    private async Task RelayFromClientToOpenAI(
        Stream inputStream,
        ClientWebSocket openAiWebSocket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested && openAiWebSocket.State == WebSocketState.Open)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                await openAiWebSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, bytesRead),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error relaying from client to OpenAI");
        }
    }

    private async Task RelayFromOpenAIToClient(
        ClientWebSocket openAiWebSocket,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested && openAiWebSocket.State == WebSocketState.Open)
            {
                var result = await openAiWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer, 0, result.Count, cancellationToken);
                await outputStream.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error relaying from OpenAI to client");
        }
    }
}
