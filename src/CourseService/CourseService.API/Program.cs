using CourseService.API.Extensions;
using CourseService.API.Middleware;
using CourseService.Application;
using CourseService.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CourseService API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/courseservice-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddApiVersioningConfig();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddRateLimiting(builder.Configuration);

    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddControllers();

    // Suppress the default "Server" header
    builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);

    var app = builder.Build();

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Swagger only in non-Production environments
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Course Service API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseSerilogRequestLogging();
    app.UseCors("CorsPolicy");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CourseService API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
