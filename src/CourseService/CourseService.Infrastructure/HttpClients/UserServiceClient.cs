namespace CourseService.Infrastructure.HttpClients;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalUserDto?> GetUserByIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/users/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserService returned {StatusCode} for user {UserId}",
                    response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExternalUserDto>(
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach UserService when fetching user {UserId}", userId);
            return null;
        }
    }
}
