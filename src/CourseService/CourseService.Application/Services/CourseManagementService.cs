namespace CourseService.Application.Services;

using Microsoft.Extensions.Logging;
using CourseService.Application.Common;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;

public class CourseManagementService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<CourseManagementService> _logger;

    public CourseManagementService(
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IUserServiceClient userServiceClient,
        ILogger<CourseManagementService> logger)
    {
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<Result<PagedResult<CourseDto>>> GetEnrolledCoursesAsync(
        Guid studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching enrolled courses for student {StudentId}, page {Page}", studentId, pageNumber);

        var pagedCourses = await _courseRepository.GetEnrolledCoursesAsync(studentId, pageNumber, pageSize, cancellationToken);
        var result = PagedResult<CourseDto>.Create(
            pagedCourses.Items.Select(c => c.ToDto()),
            pagedCourses.TotalCount,
            pagedCourses.PageNumber,
            pagedCourses.PageSize);

        return Result<PagedResult<CourseDto>>.Success(result);
    }

    public async Task<Result<CourseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithEnrollmentsAsync(id, cancellationToken);

        if (course is null)
            return Result<CourseDto>.NotFound($"Course with Id '{id}' not found.");

        return Result<CourseDto>.Success(course.ToDto());
    }

    public async Task<Result<CourseDto>> CreateCourseAsync(
        CreateCourseRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating course for instructor {InstructorId}", request.InstructorId);

        var instructor = await _userServiceClient.GetUserByIdAsync(request.InstructorId, cancellationToken);

        if (instructor is null)
            return Result<CourseDto>.NotFound($"Instructor with Id '{request.InstructorId}' does not exist.");

        if (!instructor.IsActive)
            return Result<CourseDto>.Failure("The specified instructor account is inactive.");

        if (!instructor.Role.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            return Result<CourseDto>.Failure("The specified user is not an Instructor.");

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            InstructorId = request.InstructorId,
            InstructorName = instructor.FullName,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var created = await _courseRepository.CreateAsync(course, cancellationToken);
        _logger.LogInformation("Course {CourseId} created successfully", created.Id);

        return Result<CourseDto>.Success(created.ToDto(), 201);
    }

    public async Task<Result<CourseDto>> UpdateCourseAsync(
        Guid id, UpdateCourseRequestDto request, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);

        if (course is null)
            return Result<CourseDto>.NotFound($"Course with Id '{id}' not found.");

        if (course.InstructorId != requestingUserId)
            return Result<CourseDto>.Forbidden("You are not authorized to update this course.");

        course.Title = request.Title;
        course.Description = request.Description;
        course.StartDate = request.StartDate;
        course.EndDate = request.EndDate;
        course.UpdatedAt = DateTime.UtcNow;

        var updated = await _courseRepository.UpdateAsync(course, cancellationToken);
        _logger.LogInformation("Course {CourseId} updated by instructor {InstructorId}", id, requestingUserId);

        return Result<CourseDto>.Success(updated.ToDto());
    }

    public async Task<Result<bool>> DeleteCourseAsync(
        Guid id, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);

        if (course is null)
            return Result<bool>.NotFound($"Course with Id '{id}' not found.");

        if (course.InstructorId != requestingUserId)
            return Result<bool>.Forbidden("You are not authorized to delete this course.");

        await _courseRepository.DeleteAsync(course, cancellationToken);
        _logger.LogInformation("Course {CourseId} deleted by instructor {InstructorId}", id, requestingUserId);

        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<CourseDto>>> SearchCoursesAsync(
        CourseSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching courses — StartDate:{StartDate}, EndDate:{EndDate}, Instructor:{InstructorName}",
            request.StartDate, request.EndDate, request.InstructorName);

        var pagedCourses = await _courseRepository.SearchAsync(request, cancellationToken);
        var result = PagedResult<CourseDto>.Create(
            pagedCourses.Items.Select(c => c.ToDto()),
            pagedCourses.TotalCount,
            pagedCourses.PageNumber,
            pagedCourses.PageSize);

        return Result<PagedResult<CourseDto>>.Success(result);
    }

    public async Task<bool> IsStudentEnrolledAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _enrollmentRepository.IsEnrolledAsync(courseId, studentId, cancellationToken);
    }

    public async Task<Result<EnrollmentDto>> EnrollStudentAsync(
        EnrollRequestDto request, CancellationToken cancellationToken = default)
    {
        var studentId = request.StudentId!.Value;
        _logger.LogInformation("Enrolling student {StudentId} in course {CourseId}", studentId, request.CourseId);

        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);

        if (course is null)
            return Result<EnrollmentDto>.NotFound($"Course with Id '{request.CourseId}' not found.");

        if (!course.IsActive)
            return Result<EnrollmentDto>.Failure("Cannot enroll in an inactive course.");

        if (await _enrollmentRepository.IsEnrolledAsync(request.CourseId, studentId, cancellationToken))
            return Result<EnrollmentDto>.Conflict("Student is already enrolled in this course.");

        var student = await _userServiceClient.GetUserByIdAsync(studentId, cancellationToken);

        if (student is null)
            return Result<EnrollmentDto>.NotFound($"Student with Id '{studentId}' does not exist.");

        if (!student.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            return Result<EnrollmentDto>.Failure("Only students can be enrolled in courses.");

        var enrollment = new Enrollment
        {
            CourseId = request.CourseId,
            StudentId = studentId,
            StudentName = student.FullName,
            StudentEmail = student.Email
        };

        var created = await _enrollmentRepository.CreateAsync(enrollment, cancellationToken);
        _logger.LogInformation("Student {StudentId} enrolled in course {CourseId}", studentId, request.CourseId);

        return Result<EnrollmentDto>.Success(new EnrollmentDto
        {
            Id = created.Id,
            CourseId = created.CourseId,
            CourseTitle = course.Title,
            StudentId = created.StudentId,
            StudentName = created.StudentName,
            StudentEmail = created.StudentEmail,
            EnrolledAt = created.EnrolledAt
        }, 201);
    }

    public async Task<Result<BulkEnrollResponseDto>> BulkEnrollAsync(
        Guid studentId, List<Guid> courseIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk enrolling student {StudentId} in {Count} courses", studentId, courseIds.Count);

        var student = await _userServiceClient.GetUserByIdAsync(studentId, cancellationToken);

        if (student is null)
            return Result<BulkEnrollResponseDto>.NotFound($"Student with Id '{studentId}' does not exist.");

        if (!student.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            return Result<BulkEnrollResponseDto>.Failure("Only students can be enrolled in courses.");

        if (!student.IsActive)
            return Result<BulkEnrollResponseDto>.Failure("The student account is inactive.");

        var response = new BulkEnrollResponseDto();

        foreach (var courseId in courseIds.Distinct())
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);

            if (course is null)
            {
                response.Failed.Add(new BulkEnrollErrorDto { CourseId = courseId, Reason = "Course not found." });
                continue;
            }

            if (!course.IsActive)
            {
                response.Failed.Add(new BulkEnrollErrorDto { CourseId = courseId, Reason = "Course is inactive." });
                continue;
            }

            if (await _enrollmentRepository.IsEnrolledAsync(courseId, studentId, cancellationToken))
            {
                response.Failed.Add(new BulkEnrollErrorDto { CourseId = courseId, Reason = "Already enrolled." });
                continue;
            }

            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = studentId,
                StudentName = student.FullName,
                StudentEmail = student.Email
            };

            var created = await _enrollmentRepository.CreateAsync(enrollment, cancellationToken);

            response.Enrolled.Add(new EnrollmentDto
            {
                Id = created.Id,
                CourseId = created.CourseId,
                CourseTitle = course.Title,
                StudentId = created.StudentId,
                StudentName = created.StudentName,
                StudentEmail = created.StudentEmail,
                EnrolledAt = created.EnrolledAt
            });
        }

        _logger.LogInformation("Bulk enroll complete for student {StudentId}: {Success} enrolled, {Failed} failed",
            studentId, response.Enrolled.Count, response.Failed.Count);

        return Result<BulkEnrollResponseDto>.Success(response, 201);
    }
}
