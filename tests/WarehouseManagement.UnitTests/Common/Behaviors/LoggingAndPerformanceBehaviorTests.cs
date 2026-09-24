using MediatR;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Behaviors;
using WarehouseManagement.UnitTests.Common;

namespace WarehouseManagement.UnitTests.Common.Behaviors;

public class TestLogger<T> : ILogger<T>
{
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public List<LogEntry> Logs { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Logs.Add(new LogEntry
        {
            Level = logLevel,
            Message = formatter(state, exception),
            Exception = exception
        });
    }
}

public class LoggingAndPerformanceBehaviorTests
{
    public record SampleRequest(string Data) : IRequest<string>;

    [Fact]
    public async Task LoggingBehavior_WhenHandledSuccessfully_ShouldLogStartAndCompletion()
    {
        // Arrange
        var logger = new TestLogger<LoggingBehavior<SampleRequest, string>>();
        var currentUserService = new TestCurrentUserService();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger, currentUserService);
        var request = new SampleRequest("SampleData");

        // Act
        var result = await behavior.Handle(request, () => Task.FromResult("Response"), CancellationToken.None);

        // Assert
        Assert.Equal("Response", result);
        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Information && l.Message.Contains("Starting execution of SampleRequest"));
        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Information && l.Message.Contains("Completed execution of SampleRequest"));
    }

    [Fact]
    public async Task LoggingBehavior_WhenThrowsException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var logger = new TestLogger<LoggingBehavior<SampleRequest, string>>();
        var currentUserService = new TestCurrentUserService();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger, currentUserService);
        var request = new SampleRequest("SampleData");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, () => throw new InvalidOperationException("Handler exploded"), CancellationToken.None));

        Assert.Contains(logger.Logs, l => l.Level == LogLevel.Error && l.Message.Contains("failed"));
    }

    [Fact]
    public async Task PerformanceBehavior_WhenFastExecution_ShouldNotLogWarning()
    {
        // Arrange
        var logger = new TestLogger<PerformanceBehavior<SampleRequest, string>>();
        var currentUserService = new TestCurrentUserService();
        var behavior = new PerformanceBehavior<SampleRequest, string>(logger, currentUserService);
        var request = new SampleRequest("FastData");

        // Act
        var result = await behavior.Handle(request, () => Task.FromResult("Fast"), CancellationToken.None);

        // Assert
        Assert.Equal("Fast", result);
        Assert.DoesNotContain(logger.Logs, l => l.Level == LogLevel.Warning);
    }
}
