namespace UserService.Application.Interfaces;

using UserService.Domain.Entities;

public interface ITokenService
{
    string GenerateToken(User user);
    DateTime GetTokenExpiry();
}
