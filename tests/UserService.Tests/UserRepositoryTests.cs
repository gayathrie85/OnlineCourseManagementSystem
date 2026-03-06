namespace UserService.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;
using Xunit;

public class UserRepositoryTests
{
    private UserDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new UserDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistUser()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        var user = new User
        {
            FullName = "Alice Smith",
            Email = "alice@example.com",
            PasswordHash = "hashed_password",
            Role = UserRole.Student
        };

        // Act
        var created = await repo.CreateAsync(user);

        // Assert
        created.Id.Should().NotBeEmpty();
        (await context.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        var user = new User
        {
            FullName = "Bob Jones",
            Email = "bob@example.com",
            PasswordHash = "hash",
            Role = UserRole.Instructor
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var found = await repo.GetByEmailAsync("bob@example.com");

        // Assert
        found.Should().NotBeNull();
        found!.FullName.Should().Be("Bob Jones");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        // Act
        var found = await repo.GetByEmailAsync("nobody@example.com");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        context.Users.Add(new User
        {
            Email = "exists@example.com",
            PasswordHash = "hash",
            FullName = "Test",
            Role = UserRole.Student
        });
        await context.SaveChangesAsync();

        // Act
        var exists = await repo.EmailExistsAsync("exists@example.com");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        // Act
        var exists = await repo.EmailExistsAsync("ghost@example.com");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        var user = new User
        {
            FullName = "Carol White",
            Email = "carol@example.com",
            PasswordHash = "hash",
            Role = UserRole.Instructor
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var found = await repo.GetByIdAsync(user.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Email.Should().Be("carol@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var repo = new UserRepository(context);

        // Act
        var found = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        found.Should().BeNull();
    }
}
