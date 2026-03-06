namespace CourseService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class CourseSearchRequestDto
{
    [StringLength(200, ErrorMessage = "Course name must not exceed 200 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\s.,:;'""&()\-]*$", ErrorMessage = "Course name can only contain letters, numbers, spaces, and basic punctuation.")]
    public string? CourseName { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [StringLength(100, ErrorMessage = "Instructor name must not exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z\s.'-]*$", ErrorMessage = "Instructor name can only contain letters, spaces, dots, apostrophes, and hyphens.")]
    public string? InstructorName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Page size must be between 1 and 50.")]
    public int PageSize { get; set; } = 10;
}
