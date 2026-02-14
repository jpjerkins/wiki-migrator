using System.Text.Json.Serialization;

namespace WikiMigrator.Domain.Reporting;

/// <summary>
/// Represents a single file migration result
/// </summary>
public class FileMigrationResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TiddlersFound { get; set; }
    public int TiddlersWritten { get; set; }
    public TimeSpan ParsingDuration { get; set; }
    public TimeSpan ConversionDuration { get; set; }
    public TimeSpan WriteDuration { get; set; }
    public TimeSpan TotalDuration => ParsingDuration + ConversionDuration + WriteDuration;
}

/// <summary>
/// Migration report containing statistics and results
/// </summary>
public class MigrationReport
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    
    /// <summary>
    /// Total files discovered in the input folder
    /// </summary>
    public int FilesDiscovered { get; set; }
    
    /// <summary>
    /// Files that were successfully parsed
    /// </summary>
    public int FilesParsed { get; set; }
    
    /// <summary>
    /// Files that were successfully converted
    /// </summary>
    public int FilesConverted { get; set; }
    
    /// <summary>
    /// Files that were successfully written to output
    /// </summary>
    public int FilesWritten { get; set; }
    
    /// <summary>
    /// Total errors encountered during migration
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Individual file migration results
    /// </summary>
    public List<FileMigrationResult> FileResults { get; set; } = new();
    
    /// <summary>
    /// Input folder path
    /// </summary>
    public string InputFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Output folder path
    /// </summary>
    public string OutputFolder { get; set; } = string.Empty;

    [JsonIgnore]
    public int SuccessCount => FileResults.Count(r => r.Success);
    
    [JsonIgnore]
    public double SuccessRate => FilesDiscovered > 0 
        ? (double)FilesWritten / FilesDiscovered * 100 
        : 0;
}
