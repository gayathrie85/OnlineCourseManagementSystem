namespace CourseService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class BulkEnrollRequestDto
{
    /// <summary>
    /// Required for Instructors. Ignored for Students (taken from JWT token).
    /// </summary>
    public Guid? StudentId { get; set; }

    [Required(ErrorMessage = "At least one CourseId is required.")]
    [MinLength(1, ErrorMessage = "At least one CourseId is required.")]
    [MaxLength(20, ErrorMessage = "Cannot enroll in more than 20 courses at once.")]
    public List<Guid> CourseIds { get; set; } = new();
}
