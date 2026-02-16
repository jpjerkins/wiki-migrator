using Microsoft.Extensions.Logging;
using Moq;
using WikiMigrator.Application.Jobs;
using WikiMigrator.Application.Commands;
using WikiMigrator.Domain.Entities;
using MediatR;

namespace WikiMigrator.Tests;

/// <summary>
/// Tests for dry-run mode functionality (Task 5.2)
/// </summary>
public class DryRunModeTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<MigrationJob>> _loggerMock;
    private readonly MigrationJob _job;

    public DryRunModeTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<MigrationJob>>();
        _job = new MigrationJob(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithDryRun_DoesNotCreateOutputDirectory()
    {
        // Arrange
        var inputFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(inputFolder);
        
        // Create a test file
        var testFile = Path.Combine(inputFolder, "test.md");
        await File.WriteAllTextAsync(testFile, "test content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> 
            { 
                new WikiTiddler { Title = "Test", Content = "test content" } 
            });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteBatchAsync(inputFolder, outputFolder, dryRun: true);

            // Assert - output folder should NOT be created in dry-run mode
            Assert.False(Directory.Exists(outputFolder), 
                "Output directory should not be created in dry-run mode");
            
            // Verify WriteFileCommand was NOT called
            _mediatorMock.Verify(
                m => m.Send(It.Is<WriteFileCommand>(c => true), It.IsAny<CancellationToken>()), 
                Times.Never);
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

    [Fact]
    public async Task ExecuteAsync_WithDryRun_DoesNotWriteFile()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
        await File.WriteAllTextAsync(testFile, "test content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> 
            { 
                new WikiTiddler { Title = "Test", Content = "test" } 
            });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteAsync(testFile, dryRun: true);

            // Assert - WriteFileCommand should NOT be called
            _mediatorMock.Verify(
                m => m.Send(It.Is<WriteFileCommand>(c => true), It.IsAny<CancellationToken>()), 
                Times.Never);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDryRun_WritesFile()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
        await File.WriteAllTextAsync(testFile, "test content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> 
            { 
                new WikiTiddler { Title = "Test", Content = "test" } 
            });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteAsync(testFile, dryRun: false);

            // Assert - WriteFileCommand SHOULD be called
            _mediatorMock.Verify(
                m => m.Send(It.Is<WriteFileCommand>(c => true), It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithDryRun_StillParsesAndConverts()
    {
        // Arrange
        var inputFolder = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}");
        var outputFolder = Path.Combine(Path.GetTempPath(), $"wiki_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(inputFolder);
        
        var testFile = Path.Combine(inputFolder, "test.md");
        await File.WriteAllTextAsync(testFile, "content");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiTiddler> 
            { 
                new WikiTiddler { Title = "Test", Content = "test" } 
            });
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("converted content");

        try
        {
            // Act
            await _job.ExecuteBatchAsync(inputFolder, outputFolder, dryRun: true);

            // Assert - ParseFileCommand and ConvertContentCommand SHOULD be called
            _mediatorMock.Verify(
                m => m.Send(It.IsAny<ParseFileCommand>(), It.IsAny<CancellationToken>()), 
                Times.Once);
            _mediatorMock.Verify(
                m => m.Send(It.IsAny<ConvertContentCommand>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(inputFolder))
                Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);
        }
    }
}
