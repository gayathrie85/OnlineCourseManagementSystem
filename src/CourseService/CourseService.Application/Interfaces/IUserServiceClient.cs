namespace CourseService.Application.Interfaces;

using CourseService.Application.DTOs;

public interface IUserServiceClient
{
    Task<ExternalUserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
