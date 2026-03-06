namespace CourseService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using CourseService.Application.Common;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using CourseService.Infrastructure.Data;

public class CourseRepository : ICourseRepository
{
    private readonly CourseDbContext _context;

    public CourseRepository(CourseDbContext context)
    {
        _context = context;
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Course?> GetByIdWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Course>> GetEnrolledCoursesAsync(
        Guid studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Enrollments)
            .Where(c => c.IsActive && c.Enrollments.Any(e => e.StudentId == studentId))
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Course>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<Course>> SearchAsync(
        CourseSearchRequestDto searchRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Enrollments)
            .Where(c => c.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchRequest.CourseName))
            query = query.Where(c =>
                c.Title.ToLower().Contains(searchRequest.CourseName.ToLower()));

        if (searchRequest.StartDate.HasValue)
            query = query.Where(c => c.StartDate >= searchRequest.StartDate.Value);

        if (searchRequest.EndDate.HasValue)
            query = query.Where(c => c.EndDate <= searchRequest.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(searchRequest.InstructorName))
            query = query.Where(c =>
                c.InstructorName.ToLower().Contains(searchRequest.InstructorName.ToLower()));

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((searchRequest.PageNumber - 1) * searchRequest.PageSize)
            .Take(searchRequest.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Course>.Create(items, totalCount, searchRequest.PageNumber, searchRequest.PageSize);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Courses.AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<Course> CreateAsync(Course course, CancellationToken cancellationToken = default)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);
        return course;
    }

    public async Task<Course> UpdateAsync(Course course, CancellationToken cancellationToken = default)
    {
        _context.Courses.Update(course);
        await _context.SaveChangesAsync(cancellationToken);
        return course;
    }

    public async Task DeleteAsync(Course course, CancellationToken cancellationToken = default)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
