namespace CourseService.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CourseService.Application.DTOs;
using CourseService.Domain.Entities;
using CourseService.Infrastructure.Data;
using CourseService.Infrastructure.Repositories;
using Xunit;

public class SearchAndPaginationTests
{
    private CourseDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CourseDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new CourseDbContext(options);
    }

    private async Task SeedCoursesAsync(CourseDbContext context)
    {
        var instructor1 = Guid.NewGuid();
        var instructor2 = Guid.NewGuid();

        var courses = new[]
        {
            new Course
            {
                Id = Guid.NewGuid(), Title = "C# Basics", Description = "Intro",
                InstructorId = instructor1, InstructorName = "Alice Smith",
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Course
            {
                Id = Guid.NewGuid(), Title = "Advanced .NET", Description = "Advanced",
                InstructorId = instructor1, InstructorName = "Alice Smith",
                StartDate = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Course
            {
                Id = Guid.NewGuid(), Title = "Python Intro", Description = "Python",
                InstructorId = instructor2, InstructorName = "Bob Jones",
                StartDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Course
            {
                Id = Guid.NewGuid(), Title = "Docker Fundamentals", Description = "Docker",
                InstructorId = instructor2, InstructorName = "Bob Jones",
                StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        };

        context.Courses.AddRange(courses);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_ByInstructorName_ReturnsMatchingCourses()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            InstructorName = "Alice",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(c => c.InstructorName.Contains("Alice"));
    }

    [Fact]
    public async Task SearchAsync_ByDateRange_ReturnsCoursesInRange()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(c =>
            c.StartDate >= searchRequest.StartDate.Value &&
            c.EndDate <= searchRequest.EndDate.Value);
    }

    [Fact]
    public async Task SearchAsync_Paginated_ReturnsCorrectPage()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_SecondPage_ReturnsCorrectItems()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var page1Request = new CourseSearchRequestDto { PageNumber = 1, PageSize = 2 };
        var page2Request = new CourseSearchRequestDto { PageNumber = 2, PageSize = 2 };

        // Act
        var page1 = await repo.SearchAsync(page1Request);
        var page2 = await repo.SearchAsync(page2Request);

        // Assert
        page2.Items.Should().HaveCount(2);
        page2.HasPreviousPage.Should().BeTrue();
        page2.HasNextPage.Should().BeFalse();

        var page1Ids = page1.Items.Select(c => c.Id).ToHashSet();
        var page2Ids = page2.Items.Select(c => c.Id).ToHashSet();
        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    [Fact]
    public async Task SearchAsync_ByInstructorAndDateRange_ReturnsCombinedFilter()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            InstructorName = "Bob",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().InstructorName.Should().Contain("Bob");
    }

    [Fact]
    public async Task SearchAsync_WithNoMatchingInstructor_ReturnsEmptyResult()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            InstructorName = "NonExistentInstructor",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ExcludesInactiveCourses()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);

        // Add an inactive course with same instructor as seeded data
        context.Courses.Add(new Course
        {
            Id = Guid.NewGuid(),
            Title = "Inactive Course",
            Description = "Should not appear",
            InstructorId = Guid.NewGuid(),
            InstructorName = "Alice Smith",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Utc),
            IsActive = false
        });
        await context.SaveChangesAsync();

        var repo = new CourseRepository(context);
        var searchRequest = new CourseSearchRequestDto
        {
            InstructorName = "Alice",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert — only the 2 active Alice courses, not the inactive one
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAllActiveCourses()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateContext(dbName);
        await SeedCoursesAsync(context);
        var repo = new CourseRepository(context);

        var searchRequest = new CourseSearchRequestDto
        {
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var result = await repo.SearchAsync(searchRequest);

        // Assert
        result.Items.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
    }
}
