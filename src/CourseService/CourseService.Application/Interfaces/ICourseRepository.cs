namespace CourseService.Application.Interfaces;

using CourseService.Application.Common;
using CourseService.Application.DTOs;
using CourseService.Domain.Entities;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Course?> GetByIdWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Course>> GetEnrolledCoursesAsync(Guid studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Course>> SearchAsync(CourseSearchRequestDto searchRequest, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Course> CreateAsync(Course course, CancellationToken cancellationToken = default);
    Task<Course> UpdateAsync(Course course, CancellationToken cancellationToken = default);
    Task DeleteAsync(Course course, CancellationToken cancellationToken = default);
}
