using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WikiMigrator.Domain.Reporting;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Service for generating migration reports.
/// </summary>
public class MigrationReportService
{
    private readonly ILogger<MigrationReportService> _logger;
    private MigrationReport _currentReport;

    public MigrationReportService(ILogger<MigrationReportService> logger)
    {
        _logger = logger;
        _currentReport = new MigrationReport();
    }

    /// <summary>
    /// Initialize a new report for a migration run.
    /// </summary>
    public void StartReport(string inputFolder, string outputFolder)
    {
        _currentReport = new MigrationReport
        {
            StartTime = DateTime.UtcNow,
            InputFolder = inputFolder,
            OutputFolder = outputFolder
        };
        _logger.LogInformation("Migration report started - Input: {Input}, Output: {Output}", 
            inputFolder, outputFolder);
    }

    /// <summary>
    /// Record the number of files discovered.
    /// </summary>
    public void RecordFilesDiscovered(int count)
    {
        _currentReport.FilesDiscovered = count;
        _logger.LogDebug("Recorded {Count} files discovered", count);
    }

    /// <summary>
    /// Record a file parsed successfully.
    /// </summary>
    public void RecordFileParsed(string filePath, int tiddlerCount, TimeSpan duration)
    {
        _currentReport.FilesParsed++;
        
        var result = _currentReport.FileResults.FirstOrDefault(r => r.FilePath == filePath);
        if (result != null)
        {
            result.TiddlersFound = tiddlerCount;
            result.ParsingDuration = duration;
        }
        else
        {
            _currentReport.FileResults.Add(new FileMigrationResult
            {
                FilePath = filePath,
                TiddlersFound = tiddlerCount,
                ParsingDuration = duration,
                Success = true
            });
        }
        
        _logger.LogDebug("Recorded parse for {FilePath}: {Count} tiddlers in {Duration}ms", 
            filePath, tiddlerCount, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Record a file converted successfully.
    /// </summary>
    public void RecordFileConverted(string filePath, TimeSpan duration)
    {
        _currentReport.FilesConverted++;
        
        var result = _currentReport.FileResults.FirstOrDefault(r => r.FilePath == filePath);
        if (result != null)
        {
            result.ConversionDuration = duration;
        }
        
        _logger.LogDebug("Recorded convert for {FilePath} in {Duration}ms", 
            filePath, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Record a file written successfully.
    /// </summary>
    public void RecordFileWritten(string filePath, int tiddlerCount, TimeSpan duration)
    {
        _currentReport.FilesWritten++;
        
        var result = _currentReport.FileResults.FirstOrDefault(r => r.FilePath == filePath);
        if (result != null)
        {
            result.TiddlersWritten = tiddlerCount;
            result.WriteDuration = duration;
        }
        
        _logger.LogDebug("Recorded write for {FilePath}: {Count} tiddlers in {Duration}ms", 
            filePath, tiddlerCount, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Record an error for a file.
    /// </summary>
    public void RecordError(string filePath, string errorMessage)
    {
        _currentReport.ErrorCount++;
        
        var result = _currentReport.FileResults.FirstOrDefault(r => r.FilePath == filePath);
        if (result != null)
        {
            result.Success = false;
            result.ErrorMessage = errorMessage;
        }
        else
        {
            _currentReport.FileResults.Add(new FileMigrationResult
            {
                FilePath = filePath,
                Success = false,
                ErrorMessage = errorMessage
            });
        }
        
        _logger.LogWarning("Recorded error for {FilePath}: {Error}", filePath, errorMessage);
    }

    /// <summary>
    /// Finalize the report.
    /// </summary>
    public MigrationReport FinalizeReport()
    {
        _currentReport.EndTime = DateTime.UtcNow;
        _logger.LogInformation("Migration report finalized - Duration: {Duration}", 
            _currentReport.Duration);
        return _currentReport;
    }

    /// <summary>
    /// Get the current report.
    /// </summary>
    public MigrationReport GetCurrentReport() => _currentReport;

    /// <summary>
    /// Generate and save JSON report to file.
    /// </summary>
    public async Task<string> SaveJsonReportAsync(string? outputPath = null)
    {
        var report = FinalizeReport();
        outputPath ??= Path.Combine(report.OutputFolder, "migration-report.json");
        
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await File.WriteAllTextAsync(outputPath, json);
        _logger.LogInformation("JSON report saved to: {Path}", outputPath);
        
        return outputPath;
    }

    /// <summary>
    /// Generate a console summary.
    /// </summary>
    public string GenerateConsoleSummary()
    {
        var report = FinalizeReport();
        var sb = new StringBuilder();
        
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                     MIGRATION REPORT SUMMARY");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  Input Folder:   {report.InputFolder}");
        sb.AppendLine($"  Output Folder:  {report.OutputFolder}");
        sb.AppendLine();
        sb.AppendLine("  ───────────────────────────────────────────────────────────");
        sb.AppendLine("  FILE STATISTICS");
        sb.AppendLine("  ───────────────────────────────────────────────────────────");
        sb.AppendLine($"    Files Discovered:  {report.FilesDiscovered,10:N0}");
        sb.AppendLine($"    Files Parsed:      {report.FilesParsed,10:N0}");
        sb.AppendLine($"    Files Converted:  {report.FilesConverted,10:N0}");
        sb.AppendLine($"    Files Written:     {report.FilesWritten,10:N0}");
        sb.AppendLine($"    Errors:           {report.ErrorCount,10:N0}");
        sb.AppendLine();
        sb.AppendLine("  ───────────────────────────────────────────────────────────");
        sb.AppendLine("  TIMING");
        sb.AppendLine("  ───────────────────────────────────────────────────────────");
        sb.AppendLine($"    Start Time:       {report.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"    End Time:         {report.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"} UTC");
        sb.AppendLine($"    Duration:         {report.Duration.TotalSeconds,10:F2} seconds");
        sb.AppendLine();
        
        if (report.FileResults.Any())
        {
            sb.AppendLine("  ───────────────────────────────────────────────────────────");
            sb.AppendLine("  FILE DETAILS");
            sb.AppendLine("  ───────────────────────────────────────────────────────────");
            
            foreach (var file in report.FileResults.OrderBy(f => f.FilePath))
            {
                var status = file.Success ? "✓" : "✗";
                var statusText = file.Success ? "SUCCESS" : "FAILED";
                sb.AppendLine($"    {status} [{statusText}] {file.FileName}");
                
                if (!file.Success && !string.IsNullOrEmpty(file.ErrorMessage))
                {
                    sb.AppendLine($"        Error: {file.ErrorMessage}");
                }
                else if (file.Success)
                {
                    sb.AppendLine($"        Tiddlers: {file.TiddlersFound} found, {file.TiddlersWritten} written");
                    sb.AppendLine($"        Time: {file.TotalDuration.TotalMilliseconds:F0}ms");
                }
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  SUCCESS RATE: {report.SuccessRate:F1}%");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }

    /// <summary>
    /// Print the console summary to logger.
    /// </summary>
    public void PrintConsoleSummary()
    {
        var summary = GenerateConsoleSummary();
        _logger.LogInformation("{Summary}", summary);
        Console.WriteLine(summary);
    }
}
