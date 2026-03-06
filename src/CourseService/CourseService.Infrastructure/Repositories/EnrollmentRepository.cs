namespace CourseService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using CourseService.Infrastructure.Data;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly CourseDbContext _context;

    public EnrollmentRepository(CourseDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
        => await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Enrollment>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        => await _context.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsEnrolledAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default)
        => await _context.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId, cancellationToken);

    public async Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);
        return enrollment;
    }
}
