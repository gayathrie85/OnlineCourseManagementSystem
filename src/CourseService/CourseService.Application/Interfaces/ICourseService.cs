namespace CourseService.Application.Interfaces;

using CourseService.Application.Common;
using CourseService.Application.DTOs;

public interface ICourseService
{
    Task<Result<PagedResult<CourseDto>>> GetEnrolledCoursesAsync(Guid studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<CourseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CourseDto>> CreateCourseAsync(CreateCourseRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<CourseDto>> UpdateCourseAsync(Guid id, UpdateCourseRequestDto request, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteCourseAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<CourseDto>>> SearchCoursesAsync(CourseSearchRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<EnrollmentDto>> EnrollStudentAsync(EnrollRequestDto request, CancellationToken cancellationToken = default);
}
