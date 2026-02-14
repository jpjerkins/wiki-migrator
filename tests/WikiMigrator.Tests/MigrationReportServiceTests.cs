using Microsoft.Extensions.Logging;
using Moq;
using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Reporting;

namespace WikiMigrator.Tests;

public class MigrationReportServiceTests
{
    private readonly MigrationReportService _service;
    private readonly Mock<ILogger<MigrationReportService>> _loggerMock;

    public MigrationReportServiceTests()
    {
        _loggerMock = new Mock<ILogger<MigrationReportService>>();
        _service = new MigrationReportService(_loggerMock.Object);
    }

    [Fact]
    public void StartReport_InitializesNewReport()
    {
        // Arrange
        var inputFolder = "/input";
        var outputFolder = "/output";

        // Act
        _service.StartReport(inputFolder, outputFolder);
        var report = _service.GetCurrentReport();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(inputFolder, report.InputFolder);
        Assert.Equal(outputFolder, report.OutputFolder);
        Assert.True(report.StartTime <= DateTime.UtcNow);
        Assert.Null(report.EndTime);
    }

    [Fact]
    public void RecordFilesDiscovered_SetsCount()
    {
        // Arrange
        _service.StartReport("/input", "/output");

        // Act
        _service.RecordFilesDiscovered(10);
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(10, report.FilesDiscovered);
    }

    [Fact]
    public void RecordFileParsed_IncrementsParsedCount()
    {
        // Arrange
        _service.StartReport("/input", "/output");
        _service.RecordFilesDiscovered(5);

        // Act
        _service.RecordFileParsed("/input/file1.tid", 3, TimeSpan.FromMilliseconds(100));
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(1, report.FilesParsed);
        Assert.Equal(3, report.FileResults.First().TiddlersFound);
        Assert.Equal(TimeSpan.FromMilliseconds(100), report.FileResults.First().ParsingDuration);
    }

    [Fact]
    public void RecordFileConverted_IncrementsConvertedCount()
    {
        // Arrange
        _service.StartReport("/input", "/output");

        // Act
        _service.RecordFileParsed("/input/file1.tid", 3, TimeSpan.FromMilliseconds(100));
        _service.RecordFileConverted("/input/file1.tid", TimeSpan.FromMilliseconds(50));
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(1, report.FilesConverted);
        Assert.Equal(TimeSpan.FromMilliseconds(50), report.FileResults.First().ConversionDuration);
    }

    [Fact]
    public void RecordFileWritten_IncrementsWrittenCount()
    {
        // Arrange
        _service.StartReport("/input", "/output");

        // Act
        _service.RecordFileParsed("/input/file1.tid", 3, TimeSpan.FromMilliseconds(100));
        _service.RecordFileConverted("/input/file1.tid", TimeSpan.FromMilliseconds(50));
        _service.RecordFileWritten("/input/file1.tid", 3, TimeSpan.FromMilliseconds(25));
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(1, report.FilesWritten);
        Assert.Equal(3, report.FileResults.First().TiddlersWritten);
        Assert.Equal(TimeSpan.FromMilliseconds(25), report.FileResults.First().WriteDuration);
    }

    [Fact]
    public void RecordError_IncrementsErrorCount()
    {
        // Arrange
        _service.StartReport("/input", "/output");

        // Act
        _service.RecordError("/input/file1.tid", "Parse error");
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(1, report.ErrorCount);
        Assert.False(report.FileResults.First().Success);
        Assert.Equal("Parse error", report.FileResults.First().ErrorMessage);
    }

