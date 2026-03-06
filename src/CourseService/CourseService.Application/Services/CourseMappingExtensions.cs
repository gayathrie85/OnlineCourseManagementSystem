namespace CourseService.Application.Services;

using CourseService.Application.DTOs;
using CourseService.Domain.Entities;

public static class CourseMappingExtensions
{
    public static CourseDto ToDto(this Course course)
        => new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            InstructorId = course.InstructorId,
            InstructorName = course.InstructorName,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            EnrolledStudentsCount = course.Enrollments?.Count ?? 0,
            IsActive = course.IsActive
        };
}
