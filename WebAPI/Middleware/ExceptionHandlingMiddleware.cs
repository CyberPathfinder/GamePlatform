using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Success = false,
            Message = "An internal error occurred.",
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case AuthenticationException authEx:
                // Mask the actual reason in logs to prevent enumeration/info leakage
                _logger.LogWarning("Authentication failed.");
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = authEx.Message;
                break;
            case ValidationException valEx: // System.ComponentModel.DataAnnotations.ValidationException
                _logger.LogWarning("Validation failed: {Message}", valEx.Message);
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = valEx.Message;
                break;
            case FluentValidation.ValidationException fluentValEx:
                _logger.LogWarning("Validation failed: {Message}", fluentValEx.Message);
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = fluentValEx.Errors.Select(e => e.ErrorMessage).ToList();
                break;
            case KeyNotFoundException keyEx:
                _logger.LogWarning("Resource not found: {Message}", keyEx.Message);
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = keyEx.Message;
                break;
            default:
                _logger.LogError(exception, "An unexpected error occurred.");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // In production, you might want to hide the exact exception message
                // errorResponse.Message = "Internal Server Error";
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var result = JsonSerializer.Serialize(errorResponse, options);
        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public string? TraceId { get; set; }
}
