namespace UserService.API.Middleware;

using System.Net;
using System.Text.Json;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle error responses that have no body yet (e.g. 401/403 from auth pipeline)
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400)
                await WriteStatusErrorAsync(context);
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
                await HandleExceptionAsync(context, ex);
            else
                _logger.LogError(ex, "Exception after response started");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, userMessage) = exception switch
        {
            ArgumentException or ArgumentNullException =>
                (400, "The request contains invalid or missing data."),

            UnauthorizedAccessException =>
                (401, "Authentication is required to access this resource."),

            KeyNotFoundException =>
                (404, "The requested resource was not found."),

            InvalidOperationException =>
                (409, "The operation could not be completed due to a conflict."),

            OperationCanceledException =>
                (499, "The request was cancelled."),

            _ =>
                (500, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Handled exception [{Status}]: {Message}", statusCode, exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            statusCode,
            error = GetErrorTitle(statusCode),
            message = userMessage,
            // Only expose internal details in Development
            detail = _env.IsDevelopment() ? exception.Message : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static async Task WriteStatusErrorAsync(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;

        var message = statusCode switch
        {
            400 => "The request was invalid. Please check your input and try again.",
            401 => "You must be logged in to access this resource. Please provide a valid token.",
            403 => "You do not have permission to perform this action.",
            404 => "The resource you requested could not be found.",
            405 => "This HTTP method is not allowed for this endpoint.",
            409 => "A conflict occurred. The resource may already exist.",
            422 => "The request data failed validation. Please check your input.",
            429 => "Too many requests. Please slow down and try again later.",
            500 => "An unexpected server error occurred. Please try again later.",
            502 => "The server received an invalid response from an upstream service.",
            503 => "The service is temporarily unavailable. Please try again later.",
            _   => "An error occurred while processing your request."
        };

        context.Response.ContentType = "application/json";

        var response = new
        {
            statusCode,
            error = GetErrorTitle(statusCode),
            message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static string GetErrorTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        499 => "Client Closed Request",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _   => "Error"
    };
}
