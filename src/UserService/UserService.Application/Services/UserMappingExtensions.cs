namespace UserService.Application.Services;

using UserService.Application.DTOs;
using UserService.Domain.Entities;

public static class UserMappingExtensions
{
    public static UserDto ToDto(this User user)
        => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
}
