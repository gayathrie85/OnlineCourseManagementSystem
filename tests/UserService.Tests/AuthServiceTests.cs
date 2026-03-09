namespace UserService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using Xunit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessMessage()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
            Role = UserRole.Student
        };

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(request.Email.ToLowerInvariant(), default))
            .ReturnsAsync(false);

        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            Role = request.Role
        };

        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), default))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        result.Data!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            Role = UserRole.Student
        };

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(request.Email.ToLowerInvariant(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("already exists");
        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_AsInstructor_AssignsCorrectRole()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Prof Smith",
            Email = "prof@example.com",
            Password = "Password123!",
            Role = UserRole.Instructor
        };

        _userRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        User? capturedUser = null;
        _userRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<User>(), default))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.Instructor);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var password = "Password123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john@example.com",
            FullName = "John Doe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Student,
            IsActive = true
        };

        var request = new LoginRequestDto { Email = user.Email, Password = password };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        _tokenServiceMock
            .Setup(t => t.GenerateToken(user))
            .Returns("jwt_token_here");

        _tokenServiceMock
            .Setup(t => t.GetTokenExpiry())
            .Returns(DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt_token_here");
        result.Data.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Email = "john@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            IsActive = true
        };

        var request = new LoginRequestDto { Email = user.Email, Password = "WrongPassword" };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "nobody@example.com", Password = "anypassword" };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var password = "Password123!";
        var user = new User
        {
            Email = "inactive@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = false
        };

        var request = new LoginRequestDto { Email = user.Email, Password = password };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, default))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("deactivated");
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.Student,
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(userId);
        result.Data.FullName.Should().Be(user.FullName);
        result.Data.Role.Should().Be("Student");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    #endregion
}