    [Fact]
    public void FinalizeReport_SetsEndTime()
    {
        // Arrange
        _service.StartReport("/input", "/output");
        Thread.Sleep(10); // Ensure some time passes

        // Act
        var report = _service.FinalizeReport();

        // Assert
        Assert.NotNull(report.EndTime);
        Assert.True(report.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void GetCurrentReport_ReturnsCurrentReport()
    {
        // Arrange
        _service.StartReport("/input", "/output");
        _service.RecordFilesDiscovered(5);

        // Act
        var report1 = _service.GetCurrentReport();
        var report2 = _service.GetCurrentReport();

        // Assert
        Assert.Same(report1, report2);
    }

    [Fact]
    public void GenerateConsoleSummary_ContainsExpectedContent()
    {
        // Arrange
        _service.StartReport("/input", "/output");
        _service.RecordFilesDiscovered(3);
        _service.RecordFileParsed("/input/file1.tid", 2, TimeSpan.FromMilliseconds(100));
        _service.RecordFileParsed("/input/file2.tid", 1, TimeSpan.FromMilliseconds(50));
        _service.RecordFileConverted("/input/file1.tid", TimeSpan.FromMilliseconds(30));
        _service.RecordFileConverted("/input/file2.tid", TimeSpan.FromMilliseconds(20));
        _service.RecordFileWritten("/input/file1.tid", 2, TimeSpan.FromMilliseconds(10));
        _service.RecordFileWritten("/input/file2.tid", 1, TimeSpan.FromMilliseconds(5));
        _service.RecordError("/input/file3.tid", "Test error");

        // Act
        var summary = _service.GenerateConsoleSummary();

        // Assert
        Assert.Contains("MIGRATION REPORT SUMMARY", summary);
        Assert.Contains("Files Discovered:", summary);
        Assert.Contains("3", summary);
        Assert.Contains("Files Parsed:", summary);
        Assert.Contains("2", summary);
        Assert.Contains("Files Written:", summary);
        Assert.Contains("2", summary);
        Assert.Contains("Errors:", summary);
        Assert.Contains("1", summary);
        Assert.Contains("SUCCESS RATE:", summary);
    }

    [Fact]
    public void SaveJsonReport_CreatesJsonFile()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.json");
        _service.StartReport("/input", "/output");
        _service.RecordFilesDiscovered(2);
        _service.RecordFileParsed("/input/file1.tid", 1, TimeSpan.FromMilliseconds(100));
        _service.RecordFileConverted("/input/file1.tid", TimeSpan.FromMilliseconds(50));
        _service.RecordFileWritten("/input/file1.tid", 1, TimeSpan.FromMilliseconds(25));

        // Act
        var resultPath = _service.SaveJsonReportAsync(tempPath).GetAwaiter().GetResult();

        // Assert
        Assert.True(File.Exists(resultPath));
        var json = File.ReadAllText(resultPath);
        Assert.Contains("filesDiscovered", json);
        Assert.Contains("filesWritten", json);
        
        // Cleanup
        File.Delete(resultPath);
    }

    [Fact]
    public void MultipleFiles_TrackingCorrect()
    {
        // Arrange
        _service.StartReport("/input", "/output");
        _service.RecordFilesDiscovered(5);

        // Act - simulate multiple files
        _service.RecordFileParsed("/input/file1.tid", 2, TimeSpan.FromMilliseconds(100));
        _service.RecordFileParsed("/input/file2.tid", 1, TimeSpan.FromMilliseconds(80));
        _service.RecordFileParsed("/input/file3.tid", 3, TimeSpan.FromMilliseconds(120));
        
        _service.RecordFileConverted("/input/file1.tid", TimeSpan.FromMilliseconds(50));
        _service.RecordFileConverted("/input/file2.tid", TimeSpan.FromMilliseconds(40));
        _service.RecordFileConverted("/input/file3.tid", TimeSpan.FromMilliseconds(60));
        
        _service.RecordFileWritten("/input/file1.tid", 2, TimeSpan.FromMilliseconds(10));
        _service.RecordFileWritten("/input/file2.tid", 1, TimeSpan.FromMilliseconds(10));
        
        _service.RecordError("/input/file4.tid", "Failed to parse");
        
        var report = _service.GetCurrentReport();

        // Assert
        Assert.Equal(5, report.FilesDiscovered);
        Assert.Equal(3, report.FilesParsed);
        Assert.Equal(3, report.FilesConverted);
        Assert.Equal(2, report.FilesWritten);
        Assert.Equal(1, report.ErrorCount);
        Assert.Equal(4, report.FileResults.Count);
    }
}
