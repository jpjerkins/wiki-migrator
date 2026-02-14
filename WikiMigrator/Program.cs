using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Hangfire;
using Hangfire.InMemory;
using WikiMigrator.Application.Behaviors;
using WikiMigrator.Application.Commands;
using WikiMigrator.Application.Queries;
using WikiMigrator.Application.Jobs;

var services = new ServiceCollection();

// Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole());

// Register MediatR
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ParseFileCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Register Hangfire with in-memory storage
services.AddHangfire(config => config
    .UseInMemoryStorage());
services.AddHangfireServer();

// Register MigrationJob
services.AddTransient<MigrationJob>();

// Register application services
services.AddSingleton<IMigrationSettings>(sp =>
    configuration.GetSection("Migration").Get<MigrationSettings>() ?? new MigrationSettings());

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Start Hangfire server for background job processing
var hangfireServer = serviceProvider.GetRequiredService<BackgroundJobServer>();
logger.LogInformation("Hangfire server started");

logger.LogInformation("Hello Wiki Migrator");

public class MigrationSettings : IMigrationSettings
{
    public string InputFolder { get; set; } = "./input";
    public string OutputFolder { get; set; } = "./output";
    public bool Recursive { get; set; } = true;
    public string FilePattern { get; set; } = "*.md";
}

public interface IMigrationSettings
{
    string InputFolder { get; }
    string OutputFolder { get; }
    bool Recursive { get; }
    string FilePattern { get; }
}
