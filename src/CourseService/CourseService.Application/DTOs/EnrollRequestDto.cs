namespace CourseService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class EnrollRequestDto
{
    [Required(ErrorMessage = "CourseId is required.")]
    public Guid CourseId { get; set; }

    /// <summary>
    /// Required for Instructors. Ignored for Students (taken from JWT token).
    /// </summary>
    public Guid? StudentId { get; set; }
}
