using Microsoft.AspNetCore.Mvc;
using GptRealtime.Api.Services;

namespace GptRealtime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealtimeController : ControllerBase
{
    private readonly IRealtimeService _realtimeService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RealtimeController> _logger;

    public RealtimeController(
        IRealtimeService realtimeService,
        ITokenService tokenService,
        ILogger<RealtimeController> logger)
    {
        _realtimeService = realtimeService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpGet("ws")]
    public async Task HandleWebSocket()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var token = HttpContext.Request.Query["token"].ToString();
        if (string.IsNullOrEmpty(token) || !_tokenService.ValidateToken(token))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Invalid or expired token");
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];

        try
        {
            var inputStream = new WebSocketStream(webSocket, buffer, isInput: true);
            var outputStream = new WebSocketStream(webSocket, buffer, isInput: false);

            await _realtimeService.HandleRealtimeSession(
                inputStream,
                outputStream,
                HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error");
        }
    }
}

public class WebSocketStream : Stream
{
    private readonly System.Net.WebSockets.WebSocket _webSocket;
    private readonly byte[] _buffer;
    private readonly bool _isInput;

    public WebSocketStream(System.Net.WebSockets.WebSocket webSocket, byte[] buffer, bool isInput)
    {
        _webSocket = webSocket;
        _buffer = buffer;
        _isInput = isInput;
    }

    public override bool CanRead => _isInput;
    public override bool CanWrite => !_isInput;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!_isInput) throw new NotSupportedException("Cannot read from output stream");

        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
        return result.Count;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_isInput) throw new NotSupportedException("Cannot write to input stream");

        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer, offset, count),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
