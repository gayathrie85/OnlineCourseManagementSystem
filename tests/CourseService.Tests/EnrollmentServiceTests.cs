namespace CourseService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Application.Services;
using CourseService.Domain.Entities;
using Xunit;

public class EnrollmentServiceTests
{
    private readonly Mock<ICourseRepository> _courseRepoMock;
    private readonly Mock<IEnrollmentRepository> _enrollmentRepoMock;
    private readonly Mock<IUserServiceClient> _userClientMock;
    private readonly Mock<ILogger<CourseManagementService>> _loggerMock;
    private readonly CourseManagementService _sut;

    public EnrollmentServiceTests()
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

    [Fact]
    public async Task EnrollStudentAsync_WithValidRequest_ReturnsCreatedEnrollment()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var course = new Course
        {
            Id = courseId,
            Title = "C# Fundamentals",
            IsActive = true,
            InstructorId = Guid.NewGuid()
        };

        var request = new EnrollRequestDto { CourseId = courseId, StudentId = studentId };

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(course);

        _enrollmentRepoMock
            .Setup(r => r.IsEnrolledAsync(courseId, studentId, default))
            .ReturnsAsync(false);

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(studentId, default))
            .ReturnsAsync(new ExternalUserDto
            {
                Id = studentId,
                FullName = "Alice Student",
                Email = "alice@example.com",
                Role = "Student",
                IsActive = true
            });

        var createdEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Course = course,
            StudentId = studentId,
            StudentName = "Alice Student",
            StudentEmail = "alice@example.com",
            EnrolledAt = DateTime.UtcNow
        };

        _enrollmentRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Enrollment>(), default))
            .ReturnsAsync(createdEnrollment);

        // Act
        var result = await _sut.EnrollStudentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.CourseId.Should().Be(courseId);
        result.Data.StudentId.Should().Be(studentId);
        result.Data.StudentName.Should().Be("Alice Student");
    }

    [Fact]
    public async Task EnrollStudentAsync_AlreadyEnrolled_ReturnsConflict()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, IsActive = true });

        _enrollmentRepoMock
            .Setup(r => r.IsEnrolledAsync(courseId, studentId, default))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.EnrollStudentAsync(new EnrollRequestDto
        {
            CourseId = courseId,
            StudentId = studentId
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("already enrolled");
    }

    [Fact]
    public async Task EnrollStudentAsync_WithNonExistentCourse_ReturnsNotFound()
    {
        // Arrange
        _courseRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Course?)null);

        // Act
        var result = await _sut.EnrollStudentAsync(new EnrollRequestDto
        {
            CourseId = Guid.NewGuid(),
            StudentId = Guid.NewGuid()
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithInactiveCourse_ReturnsFailure()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, IsActive = false });

        // Act
        var result = await _sut.EnrollStudentAsync(new EnrollRequestDto
        {
            CourseId = courseId,
            StudentId = Guid.NewGuid()
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task EnrollStudentAsync_WithInstructorAsStudent_ReturnsFailure()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, IsActive = true });

        _enrollmentRepoMock
            .Setup(r => r.IsEnrolledAsync(courseId, instructorId, default))
            .ReturnsAsync(false);

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(instructorId, default))
            .ReturnsAsync(new ExternalUserDto
            {
                Id = instructorId,
                Role = "Instructor",
                IsActive = true
            });

        // Act
        var result = await _sut.EnrollStudentAsync(new EnrollRequestDto
        {
            CourseId = courseId,
            StudentId = instructorId
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("Only students");
    }

    [Fact]
    public async Task EnrollStudentAsync_WithNonExistentStudent_ReturnsNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        _courseRepoMock
            .Setup(r => r.GetByIdAsync(courseId, default))
            .ReturnsAsync(new Course { Id = courseId, IsActive = true });

        _enrollmentRepoMock
            .Setup(r => r.IsEnrolledAsync(courseId, studentId, default))
            .ReturnsAsync(false);

        _userClientMock
            .Setup(c => c.GetUserByIdAsync(studentId, default))
            .ReturnsAsync((ExternalUserDto?)null);

        // Act
        var result = await _sut.EnrollStudentAsync(new EnrollRequestDto
        {
            CourseId = courseId,
            StudentId = studentId
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
