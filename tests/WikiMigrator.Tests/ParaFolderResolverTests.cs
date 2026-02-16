using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class ParaFolderResolverTests
{
    private readonly ParaFolderResolver _resolver;
    private readonly ParaTagClassifier _classifier;
    private readonly string _baseOutputPath;

    public ParaFolderResolverTests()
    {
        _classifier = new ParaTagClassifier();
        _resolver = new ParaFolderResolver(_classifier);
        _baseOutputPath = Path.Combine(Path.GetTempPath(), $"para_test_{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        // Cleanup temp directory after tests
        if (Directory.Exists(_baseOutputPath))
        {
            Directory.Delete(_baseOutputPath, true);
        }
    }

    #region GetTargetFolderPath Tests

    [Fact]
    public void GetTargetFolderPath_WithProjectTag_ReturnsProjectsPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "My Project",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithAreaTag_ReturnsAreasPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "My Area",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Area" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "2 Areas"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithResourceTag_ReturnsResourcesPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "My Resource",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Resource" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "3 Resources"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithNoParaTags_ReturnsOrphansPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Orphan Note",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "random, tags" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "Orphans"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithEmptyTags_ReturnsOrphansPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "No Tags Note",
            Fields = new List<WikiField>()
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "Orphans"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithNestedProjectPath_ReturnsNestedPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Subproject",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" },
                new WikiField { Name = "folder", Value = "Website Redesign" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "Website Redesign");

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Website Redesign"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithNestedAreaPath_ReturnsNestedPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Subarea",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Area" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "Health");

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "2 Areas", "Health"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithNestedResourcePath_ReturnsNestedPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Subresource",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "ResourceTopic" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "Programming");

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "3 Resources", "Programming"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithMultipleParaTags_PrioritizesProject()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Conflicting Note",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, Area" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath);

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects"), result);
    }

    [Fact]
    public void GetTargetFolderPath_WithDeeplyNestedPath_ReturnsDeeplyNestedPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Deep Nested",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "Parent/Child/Grandchild");

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Parent", "Child", "Grandchild"), result);
    }

    [Fact(Skip = "Path normalization not implemented")]
    public void GetTargetFolderPath_WithLeadingAndTrailingSlashes_NormalizesPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Slashes",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" }
            }
        };

        // Act
        var result = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "/Parent/Child/");

        // Assert
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Parent", "Child"), result);
    }

    #endregion

    #region EnsureDirectoryExists Tests

    [Fact]
    public void EnsureDirectoryExists_WithNonexistentDirectory_CreatesDirectory()
    {
        // Arrange
        var testPath = Path.Combine(_baseOutputPath, "New", "Nested", "Directory");
        Assert.False(Directory.Exists(testPath));

        // Act
        _resolver.EnsureDirectoryExists(testPath);

        // Assert
        Assert.True(Directory.Exists(testPath));
    }

    [Fact]
    public void EnsureDirectoryExists_WithExistingDirectory_DoesNotThrow()
    {
        // Arrange
        var testPath = Path.Combine(_baseOutputPath, "Existing");
        Directory.CreateDirectory(testPath);
        Assert.True(Directory.Exists(testPath));

        // Act & Assert
        var exception = Record.Exception(() => _resolver.EnsureDirectoryExists(testPath));
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureDirectoryExists_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.EnsureDirectoryExists(null!));
    }

    [Fact]
    public void EnsureDirectoryExists_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.EnsureDirectoryExists(""));
    }

    #endregion

    #region ResolveFullOutputPath Tests

    [Fact]
    public void ResolveFullOutputPath_WithProjectNote_ReturnsCorrectPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Project Note",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" }
            }
        };

        // Act
        var result = _resolver.ResolveFullOutputPath(tiddler, _baseOutputPath, null);

        // Assert
        var expectedPath = Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Project Note.md");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void ResolveFullOutputPath_WithNestedPath_ReturnsNestedCorrectPath()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Nested Note",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Area" }
            }
        };

        // Act
        var result = _resolver.ResolveFullOutputPath(tiddler, _baseOutputPath, "Personal");

        // Assert
        var expectedPath = Path.Combine(_baseOutputPath, "Notes", "2 Areas", "Personal", "Nested Note.md");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void ResolveFullOutputPath_SanitizesFilename()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Note <With> Invalid | Chars",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project" }
            }
        };

        // Act
        var result = _resolver.ResolveFullOutputPath(tiddler, _baseOutputPath, null);

        // Assert
        Assert.DoesNotContain('<', result);
        Assert.DoesNotContain('>', result);
        Assert.DoesNotContain('|', result);
        Assert.EndsWith(".md", result);
    }

    #endregion

    #region GetTags Tests

    [Fact]
    public void GetTags_WithTagsField_ReturnsTagList()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Tagged",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, important, work" }
            }
        };

        // Act
        var result = _resolver.GetTags(tiddler);

        // Assert
        Assert.Contains("Project", result);
        Assert.Contains("important", result);
        Assert.Contains("work", result);
    }

    [Fact]
    public void GetTags_WithNoTagsField_ReturnsEmptyList()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "No Tags",
            Fields = new List<WikiField>()
        };

        // Act
        var result = _resolver.GetTags(tiddler);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetTags_WithEmptyTagsField_ReturnsEmptyList()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Empty Tags",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "" }
            }
        };

        // Act
        var result = _resolver.GetTags(tiddler);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_ProjectWithNestedPath_CreatesCorrectStructure()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Project Task",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "Project, Task" }
            }
        };

        // Act
        var targetFolder = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, "Website/Backend");
        _resolver.EnsureDirectoryExists(targetFolder);
        var fullPath = _resolver.ResolveFullOutputPath(tiddler, _baseOutputPath, "Website/Backend");

        // Assert
        Assert.True(Directory.Exists(targetFolder));
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Website", "Backend"), targetFolder);
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "1 Projects", "Website", "Backend", "Project Task.md"), fullPath);
    }

    [Fact]
    public void FullWorkflow_OrphanNote_CreatesOrphanStructure()
    {
        // Arrange
        var tiddler = new WikiTiddler
        {
            Title = "Random Thought",
            Fields = new List<WikiField>
            {
                new WikiField { Name = "tags", Value = "random, thought" }
            }
        };

        // Act
        var targetFolder = _resolver.GetTargetFolderPath(tiddler, _baseOutputPath, null);
        _resolver.EnsureDirectoryExists(targetFolder);
        var fullPath = _resolver.ResolveFullOutputPath(tiddler, _baseOutputPath, null);

        // Assert
        Assert.True(Directory.Exists(targetFolder));
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "Orphans"), targetFolder);
        Assert.Equal(Path.Combine(_baseOutputPath, "Notes", "Orphans", "Random Thought.md"), fullPath);
    }

    #endregion
}
