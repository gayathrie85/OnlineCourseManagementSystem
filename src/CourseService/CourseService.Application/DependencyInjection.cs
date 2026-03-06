namespace CourseService.Application;

using Microsoft.Extensions.DependencyInjection;
using CourseService.Application.Interfaces;
using CourseService.Application.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICourseService, CourseManagementService>();
        return services;
    }
}
