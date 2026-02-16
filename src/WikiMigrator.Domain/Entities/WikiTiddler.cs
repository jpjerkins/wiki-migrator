using Microsoft.Extensions.Logging;
using WikiMigrator.Domain.ValueObjects;

namespace WikiMigrator.Domain.Entities;

public partial class WikiTiddler
{
    private static ILogger? _logger;

    public string Title
    {
        get => title;
        set
        {
            _logger?.LogDebug("Title being set for WikiTiddler");
            title = value;
        }
    }
    private string title = string.Empty;

    public string Content
    {
        get => content;
        set
        {
            _logger?.LogDebug("Content being set for WikiTiddler");
            content = value;
        }
    }
    private string content = string.Empty;

    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public List<WikiField> Fields { get; set; } = new();
    public TiddlerMetadata Metadata { get; set; } = new();
    public List<string> Backlinks { get; set; } = new();

    public static void SetLogger(ILogger logger) => _logger = logger;
}

public class WikiField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class MigrationResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
