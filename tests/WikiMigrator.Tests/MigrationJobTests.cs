using Microsoft.Extensions.Logging;
using Moq;
using WikiMigrator.Application.Jobs;
using WikiMigrator.Application.Commands;
using WikiMigrator.Application.Queries;
using WikiMigrator.Domain.Entities;
using MediatR;

namespace WikiMigrator.Tests;

public class MigrationJobTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<MigrationJob>> _loggerMock;
    private readonly MigrationJob _job;

    public MigrationJobTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<MigrationJob>>();
        _job = new MigrationJob(_mediatorMock.Object, _loggerMock.Object);
    }

    // Task 5.2: Dry-Run Mode
    [Fact]
    public async Task ExecuteBatchAsync_WithDryRun_DoesNotWriteFiles()
    {
        // Arrange
        var inputFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(inputFolder);
        
        // Create a test file
        var testFile = Path.Combine(inputFolder, "test.md");
        await File.WriteAllTextAsync(testFile, "content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> { new WikiTiddler { Title = "Test", Content = "test" } });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteBatchAsync(inputFolder, outputFolder, dryRun: true);

            // Assert - output folder should not be created (dry run)
            Assert.False(Directory.Exists(outputFolder), "Output folder should not be created in dry-run mode");
            
            // Verify WriteFileCommand was NOT called
            _mediatorMock.Verify(m => m.Send(It.IsAny<WriteFileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(inputFolder))
                Directory.Delete(inputFolder, true);
        }
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithoutDryRun_WritesFiles()
    {
        // Arrange
        var inputFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(inputFolder);
        
        var testFile = Path.Combine(inputFolder, "test.md");
        await File.WriteAllTextAsync(testFile, "content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> { new WikiTiddler { Title = "Test", Content = "test" } });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteBatchAsync(inputFolder, outputFolder, dryRun: false);

            // Assert - output folder should be created
            Assert.True(Directory.Exists(outputFolder), "Output folder should be created");
            
            // Verify WriteFileCommand WAS called
            _mediatorMock.Verify(m => m.Send(It.IsAny<WriteFileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(inputFolder))
                Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);
        }
    }

    // Task 5.3: Logging
    [Fact]
    public async Task ExecuteBatchAsync_LogsStartAndEnd()
    {
        // Arrange
        var inputFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(inputFolder);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler>());

        try
        {
            // Act
            await _job.ExecuteBatchAsync(inputFolder, outputFolder);

            // Assert - Verify logging was called
            _loggerMock.Verify(
                x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
        finally
        {
            if (Directory.Exists(inputFolder))
                Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);
        }
    }

    // Regression test: Output folder must be used in file path (bug fix)
    [Fact]
    public async Task ExecuteAsync_UsesOutputFolder_InFilePath()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var inputFile = Path.Combine(tempFolder, "test.html");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempFolder);
        
        await File.WriteAllTextAsync(inputFile, "<html><body></body></html>");

        string? capturedFilePath = null;
        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> { new WikiTiddler { Title = "Test Note", Content = "test" } });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<WriteFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<WriteFileCommand, CancellationToken>((cmd, _) => capturedFilePath = cmd.FilePath)
            .ReturnsAsync(true);

        try
        {
            // Act
            await _job.ExecuteAsync(inputFile, outputFolder);

            // Assert - The file path must include the output folder
            Assert.NotNull(capturedFilePath);
            Assert.StartsWith(outputFolder, capturedFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".md", capturedFilePath);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);
        }
    }
}
