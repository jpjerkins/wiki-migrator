using Microsoft.Extensions.Logging;
using Moq;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;
using WikiMigrator.Domain.Scanning;

namespace WikiMigrator.Tests;

public class LoggingTests
{
    [Fact]
    public void MigrationPipeline_LogsDebugLevel_ForSuccessfulOperations()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MigrationPipeline>>();
        var parserFactoryMock = new Mock<IParserFactory>();
        var converterMock = new Mock<IConverter>();
        
        var scanner = new WikiDirectoryScanner();
        
        // Create test logger to capture logs
        var testLogger = new TestLogger<MigrationPipeline>();
        
        var pipeline = new MigrationPipeline(
            parserFactoryMock.Object,
            converterMock.Object,
            scanner,
            testLogger);

        // Setup mocks for empty scan result
        parserFactoryMock
            .Setup(p => p.GetParser(It.IsAny<string>()))
            .Returns((IParser?)null);

        converterMock
            .Setup(c => c.ConvertAsync(It.IsAny<WikiTiddler>()))
            .Returns(Task.FromResult("# Test Content"));

        // Act - run with empty directory
        var result = pipeline.RunAsync(
            "/nonexistent/path",
            "/tmp/output",
            CancellationToken.None).Result;

        // Assert - verify logs were captured
        var logMessages = testLogger.LogMessages;
        
        // Should have Information level logs for starting migration
        Assert.Contains(logMessages, m => m.LogLevel == LogLevel.Information && m.Message.Contains("Starting migration"));
    }

    [Fact]
    public void MigrationPipeline_LogsWarningLevel_WhenNoFilesFound()
    {
        // Arrange
        var testLogger = new TestLogger<MigrationPipeline>();
        var scanner = new WikiDirectoryScanner();
        
        var pipeline = new MigrationPipeline(
            Mock.Of<IParserFactory>(),
            Mock.Of<IConverter>(),
            scanner,
            testLogger);

        // Act
        var result = pipeline.RunAsync(
            "/nonexistent/path",
            "/tmp/output",
            CancellationToken.None).Result;

        // Assert
        var logMessages = testLogger.LogMessages;
        Assert.Contains(logMessages, m => 
            m.LogLevel == LogLevel.Warning && 
            m.Message.Contains("No files found"));
    }

    [Fact]
    public void MigrationPipeline_LogsErrorLevel_WhenFileProcessingFails()
    {
        // Arrange
        var testLogger = new TestLogger<MigrationPipeline>();
        var scanner = new WikiDirectoryScanner();
        var parserFactoryMock = new Mock<IParserFactory>();
        var converterMock = new Mock<IConverter>();
        
        // Setup parser that returns a valid tiddler
        var parserMock = new Mock<IParser>();
        var tiddler = new WikiTiddler
        {
            Title = "TestTiddler",
            Content = "Test content"
        };
        parserMock
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<IEnumerable<WikiTiddler>>(new[] { tiddler }));
        
        parserFactoryMock
            .Setup(f => f.GetParser(It.IsAny<string>()))
            .Returns(parserMock.Object);

        // Setup converter to throw
        converterMock
            .Setup(c => c.ConvertAsync(It.IsAny<WikiTiddler>()))
            .Throws(new InvalidOperationException("Conversion failed"));

        // Create temp directory with a .tid file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "TestTiddler.tid"), "title: TestTiddler\n\nTest content");

        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var pipeline = new MigrationPipeline(
            parserFactoryMock.Object,
            converterMock.Object,
            scanner,
            testLogger);

        try
        {
            // Act
            var result = pipeline.RunAsync(
                tempDir,
                outputDir,
                CancellationToken.None).Result;

            // Assert
            var logMessages = testLogger.LogMessages;
            
            // Should have error log with tiddler name and reason
            Assert.Contains(logMessages, m => 
                m.LogLevel == LogLevel.Error && 
                m.Message.Contains("TestTiddler") &&
                m.Message.Contains("Conversion failed"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void MigrationPipeline_LogsInformationLevel_WhenTiddlerParsed()
    {
        // Arrange
        var testLogger = new TestLogger<MigrationPipeline>();
        var scanner = new WikiDirectoryScanner();
        var parserFactoryMock = new Mock<IParserFactory>();
        var converterMock = new Mock<IConverter>();
        
        // Setup parser that returns a valid tiddler
        var parserMock = new Mock<IParser>();
        var tiddler = new WikiTiddler
        {
            Title = "MyTestTiddler",
            Content = "Test content"
        };
        parserMock
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<IEnumerable<WikiTiddler>>(new[] { tiddler }));
        
        parserFactoryMock
            .Setup(f => f.GetParser(It.IsAny<string>()))
            .Returns(parserMock.Object);

        converterMock
            .Setup(c => c.ConvertAsync(It.IsAny<WikiTiddler>()))
            .Returns(Task.FromResult("# Test Content"));

        // Create temp directory with a .tid file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "MyTestTiddler.tid"), "title: MyTestTiddler\n\nTest content");

        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var pipeline = new MigrationPipeline(
            parserFactoryMock.Object,
            converterMock.Object,
            scanner,
            testLogger);

        try
        {
            // Act
            var result = pipeline.RunAsync(
                tempDir,
                outputDir,
                CancellationToken.None).Result;

            // Assert
            var logMessages = testLogger.LogMessages;
            
            // Should have information log about tiddler being parsed
            Assert.Contains(logMessages, m => 
                m.LogLevel == LogLevel.Information && 
                m.Message.Contains("Tiddler parsed") &&
                m.Message.Contains("MyTestTiddler"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void MigrationPipeline_LogsInformationLevel_WhenTiddlerWritten()
    {
        // Arrange
        var testLogger = new TestLogger<MigrationPipeline>();
        var scanner = new WikiDirectoryScanner();
        var parserFactoryMock = new Mock<IParserFactory>();
        var converterMock = new Mock<IConverter>();
        
        // Setup parser that returns a valid tiddler
        var parserMock = new Mock<IParser>();
        var tiddler = new WikiTiddler
        {
            Title = "WriteTestTiddler",
            Content = "Test content"
        };
        parserMock
            .Setup(p => p.ParseAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<IEnumerable<WikiTiddler>>(new[] { tiddler }));
        
        parserFactoryMock
            .Setup(f => f.GetParser(It.IsAny<string>()))
            .Returns(parserMock.Object);

        converterMock
            .Setup(c => c.ConvertAsync(It.IsAny<WikiTiddler>()))
            .Returns(Task.FromResult("# Converted Content"));

        // Create temp directory with a .tid file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "WriteTestTiddler.tid"), "title: WriteTestTiddler\n\nTest content");

        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var pipeline = new MigrationPipeline(
            parserFactoryMock.Object,
            converterMock.Object,
            scanner,
            testLogger);

        try
        {
            // Act
            var result = pipeline.RunAsync(
                tempDir,
                outputDir,
                CancellationToken.None).Result;

            // Assert
            var logMessages = testLogger.LogMessages;
            
            // Should have information log about tiddler being written
            Assert.Contains(logMessages, m => 
                m.LogLevel == LogLevel.Information && 
                m.Message.Contains("Tiddler written") &&
                m.Message.Contains("WriteTestTiddler"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }
}

/// <summary>
/// Test logger that captures log messages for testing
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<LogMessage> LogMessages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        LogMessages.Add(new LogMessage
        {
            LogLevel = logLevel,
            Message = message,
            Exception = exception
        });
    }
}

public class LogMessage
{
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
