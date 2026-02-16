using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class MarkdownWriterTests
{
    private readonly MarkdownWriter _markdownWriter;
    private readonly TagProcessor _tagProcessor;

    public MarkdownWriterTests()
    {
        _tagProcessor = new TagProcessor();
        _markdownWriter = new MarkdownWriter(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MarkdownWriter>.Instance,
            _tagProcessor);
    }

    #region SanitizeFilename Tests

    [Fact]
    public void SanitizeFilename_WithNormalTitle_ReturnsUnchanged()
    {
        // Arrange
        var title = "My Wiki Page";

        // Act
        var result = _markdownWriter.SanitizeFilename(title);

        // Assert
        Assert.Equal("My Wiki Page", result);
    }

    [Fact]
    public void SanitizeFilename_WithInvalidCharacters_ReplacesWithUnderscore()
    {
        // Arrange
        var title = "Page <>:|?* With Invalid";

        // Act
        var result = _markdownWriter.SanitizeFilename(title);

        // Assert
        Assert.DoesNotContain('<', result);
        Assert.DoesNotContain('>', result);
        Assert.DoesNotContain(':', result);
        Assert.DoesNotContain('|', result);
        Assert.DoesNotContain('?', result);
        Assert.DoesNotContain('*', result);
    }

    [Fact]
    public void SanitizeFilename_WithEmptyString_ReturnsUntitled()
    {
        // Act
        var result = _markdownWriter.SanitizeFilename("");

        // Assert
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void SanitizeFilename_WithWhitespace_ReturnsUntitled()
    {
        // Act
        var result = _markdownWriter.SanitizeFilename("   ");

        // Assert
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void SanitizeFilename_WithLeadingDots_TrimsThem()
    {
        // Act
        var result = _markdownWriter.SanitizeFilename("...My Page");

        // Assert
        Assert.Equal("My Page", result);
    }

    [Fact]
    public void SanitizeFilename_WithTrailingDots_TrimsThem()
    {
        // Act
        var result = _markdownWriter.SanitizeFilename("My Page...");

        // Assert
        Assert.Equal("My Page", result);
    }

    [Fact]
    public void SanitizeFilename_WithVeryLongTitle_TruncatesTo200()
    {
        // Arrange
        var longTitle = new string('a', 300);

        // Act
        var result = _markdownWriter.SanitizeFilename(longTitle);

        // Assert
        Assert.Equal(200, result.Length);
    }

    [Fact]
    public void SanitizeFilename_WithSlash_ReplacesWithUnderscore()
    {
        // Act
        var result = _markdownWriter.SanitizeFilename("parent/child");

        // Assert
        Assert.Equal("parent_child", result);
    }

    #endregion

    #region GenerateFrontmatter Tests

    [Fact]
    public void GenerateFrontmatter_WithBasicTiddler_GeneratesYamlFrontmatter()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Test Page",
            Created = new DateTime(2024, 1, 15),
            Modified = new DateTime(2024, 1, 20)
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert
        Assert.StartsWith("---", result);
        Assert.Contains("title: \"Test Page\"", result);
        Assert.Contains("created: 2024-01-15", result);
        Assert.Contains("modified: 2024-01-20", result);
        Assert.EndsWith("---\n\n", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithTags_IncludesTagsInFrontmatter()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Tagged Page",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "tag1, tag2" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert
        Assert.Contains("tags:", result);
        Assert.Contains("tag1", result);
        Assert.Contains("tag2", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithCustomFields_IncludesCustomFields()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Custom Page",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "author", Value = "John Doe" },
                new WikiField { Name = "status", Value = "published" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert
        Assert.Contains("author:", result);
        Assert.Contains("status:", result);
        Assert.Contains("John Doe", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithSpecialCharactersInTitle_EscapesThem()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Page \"With\" Quotes",
            Created = DateTime.Now,
            Modified = DateTime.Now
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert
        Assert.Contains("title: \"Page \\\"With\\\" Quotes\"", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithNewlinesInContent_EscapesThem()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Test",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "description", Value = "Line 1\nLine 2" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert
        Assert.Contains("Line 1\\nLine 2", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithOriginalTags_PreservesAllOriginalTags()
    {
        // Arrange - tiddler with multiple original tags including PARA and Task tags
        var tiddler = new WikiTiddler
        {
            Title = "Project Task Note",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, Task, work, important" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert - all original tags should be preserved in the frontmatter
        Assert.Contains("tags:", result);
        Assert.Contains("Project", result);
        Assert.Contains("Task", result);
        Assert.Contains("work", result);
        Assert.Contains("important", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithProjectAndTaskTags_PreservesBothInFrontmatter()
    {
        // Arrange - note with both Project and Task tags
        var tiddler = new WikiTiddler
        {
            Title = "Project with Tasks",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, Task" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert - both Project and Task should be in the tags
        Assert.Contains("tags:", result);
        Assert.Contains("Project", result);
        Assert.Contains("Task", result);
    }

    [Fact]
    public void GenerateFrontmatter_WithHierarchicalTags_PreservesOriginalAndExpandsHierarchy()
    {
        // Arrange - tag with hierarchy like "parent/child"
        var tiddler = new WikiTiddler
        {
            Title = "Hierarchical Tags",
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, work/subtask" }
            }
        };

        // Act
        var result = _markdownWriter.GenerateFrontmatter(tiddler);

        // Assert - original tags should be preserved
        Assert.Contains("tags:", result);
        Assert.Contains("Project", result);
        // The hierarchy expansion adds parent tags, but original should still be there
        Assert.Contains("work/subtask", result);
    }

    #endregion

    #region GenerateMarkdown Tests

    [Fact]
    public void GenerateMarkdown_WithTiddler_CombinesFrontmatterAndContent()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Test Page",
            Created = new DateTime(2024, 1, 15),
            Modified = new DateTime(2024, 1, 20)
        };
        var content = "# Hello World\n\nThis is the content.";

        // Act
        var result = _markdownWriter.GenerateMarkdown(tiddler, content);

        // Assert
        Assert.StartsWith("---", result);
        Assert.Contains("title: \"Test Page\"", result);
        Assert.Contains("# Hello World", result);
        Assert.Contains("This is the content.", result);
    }

    #endregion

    #region WriteMarkdownFileAsync Tests

    [Fact]
    public async Task WriteMarkdownFileAsync_WithValidPath_CreatesFile()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Test Page",
            Created = new DateTime(2024, 1, 15),
            Modified = new DateTime(2024, 1, 20)
        };
        var content = "# Hello World";
        var tempPath = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}", "Test Page.md");

        try
        {
            // Act
            var result = await _markdownWriter.WriteMarkdownFileAsync(tiddler, content, tempPath);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(tempPath));
            var fileContent = await File.ReadAllTextAsync(tempPath);
            Assert.Contains("title: \"Test Page\"", fileContent);
            Assert.Contains("# Hello World", fileContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            var dir = Path.GetDirectoryName(tempPath);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public async Task WriteMarkdownFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Test",
            Created = DateTime.Now,
            Modified = DateTime.Now
        };
        var content = "Content";
        var tempPath = Path.Combine(Path.GetTempPath(), $"wiki_test_{Guid.NewGuid()}", "subdir", "Test.md");

        try
        {
            // Act
            var result = await _markdownWriter.WriteMarkdownFileAsync(tiddler, content, tempPath);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(tempPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            var dir = Path.GetDirectoryName(tempPath);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    #endregion
}
