namespace CourseService.API.Controllers;

using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CourseService.Application.Common;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class EnrollmentsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(ICourseService courseService, ILogger<EnrollmentsController> logger)
    {
        _courseService = courseService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get all courses the logged-in student is enrolled in (paginated). Student only.
    /// </summary>
    [HttpGet("my-courses")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolledCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetCurrentUserId();
        var result = await _courseService.GetEnrolledCoursesAsync(studentId, pageNumber, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>
    /// Enroll in a course. Students can self-enroll (StudentId is ignored and taken from token).
    /// Instructors can enroll any student by providing StudentId.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student,Instructor")]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnrollStudent(
        [FromBody] EnrollRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var role = GetCurrentUserRole();

        // Students can only enroll themselves
        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            request.StudentId = GetCurrentUserId();
        }
        else
        {
            if (request.StudentId is null || request.StudentId == Guid.Empty)
                return BadRequest(ErrorResponseDto.From(400, "StudentId is required for Instructor enrollment."));
        }

        var result = await _courseService.EnrollStudentAsync(request, cancellationToken);

        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>
    /// Bulk enroll in multiple courses at once.
    /// Students: provide a list of CourseIds (StudentId is ignored, taken from token).
    /// Instructors: provide a StudentId and a list of CourseIds.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Student,Instructor")]
    [ProducesResponseType(typeof(BulkEnrollResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkEnroll(
        [FromBody] BulkEnrollRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var role = GetCurrentUserRole();
        Guid studentId;

        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            studentId = GetCurrentUserId();
        }
        else
        {
            if (request.StudentId is null || request.StudentId == Guid.Empty)
                return BadRequest(ErrorResponseDto.From(400, "StudentId is required for Instructor enrollment."));

            studentId = request.StudentId.Value;
        }

        var result = await _courseService.BulkEnrollAsync(studentId, request.CourseIds, cancellationToken);

        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }
}
