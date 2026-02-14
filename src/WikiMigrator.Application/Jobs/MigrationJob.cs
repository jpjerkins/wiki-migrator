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
    public async Task ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting migration job for file: {FilePath}", filePath);

        try
        {
            // Parse the file
            var tiddlers = await _mediator.Send(new ParseFileCommand(filePath), cancellationToken);
            
            foreach (var tiddler in tiddlers)
            {
                // Convert content
                var convertedContent = await _mediator.Send(
                    new ConvertContentCommand(tiddler), 
                    cancellationToken);
                
                // Write output
                await _mediator.Send(
                    new WriteFileCommand(tiddler.Title, convertedContent),
                    cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch migration job. Input: {Input}, Output: {Output}", 
            inputFolder, outputFolder);

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(inputFolder, filePattern, searchOption);

        _logger.LogInformation("Found {Count} files to migrate", files.Length);

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Batch migration cancelled");
                break;
            }

            await ExecuteAsync(file, cancellationToken);
        }

        _logger.LogInformation("Batch migration job completed");
    }
}
