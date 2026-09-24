using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Exceptions;
using WarehouseManagement.Domain.Exceptions;

namespace WarehouseManagement.API.Middlewares;

/// <summary>
/// Global middleware capturing uncaught domain and application exceptions 
/// and converting them into standard RFC 7807 ProblemDetails JSON responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ValidationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationProblemDetails = new ValidationProblemDetails(ex.Errors)
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Instance = context.Request.Path
                };
                _logger.LogWarning(ex, "Validation failure at {Path}: {@Errors}", context.Request.Path, ex.Errors);
                var validationJson = JsonSerializer.Serialize(validationProblemDetails, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await response.WriteAsync(validationJson);
                return;

            case DbUpdateConcurrencyException ex:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Concurrency Conflict";
                problemDetails.Detail = "A concurrency conflict occurred. The record was modified by another operation. Please reload the latest state and retry.";
                _logger.LogWarning(ex, "Concurrency conflict detected at {Path}", context.Request.Path);
                break;
            case KeyNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = ex.Message;
                _logger.LogWarning(ex, "Resource not found at {Path}", context.Request.Path);
                break;

            case DomainException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Domain Rule Violation";
                problemDetails.Detail = ex.Message;
                _logger.LogWarning(ex, "Domain exception at {Path}", context.Request.Path);
                break;

            case InvalidOperationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Operation";
                problemDetails.Detail = ex.Message;
                _logger.LogWarning(ex, "Invalid operation at {Path}", context.Request.Path);
                break;

            case ArgumentException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Argument";
                problemDetails.Detail = ex.Message;
                _logger.LogWarning(ex, "Argument exception at {Path}", context.Request.Path);
                break;

            case UnauthorizedAccessException ex:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = ex.Message;
                _logger.LogWarning(ex, "Forbidden access at {Path}", context.Request.Path);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred. Please contact system administrator.";
                _logger.LogError(exception, "Unhandled error processing request at {Path}", context.Request.Path);
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }
}
