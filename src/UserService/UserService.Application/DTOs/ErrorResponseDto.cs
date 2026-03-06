namespace UserService.Application.DTOs;

public class ErrorResponseDto
{
    public int StatusCode { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static ErrorResponseDto From(int statusCode, string message) => new()
    {
        StatusCode = statusCode,
        Error = statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _   => "Error"
        },
        Message = message
    };
}
