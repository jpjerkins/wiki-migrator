using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using WikiMigrator.Application.Behaviors;
using WikiMigrator.Application.Commands;
using WikiMigrator.Application.Queries;
using WikiMigrator.Application.Jobs;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Application.Services;
using WikiMigrator.Infrastructure.Parsers;

var services = new ServiceCollection();

// Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole());

// Register MediatR - scan all assemblies for handlers
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ParseFileCommand).Assembly);
    cfg.RegisterServicesFromAssemblyContaining<HtmlWikiParser>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Register all Application services
services.AddTransient<IConverter, WikiSyntaxConverter>();
services.AddTransient<IMarkdownWriter, MarkdownWriter>();
services.AddTransient<ITagProcessor, TagProcessor>();
services.AddTransient<ILinkResolver, LinkResolver>();
services.AddTransient<IMigrationPipeline, MigrationPipeline>();
services.AddTransient<IMigrationService, MigrationService>();
services.AddTransient<MigrationReportService>();
services.AddTransient<AdvancedWikiParser>();
services.AddTransient<BacklinkGenerator>();
services.AddTransient<LinkGraphBuilder>();
services.AddTransient<ImageReferenceParser>();
services.AddTransient<AttachmentCopier>();
services.AddTransient<ParaFolderResolver>();
services.AddTransient<ParaTagClassifier>();

// Register Infrastructure services 
services.AddTransient<IParser, HtmlWikiParser>();
services.AddTransient<IParser, TidFileParser>();
services.AddTransient<IParser, TiddlerWikiParser>();
services.AddTransient<IParser, TiddlyWiki5JsonParser>();
services.AddTransient<IParserFactory, ParserFactory>();

// Register MigrationJob
services.AddTransient<MigrationJob>();

// Register application settings
services.AddSingleton<IMigrationSettings>(sp =>
    configuration.GetSection("Migration").Get<MigrationSettings>() ?? new MigrationSettings());

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var settings = serviceProvider.GetRequiredService<IMigrationSettings>();
var job = serviceProvider.GetRequiredService<MigrationJob>();

// Check for command line arguments
if (args.Length > 0 && args[0] == "--file")
{
    // Single file migration
    var inputFile = args.Length > 1 ? args[1] : settings.InputFolder;
    var outputFolder = args.Length > 2 ? args[2] : settings.OutputFolder;
    
    logger.LogInformation("Running single file migration: {Input} -> {Output}", inputFile, outputFolder);
    await job.ExecuteAsync(inputFile, outputFolder);
    logger.LogInformation("Migration complete!");
}
else
{
    logger.LogInformation("Hello Wiki Migrator");
    logger.LogInformation("Usage: dotnet run -- --file <inputFile> <outputFolder>");
    logger.LogInformation("Or configure in appsettings.json and run without arguments for batch mode");
}

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
    string OutputFolder { get; set; }
    bool Recursive { get; set; }
    string FilePattern { get; set; }
}
