namespace CourseService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Application.Services;
using CourseService.Domain.Entities;
using CourseService.Application.Common;
using Xunit;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courseRepoMock;
    private readonly Mock<IEnrollmentRepository> _enrollmentRepoMock;
    private readonly Mock<IUserServiceClient> _userClientMock;
    private readonly Mock<ILogger<CourseManagementService>> _loggerMock;
    private readonly CourseManagementService _sut;

    public CourseServiceTests()
    {
        _courseRepoMock = new Mock<ICourseRepository>();
        _enrollmentRepoMock = new Mock<IEnrollmentRepository>();
        _userClientMock = new Mock<IUserServiceClient>();
        _loggerMock = new Mock<ILogger<CourseManagementService>>();

        _sut = new CourseManagementService(
            _courseRepoMock.Object,
            _enrollmentRepoMock.Object,
            _userClientMock.Object,
            _loggerMock.Object);
    }

    #region CreateCourse Tests

    [Fact]
    public async Task CreateCourseAsync_WithValidInstructor_ReturnsCreatedCourse()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var request = new CreateCourseRequestDto
        {
            Title = "Introduction to C#",
            Description = "A comprehensive course on C# programming fundamentals",
            InstructorId = instructorId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(instructorId, default))
            .ReturnsAsync(new ExternalUserDto
            {
                Id = instructorId,
                FullName = "Prof. Johnson",
                Email = "prof@example.com",
                Role = "Instructor",
                IsActive = true
            });

        var createdCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            InstructorId = instructorId,
            InstructorName = "Prof. Johnson",
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _courseRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Course>(), default))
            .ReturnsAsync(createdCourse);

        // Act
        var result = await _sut.CreateCourseAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Title.Should().Be(request.Title);
        result.Data.InstructorName.Should().Be("Prof. Johnson");
    }

    [Fact]
    public async Task CreateCourseAsync_WithNonExistentInstructor_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateCourseRequestDto
        {
            Title = "C# Basics",
            Description = "Description of at least 10 chars",
            InstructorId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((ExternalUserDto?)null);

        // Act
        var result = await _sut.CreateCourseAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("does not exist");
        _courseRepoMock.Verify(r => r.CreateAsync(It.IsAny<Course>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateCourseAsync_WithStudentAsInstructor_ReturnsFailure()
    {
        // Arrange
        var request = new CreateCourseRequestDto
        {
            Title = "Course Title",
            Description = "Course description here",
            InstructorId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new ExternalUserDto
            {
                Id = request.InstructorId,
                Role = "Student",
                IsActive = true
            });

        // Act
        var result = await _sut.CreateCourseAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("not an Instructor");
    }

    #endregion

    #region UpdateCourse Tests

    [Fact]
    public async Task UpdateCourseAsync_ByOwner_ReturnsUpdatedCourse()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var existingCourse = new Course
        {
            Id = courseId,
            InstructorId = instructorId,
            Title = "Old Title",
            Description = "Old description",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var request = new UpdateCourseRequestDto
        {
            Title = "New Title",
            Description = "Updated description content here",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(60)
        };

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(existingCourse);

        _courseRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Course>(), default))
            .ReturnsAsync((Course c, CancellationToken _) => c);

        // Act
        var result = await _sut.UpdateCourseAsync(courseId, request, instructorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateCourseAsync_ByNonOwner_ReturnsForbidden()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course
        {
            Id = courseId,
            InstructorId = Guid.NewGuid()
        };

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(course);

        var request = new UpdateCourseRequestDto
        {
            Title = "Hacked",
            Description = "Not authorized description",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _sut.UpdateCourseAsync(courseId, request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateCourseAsync_WithNonExistentCourse_ReturnsNotFound()
    {
        // Arrange
        _courseRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Course?)null);

        var request = new UpdateCourseRequestDto
        {
            Title = "Title",
            Description = "Some description",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _sut.UpdateCourseAsync(Guid.NewGuid(), request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    #endregion

    #region DeleteCourse Tests

    [Fact]
    public async Task DeleteCourseAsync_ByOwner_ReturnsSuccess()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, InstructorId = instructorId });

        _courseRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Course>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteCourseAsync(courseId, instructorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _courseRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Course>(), default), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseAsync_ByNonOwner_ReturnsForbidden()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, InstructorId = Guid.NewGuid() });

        // Act
        var result = await _sut.DeleteCourseAsync(courseId, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _courseRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Course>(), default), Times.Never);
    }

    #endregion

    #region GetEnrolledCourses Tests

    [Fact]
    public async Task GetEnrolledCoursesAsync_ReturnsPaginatedCoursesForStudent()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courses = new List<Course>
        {
            new() { Id = Guid.NewGuid(), Title = "Course A", InstructorId = Guid.NewGuid(), Enrollments = new List<Enrollment>() },
            new() { Id = Guid.NewGuid(), Title = "Course B", InstructorId = Guid.NewGuid(), Enrollments = new List<Enrollment>() }
        };

        _courseRepoMock
            .Setup(r => r.GetEnrolledCoursesAsync(studentId, 1, 10, default))
            .ReturnsAsync(PagedResult<Course>.Create(courses, 2, 1, 10));

        // Act
        var result = await _sut.GetEnrolledCoursesAsync(studentId, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingCourse_ReturnsCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = new Course
        {
            Id = courseId,
            Title = "C# Fundamentals",
            Description = "Learn C# from scratch",
            InstructorId = Guid.NewGuid(),
            InstructorName = "Prof. Jones",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            Enrollments = new List<Enrollment>()
        };

        _courseRepoMock
            .Setup(r => r.GetByIdWithEnrollmentsAsync(courseId, default))
            .ReturnsAsync(course);

        // Act
        var result = await _sut.GetByIdAsync(courseId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(courseId);
        result.Data.Title.Should().Be("C# Fundamentals");
        result.Data.InstructorName.Should().Be("Prof. Jones");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentCourse_ReturnsNotFound()
    {
        // Arrange
        _courseRepoMock
            .Setup(r => r.GetByIdWithEnrollmentsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Course?)null);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion
}
