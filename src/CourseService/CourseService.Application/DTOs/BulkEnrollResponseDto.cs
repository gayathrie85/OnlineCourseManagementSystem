namespace CourseService.Application.DTOs;

public class BulkEnrollResponseDto
{
    public List<EnrollmentDto> Enrolled { get; set; } = new();
    public List<BulkEnrollErrorDto> Failed { get; set; } = new();
}

public class BulkEnrollErrorDto
{
    public Guid CourseId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
