using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that monitors request execution time and emits warnings
/// for long-running requests that exceed performance thresholds (e.g., > 500ms).
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long ThresholdMilliseconds = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > ThresholdMilliseconds)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
            var username = _currentUserService.Username ?? "Unauthenticated";

            _logger.LogWarning(
                "Long Running Request Detected: {RequestName} took {ElapsedMilliseconds}ms (> {Threshold}ms) | User: {Username} (Id: {UserId})",
                requestName, elapsedMilliseconds, ThresholdMilliseconds, username, userId);
        }

        return response;
    }
}
