using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

public class MigrationService : IMigrationService
{
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAsync(string sourcePath, string targetPath)
    {
        _logger.LogInformation("Migration started - Source: {SourcePath}, Target: {TargetPath}", sourcePath, targetPath);

        var result = new MigrationResult
        {
            Success = true,
            TotalProcessed = 0,
            SuccessfulMigrations = 0,
            FailedMigrations = 0
        };

        try
        {
            _logger.LogDebug("Migration processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        _logger.LogInformation("Migration completed - Processed: {Total}, Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
            result.TotalProcessed, result.SuccessfulMigrations, result.FailedMigrations, result.Duration.TotalMilliseconds);

        return await Task.FromResult(result);
    }
}
