namespace CourseService.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateCourseRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\s.,:;'""&()\-]+$", ErrorMessage = "Title can only contain letters, numbers, spaces, and basic punctuation.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "InstructorId is required.")]
    public Guid InstructorId { get; set; }

    [Required(ErrorMessage = "StartDate is required.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "EndDate is required.")]
    public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
            yield return new ValidationResult(
                "EndDate must be after StartDate.",
                new[] { nameof(EndDate) });

        if (StartDate.Date < DateTime.UtcNow.Date)
            yield return new ValidationResult(
                "StartDate cannot be in the past.",
                new[] { nameof(StartDate) });
    }
}
