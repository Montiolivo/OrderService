using System.Net;
using System.Text.Json;
using FluentValidation;
using OrderService.Domain.Exceptions;

namespace OrderService.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
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
        var (statusCode, message, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),

            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Message,
                (Dictionary<string, string[]>?)null
            ),

            InsufficientStockException ise => (
                HttpStatusCode.Conflict,
                ise.Message,
                (Dictionary<string, string[]>?)null
            ),

            DomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                (Dictionary<string, string[]>?)null
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (Dictionary<string, string[]>?)null
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception [{Status}] for {Method} {Path}",
                (int)statusCode, context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse(
            Status: (int)statusCode,
            Message: message,
            Errors: errors,
            TraceId: context.TraceIdentifier
        );

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse(
    int Status,
    string Message,
    Dictionary<string, string[]>? Errors,
    string TraceId
);
