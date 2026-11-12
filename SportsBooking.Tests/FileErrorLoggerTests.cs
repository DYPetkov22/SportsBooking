using System.Text.Json;
using Microsoft.Extensions.Logging;
using SportsBooking.Web.Infrastructure;

namespace SportsBooking.Tests;

public sealed class FileErrorLoggerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "sportsbooking-log-tests-" + Guid.NewGuid());
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    [Fact]
    public void WritesErrorAndExceptionToDailyJsonFile()
    {
        using var provider = new FileErrorLoggerProvider(_directory);
        provider.CreateLogger("Payments").LogError(new InvalidOperationException("Save failed"), "Payment {Id} failed", 17);
        var file = Assert.Single(Directory.GetFiles(_directory, "errors-*.log"));
        using var json = JsonDocument.Parse(Assert.Single(File.ReadAllLines(file)));
        Assert.Equal("Error", json.RootElement.GetProperty("Level").GetString());
        Assert.Equal("Payments", json.RootElement.GetProperty("Category").GetString());
        Assert.Contains("17", json.RootElement.GetProperty("Message").GetString());
        Assert.Contains("Save failed", json.RootElement.GetProperty("Exception").GetString());
    }

    [Fact]
    public void DoesNotCreateFilesForInformationMessages()
    {
        using var provider = new FileErrorLoggerProvider(_directory);
        provider.CreateLogger("Test").LogInformation("Normal request");
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void ConcurrentErrorsAreWrittenAsCompleteRecords()
    {
        using var provider = new FileErrorLoggerProvider(_directory);
        var logger = provider.CreateLogger("Test");
        Parallel.For(0, 40, i => logger.LogError("Failure {Id}", i));
        var lines = File.ReadAllLines(Assert.Single(Directory.GetFiles(_directory)));
        Assert.Equal(40, lines.Length);
        foreach (var line in lines) { using var json = JsonDocument.Parse(line); Assert.Equal("Error", json.RootElement.GetProperty("Level").GetString()); }
    }

    [Fact]
    public void UnwritableDirectoryDoesNotHideOriginalError()
    {
        Directory.CreateDirectory(_directory);
        var blocker = Path.Combine(_directory, "file");
        File.WriteAllText(blocker, "blocks directory creation");
        using var provider = new FileErrorLoggerProvider(Path.Combine(blocker, "logs"));
        Assert.Null(Record.Exception(() => provider.CreateLogger("Test").LogError("Original error")));
    }
}
