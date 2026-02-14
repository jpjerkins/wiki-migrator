using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Interfaces;

/// <summary>
/// Progress event arguments for migration pipeline events.
/// </summary>
public class MigrationProgressEventArgs : EventArgs
{
    public required MigrationPhase Phase { get; init; }
    public required int Current { get; init; }
    public required int Total { get; init; }
    public string? CurrentFile { get; init; }
    public string? Message { get; init; }
    public double PercentComplete => Total > 0 ? (double)Current / Total * 100 : 0;
}

/// <summary>
/// Represents a single file migration result.
/// </summary>
public class FileMigrationResult
{
    public required string SourcePath { get; set; }
    public required string TargetPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public WikiTiddler? Tiddler { get; set; }
}

/// <summary>
/// Migration pipeline phases.
/// </summary>
public enum MigrationPhase
{
    Scanning,
    Parsing,
    Converting,
    Writing,
    Completed,
    Cancelled,
    Failed
}

/// <summary>
/// Interface for the migration pipeline orchestrator.
/// </summary>
public interface IMigrationPipeline
{
    /// <summary>
    /// Runs the migration pipeline with the specified options.
    /// </summary>
    /// <param name="sourcePath">Source directory path.</param>
    /// <param name="targetPath">Target directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress handler.</param>
    /// <returns>Migration result with statistics.</returns>
    Task<MigrationResult> RunAsync(
        string sourcePath, 
        string targetPath,
        CancellationToken cancellationToken = default,
        IProgress<MigrationProgressEventArgs>? progress = null);
}
