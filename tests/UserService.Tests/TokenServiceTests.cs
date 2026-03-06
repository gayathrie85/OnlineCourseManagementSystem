namespace UserService.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Settings;
using Xunit;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "SuperSecretTestKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };

        _sut = new TokenService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateToken_ForStudent_ReturnsValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Student",
            Email = "alice@example.com",
            Role = UserRole.Student
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(_jwtSettings.Issuer);
        jwt.Audiences.Should().Contain(_jwtSettings.Audience);
        jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Student");
        jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(user.Id.ToString());
        jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ForInstructor_ContainsInstructorRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Prof Smith",
            Email = "prof@example.com",
            Role = UserRole.Instructor
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Instructor");
    }

    [Fact]
    public void GenerateToken_IsSignedWithCorrectKey_PassesValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.Student
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert — validate signature using the same secret
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        var act = () => handler.ValidateToken(token, validationParams, out _);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_SignedWithWrongKey_FailsValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.Student
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert — validate with a different secret should throw
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WrongSecretKeyThatIsAlso32CharsLongXX")),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        var act = () => handler.ValidateToken(token, validationParams, out _);
        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    [Fact]
    public void GetTokenExpiry_ReturnsApproximatelyNowPlusExpiryMinutes()
    {
        // Act
        var before = DateTime.UtcNow;
        var expiry = _sut.GetTokenExpiry();
        var after = DateTime.UtcNow;

        // Assert
        expiry.Should().BeOnOrAfter(before.AddMinutes(_jwtSettings.ExpiryMinutes));
        expiry.Should().BeOnOrBefore(after.AddMinutes(_jwtSettings.ExpiryMinutes).AddSeconds(1));
    }

    [Fact]
    public void GenerateToken_EachCallProducesUniqueJti()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.Student
        };

        // Act
        var token1 = _sut.GenerateToken(user);
        var token2 = _sut.GenerateToken(user);

        // Assert — different JTI (JWT ID) each time prevents replay attacks
        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Id;
        var jti2 = handler.ReadJwtToken(token2).Id;
        jti1.Should().NotBe(jti2);
    }
}
