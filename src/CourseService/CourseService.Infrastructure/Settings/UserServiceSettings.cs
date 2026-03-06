namespace CourseService.Infrastructure.Settings;

public class UserServiceSettings
{
    public const string SectionName = "UserServiceSettings";
    public string BaseUrl { get; set; } = "http://localhost:5001";
}
