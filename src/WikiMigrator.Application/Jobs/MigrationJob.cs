using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Commands;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Jobs;

/// <summary>
/// Background job that wraps the migration process using MediatR
/// </summary>
public class MigrationJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<MigrationJob> _logger;

    public MigrationJob(IMediator mediator, ILogger<MigrationJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Execute the migration job for a specific file
    /// </summary>
    public async Task ExecuteAsync(string filePath, string outputFolder, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration job for file: {FilePath}, DryRun: {DryRun}", filePath, dryRun);

        try
        {
            // Parse the file
            var tiddlers = await _mediator.Send(new ParseFileCommand(filePath), cancellationToken);
            
            // Filter out system tiddlers (titles starting with "$")
            tiddlers = tiddlers.Where(t => !t.Title.StartsWith("$")).ToList();
            
            _logger.LogInformation("Parsed {Count} tiddlers (excluding system tiddlers)", tiddlers.Count);
            
            foreach (var tiddler in tiddlers)
            {
                // Convert content
                var convertedContent = await _mediator.Send(
                    new ConvertContentCommand(tiddler), 
                    cancellationToken);
                
                // Write output only if not dry run
                if (!dryRun)
                {
                    var sanitizedTitle = SanitizeFileName(tiddler.Title);
                    var outputPath = Path.Combine(outputFolder, sanitizedTitle + ".md");
                    await _mediator.Send(
                        new WriteFileCommand(outputPath, convertedContent),
                        cancellationToken);
                }
                else
                {
                    _logger.LogInformation("DRY RUN: Would write file for tiddler: {Title}", tiddler.Title);
                }
            }

            _logger.LogInformation("Completed migration job for file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration job failed for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Execute the migration job for all files in the input folder
    /// </summary>
    public async Task ExecuteBatchAsync(
        string inputFolder, 
        string outputFolder, 
        string filePattern = "*.md",
        bool recursive = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (dryRun)
        {
            _logger.LogInformation("Starting DRY RUN batch migration. Input: {Input}, Output: {Output}", 
                inputFolder, outputFolder);
        }
        else
        {
            _logger.LogInformation("Starting batch migration job. Input: {Input}, Output: {Output}", 
                inputFolder, outputFolder);
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(inputFolder, filePattern, searchOption);

        _logger.LogInformation("Found {Count} files to migrate", files.Length);

        // Create output directory only if not dry run
        if (!dryRun && !string.IsNullOrEmpty(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Batch migration cancelled");
                break;
            }

            await ExecuteAsync(file, outputFolder, dryRun, cancellationToken);
        }

        _logger.LogInformation("Batch migration job completed. DryRun: {DryRun}", dryRun);
    }

    /// <summary>
    /// Sanitizes a filename for the file system
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "untitled";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // Replace spaces and underscores with hyphens
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\s_]+", "-");

        // Convert to lowercase
        sanitized = sanitized.ToLowerInvariant();

        // Remove leading/trailing hyphens
        sanitized = sanitized.Trim('-');

        return string.IsNullOrEmpty(sanitized) ? "untitled" : sanitized;
    }
}
