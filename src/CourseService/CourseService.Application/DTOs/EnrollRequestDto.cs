namespace CourseService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class EnrollRequestDto
{
    [Required(ErrorMessage = "CourseId is required.")]
    public Guid CourseId { get; set; }

    [Required(ErrorMessage = "StudentId is required.")]
    public Guid StudentId { get; set; }
}
