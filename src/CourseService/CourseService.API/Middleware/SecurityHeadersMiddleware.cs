namespace CourseService.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Prevent MIME-type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Block reflected XSS (legacy browsers)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Control referrer information sent with requests
        headers["Referrer-Policy"] = "no-referrer";

        // Restrict access to browser features
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Content Security Policy — APIs don't serve HTML, so restrict everything
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Remove server identity header
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await _next(context);
    }
}
