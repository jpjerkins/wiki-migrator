using WikiMigrator.Application.Services;

namespace WikiMigrator.Tests;

public class ImageReferenceParserTests
{
    [Fact]
    public void ExtractImages_WithMarkdownImages_ReturnsReferences()
    {
        // Arrange
        var content = "Here is an image: ![Alt text](image.png)";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("image.png", images[0].OriginalPath);
        Assert.Equal("Alt text", images[0].AltText);
        Assert.Equal(ImageReference.ImageReferenceType.Markdown, images[0].Type);
    }

    [Fact]
    public void ExtractImages_WithMultipleMarkdownImages_ReturnsAll()
    {
        // Arrange
        var content = "![Image1](img1.png) and ![Image2](img2.jpg)";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Equal(2, images.Count);
    }

    [Fact]
    public void ExtractImages_WithHtmlImages_ReturnsReferences()
    {
        // Arrange
        var content = "<img src=\"test.png\" alt=\"Test Image\">";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("test.png", images[0].OriginalPath);
        Assert.Equal("Test Image", images[0].AltText);
        Assert.Equal(ImageReference.ImageReferenceType.Html, images[0].Type);
    }

    [Fact]
    public void ExtractImages_WithHtmlImagesAltBeforeSrc_ReturnsAlt()
    {
        // Arrange
        var content = "<img alt=\"Alt Text\" src=\"test.png\">";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("test.png", images[0].OriginalPath);
        Assert.Equal("Alt Text", images[0].AltText);
    }

    [Fact]
    public void ExtractImages_WithWikiImages_ReturnsReferences()
    {
        // Arrange
        var content = "{{myimage.png}}";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("myimage.png", images[0].OriginalPath);
        Assert.Equal(ImageReference.ImageReferenceType.Wiki, images[0].Type);
    }

    [Fact]
    public void ExtractImages_WithWikiImagesAndAlt_ReturnsAlt()
    {
        // Arrange
        var content = "{{myimage.png||This is alt}}";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("myimage.png", images[0].OriginalPath);
        Assert.Equal("This is alt", images[0].AltText);
    }

    [Fact]
    public void ExtractImages_WithNonImageWikiTransclusion_Ignores()
    {
        // Arrange
        var content = "{{SomeTiddler}}";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Empty(images);
    }

    [Fact]
    public void ExtractImages_WithMixedImages_ReturnsAll()
    {
        // Arrange
        var content = "Markdown: ![alt](img.png) HTML: <img src=\"html.png\"> Wiki: {{wiki.png}}";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Equal(3, images.Count);
        Assert.Contains(images, i => i.Type == ImageReference.ImageReferenceType.Markdown);
        Assert.Contains(images, i => i.Type == ImageReference.ImageReferenceType.Html);
        Assert.Contains(images, i => i.Type == ImageReference.ImageReferenceType.Wiki);
    }

    [Fact]
    public void ExtractImages_WithNoImages_ReturnsEmpty()
    {
        // Arrange
        var content = "Just plain text with [[links]] and no images";

        // Act
        var images = ImageReferenceParser.ExtractImages(content).ToList();

        // Assert
        Assert.Empty(images);
    }

    [Fact]
    public void ExtractImages_WithEmptyContent_ReturnsEmpty()
    {
        // Act
        var images = ImageReferenceParser.ExtractImages("").ToList();

        // Assert
        Assert.Empty(images);
    }
}

public class AttachmentCopierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _outputDir;
    private readonly AttachmentCopier _copier;

    public AttachmentCopierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"wiki_migrator_test_{Guid.NewGuid()}");
        _outputDir = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(_outputDir);
        _copier = new AttachmentCopier(_outputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void CopyAttachment_WithExistingFile_CopiesToOutput()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        var testFile = Path.Combine(sourceDir, "test.png");
        File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });

        // Act
        var result = _copier.CopyAttachment("test.png", sourceDir);

        // Assert
        Assert.True(File.Exists(result));
        Assert.StartsWith(_outputDir, result);
    }

    [Fact]
    public void CopyAttachment_WithSubfolder_CreatesSubfolder()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        var testFile = Path.Combine(sourceDir, "test.png");
        File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });

        // Act
        var result = _copier.CopyAttachment("test.png", sourceDir);

        // Assert
        Assert.Contains("attachments", result);
    }

    [Fact]
    public void CopyAttachment_WithRelativePath_ResolvesCorrectly()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        var imagesDir = Path.Combine(sourceDir, "images");
        Directory.CreateDirectory(imagesDir);
        var testFile = Path.Combine(imagesDir, "test.png");
        File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });

        // Act
        var result = _copier.CopyAttachment("images/test.png", sourceDir);

        // Assert
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void CopyAttachment_WithDuplicateFile_AddsHash()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        
        // Create two identical files
        var file1 = Path.Combine(sourceDir, "test.png");
        var file2 = Path.Combine(sourceDir, "sub", "test.png");
        Directory.CreateDirectory(Path.GetDirectoryName(file2)!);
        File.WriteAllBytes(file1, new byte[] { 1, 2, 3, 4 });
        File.WriteAllBytes(file2, new byte[] { 1, 2, 3, 4 });

        // Act
        var result1 = _copier.CopyAttachment("test.png", sourceDir);
        var result2 = _copier.CopyAttachment("sub/test.png", sourceDir);

        // Assert
        Assert.NotEqual(result1, result2);
        Assert.True(File.Exists(result1));
        Assert.True(File.Exists(result2));
    }

    [Fact]
    public void CopyAttachment_WithNonExistentFile_ReturnsExpectedPath()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);

        // Act
        var result = _copier.CopyAttachment("missing.png", sourceDir);

        // Assert
        Assert.Contains("missing.png", result);
    }

    [Fact]
    public void GetCopiedFiles_ReturnsAllCopied()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        
        var file1 = Path.Combine(sourceDir, "a.png");
        var file2 = Path.Combine(sourceDir, "b.png");
        File.WriteAllBytes(file1, new byte[] { 1 });
        File.WriteAllBytes(file2, new byte[] { 2 });

        // Act
        _copier.CopyAttachment("a.png", sourceDir);
        _copiedFiles = _copier.GetCopiedFiles();

        // Assert
        Assert.Equal(1, _copiedFiles.Count);
    }

    private IReadOnlyDictionary<string, string> _copiedFiles = null!;

    [Fact]
    public void RewriteImagePaths_WithMarkdownImage_RewritesPath()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        var testFile = Path.Combine(sourceDir, "test.png");
        File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });

        var content = "![My Image](test.png)";

        // Act
        var result = _copier.RewriteImagePaths(content, sourceDir);

        // Assert
        Assert.Contains("attachments/test.png", result);
    }

    [Fact]
    public void RewriteImagePaths_WithHtmlImage_RewritesPath()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        var testFile = Path.Combine(sourceDir, "test.png");
        File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4 });

        var content = "<img src=\"test.png\" alt=\"Alt\">";

        // Act
        var result = _copier.RewriteImagePaths(content, sourceDir);

        // Assert
        Assert.Contains("attachments/test.png", result);
    }

    [Fact]
    public void RewriteImagePaths_WithNoImages_ReturnsOriginal()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "wiki");
        Directory.CreateDirectory(sourceDir);
        var content = "No images here";

        // Act
        var result = _copier.RewriteImagePaths(content, sourceDir);

        // Assert
        Assert.Equal(content, result);
    }
}
