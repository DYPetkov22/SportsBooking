using System.Text.Json;

namespace SportsBooking.Web.Infrastructure;

/// <summary>Daily JSON-lines error files outside wwwroot. A logging failure never hides the original exception.</summary>
public sealed class FileErrorLoggerProvider(string directory) : ILoggerProvider
{
    private readonly object _gate = new();
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);
    public void Dispose() { }

    private void Write<TState>(string category, LogLevel level, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var line = JsonSerializer.Serialize(new
            {
                TimestampUtc = now, Level = level.ToString(), Category = category,
                EventId = eventId.Id, Message = formatter(state, exception), Exception = exception?.ToString()
            });
            lock (_gate)
            {
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, $"errors-{now:yyyy-MM-dd}.log"), line + Environment.NewLine);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"File error logging unavailable: {ex.Message}");
        }
    }

    private sealed class FileLogger(FileErrorLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error && logLevel != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) provider.Write(category, logLevel, eventId, state, exception, formatter);
        }
    }
}
