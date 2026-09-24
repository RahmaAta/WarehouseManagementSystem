using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.API.Middlewares;
using WarehouseManagement.Application.Common.Exceptions;
using WarehouseManagement.UnitTests.Common.Behaviors;

namespace WarehouseManagement.UnitTests.Common.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly TestLogger<ExceptionHandlingMiddleware> _logger = new();

    private DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/test-endpoint";
        return context;
    }

    private async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_ShouldReturn400WithValidationProblemDetails()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
        {
            var failures = new List<ValidationFailure>
            {
                new("SKU", "SKU cannot be empty."),
                new("Price", "Price must be greater than zero.")
            };
            throw new ValidationException(failures);
        }, _logger);

        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var body = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Validation Failed", problemDetails.Title);
        Assert.True(problemDetails.Errors.ContainsKey("SKU"));
        Assert.True(problemDetails.Errors.ContainsKey("Price"));
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundExceptionThrown_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
        {
            throw new KeyNotFoundException("Product with ID 999 not found.");
        }, _logger);

        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Resource Not Found", problemDetails.Title);
        Assert.Contains("999", problemDetails.Detail);
    }

    [Fact]
    public async Task InvokeAsync_WhenDbUpdateConcurrencyExceptionThrown_ShouldReturn409WithConflictDetails()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
        {
            throw new DbUpdateConcurrencyException("Concurrency conflict");
        }, _logger);

        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("Concurrency Conflict", problemDetails.Title);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionThrown_ShouldReturn500()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
        {
            throw new Exception("Unexpected database connection crash");
        }, _logger);

        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("Internal Server Error", problemDetails.Title);
    }
}
