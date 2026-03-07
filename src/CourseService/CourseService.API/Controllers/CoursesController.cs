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
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(ICourseService courseService, ILogger<CoursesController> logger)
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
    /// Search all active courses with optional filters (paginated). Available to all authenticated users.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] CourseSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.SearchCoursesAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>Get a course by ID. Available to all authenticated users.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>Create a new course (Instructor only)</summary>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _courseService.CreateCourseAsync(request, cancellationToken);

        return result.IsSuccess
            ? StatusCode(result.StatusCode, result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>Update a course (Instructor only — must be course owner)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCourseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _courseService.UpdateCourseAsync(id, request, userId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

    /// <summary>Delete (deactivate) a course. Instructor only — must be course owner. Fails if students are enrolled.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _courseService.DeleteCourseAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(result.StatusCode, ErrorResponseDto.From(result.StatusCode, result.ErrorMessage!));
    }

}
