namespace CourseService.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CourseService.Application.Interfaces;
using CourseService.Infrastructure.Data;
using CourseService.Infrastructure.HttpClients;
using CourseService.Infrastructure.Repositories;
using CourseService.Infrastructure.Settings;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var userServiceSettings = configuration
            .GetSection(UserServiceSettings.SectionName)
            .Get<UserServiceSettings>() ?? new UserServiceSettings();

        services.Configure<UserServiceSettings>(
            configuration.GetSection(UserServiceSettings.SectionName));

        services.AddDbContext<CourseDbContext>(options =>
            options.UseInMemoryDatabase("CourseServiceDb"));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        services.AddHttpContextAccessor();
        services.AddTransient<AuthorizationDelegatingHandler>();

        services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
        {
            client.BaseAddress = new Uri(userServiceSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();

        return services;
    }
}
