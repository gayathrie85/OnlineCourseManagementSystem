namespace CourseService.API.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>Enroll a student in a course (Instructor only)</summary>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
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

        var result = await _courseService.EnrollStudentAsync(request, cancellationToken);

        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }
}
