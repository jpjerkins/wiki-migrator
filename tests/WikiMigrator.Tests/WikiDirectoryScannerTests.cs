using WikiMigrator.Domain.Scanning;

namespace WikiMigrator.Tests;

public class WikiDirectoryScannerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly WikiDirectoryScanner _scanner;

    public WikiDirectoryScannerTests()
    {
        _scanner = new WikiDirectoryScanner();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WikiScannerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Scan_WithEmptyDirectory_ReturnsEmptyResult()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var result = _scanner.Scan(emptyDir);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Scan_WithNonexistentDirectory_ReturnsEmptyResult()
    {
        // Act
        var result = _scanner.Scan("/nonexistent/path");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Scan_WithTidFiles_DiscoversTidFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test1.tid"), "title: Test\ncreated: 20240101\n\nContent");
        File.WriteAllText(Path.Combine(_testDirectory, "test2.tid"), "title: Test2\ncreated: 20240102\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.TidFileCount);
        Assert.Equal(0, result.HtmlFileCount);
    }

    [Fact]
    public void Scan_WithHtmlFiles_DiscoversHtmlFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test1.html"), "<div class='tiddler'>Test</div>");
        File.WriteAllText(Path.Combine(_testDirectory, "test2.html"), "<div class='tiddler'>Test2</div>");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(0, result.TidFileCount);
        Assert.Equal(2, result.HtmlFileCount);
    }

    [Fact]
    public void Scan_WithMixedFiles_DiscoversAllFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.tid"), "title: Test\ncreated: 20240101\n\nContent");
        File.WriteAllText(Path.Combine(_testDirectory, "test.html"), "<div class='tiddler'>Test</div>");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.TidFileCount);
        Assert.Equal(1, result.HtmlFileCount);
    }

    [Fact]
    public void Scan_WithNestedDirectories_DiscoversFilesRecursively()
    {
        // Arrange
        var subDir1 = Path.Combine(_testDirectory, "sub1");
        var subDir2 = Path.Combine(_testDirectory, "sub1", "sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        File.WriteAllText(Path.Combine(_testDirectory, "root.tid"), "title: Root\ncreated: 20240101\n\nContent");
        File.WriteAllText(Path.Combine(subDir1, "sub1.tid"), "title: Sub1\ncreated: 20240102\n\nContent");
        File.WriteAllText(Path.Combine(subDir2, "sub2.tid"), "title: Sub2\ncreated: 20240103\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(3, result.TotalFiles);
    }

    [Fact]
    public void Scan_IgnoresNonWikiFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.txt"), "text file");
        File.WriteAllText(Path.Combine(_testDirectory, "test.md"), "markdown file");
        File.WriteAllText(Path.Combine(_testDirectory, "test.tid"), "title: Test\ncreated: 20240101\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(1, result.TotalFiles);
    }

    [Fact]
    public void Scan_PopulatesFileInfo_Correctly()
    {
        // Arrange
        var fileName = "test.tid";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, "title: Test\ncreated: 20240101\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);
        var file = result.Files.First();

        // Assert
        Assert.Equal(filePath, file.FullPath);
        Assert.Equal(fileName, file.FileName);
        Assert.Equal(WikiFileType.Tid, file.FileType);
        Assert.True(file.Size > 0);
    }

    [Fact]
    public void Scan_ExtractsTiddlerMetadata_FromTidFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.tid"), "title: Test\ncreated: 202401011200\nmodified: 202401021300\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);
        var file = result.Files.First();

        // Assert
        Assert.NotNull(file.TiddlerCreated);
        Assert.NotNull(file.TiddlerModified);
    }

    [Fact]
    public void Scan_SortsByModificationDate()
    {
        // Arrange
        var oldFile = Path.Combine(_testDirectory, "old.tid");
        var newFile = Path.Combine(_testDirectory, "new.tid");
        
        File.WriteAllText(oldFile, "title: Old\ncreated: 20240101\n\nContent");
        File.WriteAllText(newFile, "title: New\ncreated: 20240102\n\nContent");
        
        // Set different file modification times
        File.SetLastWriteTime(oldFile, new DateTime(2024, 1, 1));
        File.SetLastWriteTime(newFile, new DateTime(2024, 1, 2));

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal("old.tid", result.Files[0].FileName);
        Assert.Equal("new.tid", result.Files[1].FileName);
    }

    [Fact]
    public void Scan_ComputesRelativePaths()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.tid"), "title: Test\ncreated: 20240101\n\nContent");
        
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.tid"), "title: Nested\ncreated: 20240101\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        var rootFile = result.Files.First(f => f.FileName == "test.tid");
        var nestedFile = result.Files.First(f => f.FileName == "nested.tid");
        
        Assert.Equal("test.tid", rootFile.RelativePath);
        Assert.Equal(Path.Combine("subdir", "nested.tid"), nestedFile.RelativePath);
    }

    [Fact]
    public void GetProcessingOrder_ReturnsFilesSortedByDate()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.tid");
        var file2 = Path.Combine(_testDirectory, "file2.tid");
        var file3 = Path.Combine(_testDirectory, "file3.tid");
        
        File.WriteAllText(file1, "title: File1\ncreated: 20240103\n\nContent");
        File.WriteAllText(file2, "title: File2\ncreated: 20240101\n\nContent");
        File.WriteAllText(file3, "title: File3\ncreated: 20240102\n\nContent");

        // Act
        var result = _scanner.Scan(_testDirectory);
        var processingOrder = _scanner.GetProcessingOrder(result).ToList();

        // Assert
        Assert.Equal(3, processingOrder.Count);
        Assert.Equal("file2.tid", processingOrder[0].FileName);
        Assert.Equal("file3.tid", processingOrder[1].FileName);
        Assert.Equal("file1.tid", processingOrder[2].FileName);
    }

    [Fact]
    public void Scan_WithUppercaseExtensions_DiscoversFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.TID"), "title: Test\ncreated: 20240101\n\nContent");
        File.WriteAllText(Path.Combine(_testDirectory, "test.HTML"), "<div class='tiddler'>Test</div>");

        // Act
        var result = _scanner.Scan(_testDirectory);

        // Assert
        Assert.Equal(2, result.TotalFiles);
    }

    [Fact]
    public void Scan_WithEmptyPath_ReturnsEmptyResult()
    {
        // Act
        var result = _scanner.Scan(string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Scan_WithWhitespacePath_ReturnsEmptyResult()
    {
        // Act
        var result = _scanner.Scan("   ");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
    }
}
