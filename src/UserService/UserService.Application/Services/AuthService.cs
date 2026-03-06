namespace UserService.Application.Services;

using Microsoft.Extensions.Logging;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

        if (await _userRepository.EmailExistsAsync(request.Email.ToLowerInvariant(), cancellationToken))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            return Result<AuthResponseDto>.Conflict("A user with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
        _logger.LogInformation("User registered successfully. Id: {UserId}", createdUser.Id);

        var token = _tokenService.GenerateToken(createdUser);
        return Result<AuthResponseDto>.Success(BuildAuthResponse(createdUser, token), 201);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email: {Email} - invalid credentials", request.Email);
            return Result<AuthResponseDto>.Unauthorized("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {UserId} is inactive", user.Id);
            return Result<AuthResponseDto>.Unauthorized("Account is deactivated.");
        }

        var token = _tokenService.GenerateToken(user);
        _logger.LogInformation("User {UserId} logged in successfully", user.Id);
        return Result<AuthResponseDto>.Success(BuildAuthResponse(user, token));
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
            return Result<UserDto>.NotFound($"User with Id '{id}' not found.");

        return Result<UserDto>.Success(user.ToDto());
    }

    private AuthResponseDto BuildAuthResponse(User user, string token)
        => new()
        {
            Token = token,
            ExpiresAt = _tokenService.GetTokenExpiry(),
            User = user.ToDto()
        };
}
