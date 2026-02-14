using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;
using WikiMigrator.Domain.Scanning;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Migration pipeline orchestrator that coordinates scan → parse → convert → write.
/// </summary>
public class MigrationPipeline : IMigrationPipeline
{
    private readonly IParserFactory _parserFactory;
    private readonly IConverter _converter;
    private readonly WikiDirectoryScanner _scanner;
    private readonly ILogger<MigrationPipeline> _logger;

    public MigrationPipeline(
        IParserFactory parserFactory,
        IConverter converter,
        WikiDirectoryScanner scanner,
        ILogger<MigrationPipeline> logger)
    {
        _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MigrationResult> RunAsync(
        string sourcePath,
        string targetPath,
        CancellationToken cancellationToken = default,
        IProgress<MigrationProgressEventArgs>? progress = null)
    {
        var result = new MigrationResult
        {
            Success = true,
            TotalProcessed = 0,
            SuccessfulMigrations = 0,
            FailedMigrations = 0
        };

        var startTime = DateTime.UtcNow;

        try
        {
            // Phase 1: Scanning
            _logger.LogInformation("Starting migration - Source: {SourcePath}, Target: {TargetPath}", sourcePath, targetPath);
            
            progress?.Report(new MigrationProgressEventArgs
            {
                Phase = MigrationPhase.Scanning,
                Current = 0,
                Total = 0,
                Message = "Scanning source directory..."
            });

            var scanResult = _scanner.Scan(sourcePath, sourcePath);
            var files = scanResult.Files;

            if (files.Count == 0)
            {
                _logger.LogWarning("No files found to migrate in {SourcePath}", sourcePath);
                progress?.Report(new MigrationProgressEventArgs
                {
                    Phase = MigrationPhase.Completed,
                    Current = 0,
                    Total = 0,
                    Message = "No files found to migrate"
                });
                return result;
            }

            _logger.LogInformation("Found {FileCount} files to migrate", files.Count);
            
            progress?.Report(new MigrationProgressEventArgs
            {
                Phase = MigrationPhase.Scanning,
                Current = files.Count,
                Total = files.Count,
                Message = $"Found {files.Count} files to migrate"
            });

            // Ensure target directory exists
            Directory.CreateDirectory(targetPath);

            // Process each file
            for (int i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var file = files[i];
                result.TotalProcessed++;

                try
                {
                    // Phase 2: Parsing
                    progress?.Report(new MigrationProgressEventArgs
                    {
                        Phase = MigrationPhase.Parsing,
                        Current = i + 1,
                        Total = files.Count,
                        CurrentFile = file.RelativePath,
                        Message = $"Parsing {file.FileName}..."
                    });

                    var tiddler = await ParseFileAsync(file, cancellationToken);
                    if (tiddler == null)
                    {
                        throw new InvalidOperationException($"Failed to parse file: {file.FileName}");
                    }

                    // Phase 3: Converting
                    progress?.Report(new MigrationProgressEventArgs
                    {
                        Phase = MigrationPhase.Converting,
                        Current = i + 1,
                        Total = files.Count,
                        CurrentFile = file.RelativePath,
                        Message = $"Converting {file.FileName}..."
                    });

                    var convertedContent = await _converter.ConvertAsync(tiddler);

                    // Phase 4: Writing
                    progress?.Report(new MigrationProgressEventArgs
                    {
                        Phase = MigrationPhase.Writing,
                        Current = i + 1,
                        Total = files.Count,
                        CurrentFile = file.RelativePath,
                        Message = $"Writing {file.FileName}..."
                    });

                    await WriteFileAsync(file, convertedContent, targetPath, cancellationToken);

                    result.SuccessfulMigrations++;
                    _logger.LogDebug("Successfully migrated: {FileName}", file.FileName);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Migration cancelled by user");
                    result.Success = false;
                    progress?.Report(new MigrationProgressEventArgs
                    {
                        Phase = MigrationPhase.Cancelled,
                        Current = i + 1,
                        Total = files.Count,
                        CurrentFile = file.RelativePath,
                        Message = "Migration cancelled"
                    });
                    throw;
                }
                catch (Exception ex)
                {
                    // Handle partial failure - log and continue
                    result.FailedMigrations++;
                    result.Errors.Add($"{file.RelativePath}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate file: {FileName}", file.FileName);
                    
                    progress?.Report(new MigrationProgressEventArgs
                    {
                        Phase = MigrationPhase.Writing,
                        Current = i + 1,
                        Total = files.Count,
                        CurrentFile = file.RelativePath,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            // Mark overall success if at least some files succeeded
            result.Success = result.SuccessfulMigrations > 0;

            if (result.FailedMigrations > 0)
            {
                _logger.LogWarning("Migration completed with {FailedCount} failures out of {TotalCount} files",
                    result.FailedMigrations, result.TotalProcessed);
            }
            else
            {
                _logger.LogInformation("Migration completed successfully - {Count} files migrated", result.SuccessfulMigrations);
            }

            progress?.Report(new MigrationProgressEventArgs
            {
                Phase = MigrationPhase.Completed,
                Current = files.Count,
                Total = files.Count,
                Message = $"Migration complete: {result.SuccessfulMigrations} succeeded, {result.FailedMigrations} failed"
            });
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Migration failed with error");
            
            progress?.Report(new MigrationProgressEventArgs
            {
                Phase = MigrationPhase.Failed,
                Current = result.TotalProcessed,
                Total = result.TotalProcessed,
                Message = $"Migration failed: {ex.Message}"
            });
        }
        finally
        {
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    private async Task<WikiTiddler?> ParseFileAsync(WikiFileInfo file, CancellationToken cancellationToken)
    {
        var parser = _parserFactory.GetParser(file.FullPath);
        if (parser == null)
        {
            _logger.LogWarning("No parser found for file: {FileName}", file.FileName);
            return null;
        }

        var content = await File.ReadAllTextAsync(file.FullPath, cancellationToken);
        var tiddlers = await parser.ParseAsync(content);

        return tiddlers.FirstOrDefault();
    }

    private async Task WriteFileAsync(
        WikiFileInfo file,
        string content,
        string targetPath,
        CancellationToken cancellationToken)
    {
        // Create target directory structure
        var relativeDir = Path.GetDirectoryName(file.RelativePath) ?? string.Empty;
        var targetDir = string.IsNullOrEmpty(relativeDir)
            ? targetPath
            : Path.Combine(targetPath, relativeDir);

        Directory.CreateDirectory(targetDir);

        // Change extension to .md
        var targetFileName = Path.GetFileNameWithoutExtension(file.FileName) + ".md";
        var targetFilePath = Path.Combine(targetDir, targetFileName);

        await File.WriteAllTextAsync(targetFilePath, content, cancellationToken);
    }
}
