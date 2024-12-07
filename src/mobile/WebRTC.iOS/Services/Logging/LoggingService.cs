namespace WebRTC.iOS.Services.Logging;

public class LoggingService : ILoggingService
{
    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - {message}");
    }
}