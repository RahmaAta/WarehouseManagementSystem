using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs incoming requests with contextual user details
/// and execution duration.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
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
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
        var username = _currentUserService.Username ?? "Unauthenticated";

        _logger.LogInformation(
            "Starting execution of {RequestName} | User: {Username} (Id: {UserId})",
            requestName, username, userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed execution of {RequestName} in {ElapsedMilliseconds}ms | User: {Username}",
                requestName, stopwatch.ElapsedMilliseconds, username);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms | User: {Username}",
                requestName, stopwatch.ElapsedMilliseconds, username);
            throw;
        }
    }
}
