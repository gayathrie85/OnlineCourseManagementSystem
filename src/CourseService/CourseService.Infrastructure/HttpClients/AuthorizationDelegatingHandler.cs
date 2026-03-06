namespace CourseService.Infrastructure.HttpClients;

using Microsoft.AspNetCore.Http;

public class AuthorizationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);

        return await base.SendAsync(request, cancellationToken);
    }
}
