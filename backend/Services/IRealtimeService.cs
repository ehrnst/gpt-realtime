namespace GptRealtime.Api.Services;

public interface IRealtimeService
{
    Task HandleRealtimeSession(
        Stream inputStream, 
        Stream outputStream, 
        CancellationToken cancellationToken);
}
