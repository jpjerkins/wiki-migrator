using WikiMigrator.Application.Services;

namespace WikiMigrator.Tests;

public class TagProcessorTests
{
    private readonly TagProcessor _tagProcessor;

    public TagProcessorTests()
    {
        _tagProcessor = new TagProcessor();
    }

    #region ParseTags Tests

    [Fact]
    public void ParseTags_WithNullString_ReturnsEmptyList()
    {
        // Act
        var result = _tagProcessor.ParseTags(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTags_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = _tagProcessor.ParseTags("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTags_WithWhitespace_ReturnsEmptyList()
    {
        // Act
        var result = _tagProcessor.ParseTags("   ");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTags_WithSingleTag_ReturnsSingleItemList()
    {
        // Act
        var result = _tagProcessor.ParseTags("tag1");

        // Assert
        Assert.Single(result);
        Assert.Equal("tag1", result[0]);
    }

    [Fact]
    public void ParseTags_WithMultipleCommaSeparatedTags_ReturnsAllTags()
    {
        // Act
        var result = _tagProcessor.ParseTags("tag1, tag2, tag3");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("tag1", result);
        Assert.Contains("tag2", result);
        Assert.Contains("tag3", result);
    }

    [Fact]
    public void ParseTags_WithExtraWhitespace_TrimsTags()
    {
        // Act
        var result = _tagProcessor.ParseTags("  tag1  ,   tag2   ,   tag3  ");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("tag1", result[0]);
        Assert.Equal("tag2", result[1]);
        Assert.Equal("tag3", result[2]);
    }

    [Fact]
    public void ParseTags_WithEmptyEntries_FiltersThemOut()
    {
        // Act
        var result = _tagProcessor.ParseTags("tag1, , tag2, , tag3");

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ParseTags_WithHierarchicalTags_PreservesSlash()
    {
        // Act
        var result = _tagProcessor.ParseTags("parent/child, sibling/child");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("parent/child", result[0]);
        Assert.Equal("sibling/child", result[1]);
    }

    #endregion

    #region GenerateYamlTags Tests

    [Fact]
    public void GenerateYamlTags_WithNullTags_ReturnsEmptyArray()
    {
        // Act
        var result = _tagProcessor.GenerateYamlTags(null!);

        // Assert
        Assert.Equal("tags: []", result);
    }

    [Fact]
    public void GenerateYamlTags_WithEmptyList_ReturnsEmptyArray()
    {
        // Act
        var result = _tagProcessor.GenerateYamlTags(new List<string>());

        // Assert
        Assert.Equal("tags: []", result);
    }

    [Fact]
    public void GenerateYamlTags_WithSingleTag_ReturnsInlineArray()
    {
        // Act
        var result = _tagProcessor.GenerateYamlTags(new List<string> { "tag1" });

        // Assert
        Assert.Equal("tags: [tag1]", result);
    }

    [Fact]
    public void GenerateYamlTags_WithMultipleTags_ReturnsMultilineArray()
    {
        // Act
        var result = _tagProcessor.GenerateYamlTags(new List<string> { "tag1", "tag2", "tag3" });

        // Assert
        Assert.Contains("tags:", result);
        Assert.Contains("  - tag1", result);
        Assert.Contains("  - tag2", result);
        Assert.Contains("  - tag3", result);
    }

    [Fact]
    public void GenerateYamlTags_WithHierarchicalTags_PreservesSlashes()
    {
        // Act
        var result = _tagProcessor.GenerateYamlTags(new List<string> { "parent/child", "parent/other" });

        // Assert
        Assert.Contains("  - parent/child", result);
        Assert.Contains("  - parent/other", result);
    }

    #endregion

    #region ProcessTags Tests

    [Fact]
    public void ProcessTags_WithNullInput_ReturnsEmptyArray()
    {
        // Act
        var result = _tagProcessor.ProcessTags(null!);

        // Assert
        Assert.Equal("tags: []", result);
    }

    [Fact]
    public void ProcessTags_WithEmptyInput_ReturnsEmptyArray()
    {
        // Act
        var result = _tagProcessor.ProcessTags("");

        // Assert
        Assert.Equal("tags: []", result);
    }

    [Fact]
    public void ProcessTags_WithSingleTag_ReturnsYamlWithTag()
    {
        // Act
        var result = _tagProcessor.ProcessTags("tag1");

        // Assert
        Assert.Equal("tags: [tag1]", result);
    }

    [Fact]
    public void ProcessTags_WithMultipleTags_ReturnsMultilineYaml()
    {
        // Act
        var result = _tagProcessor.ProcessTags("tag1, tag2, tag3");

        // Assert
        Assert.Contains("tags:", result);
        Assert.Contains("  - tag1", result);
        Assert.Contains("  - tag2", result);
        Assert.Contains("  - tag3", result);
    }

    [Fact]
    public void ProcessTags_WithHierarchicalTag_ExpandsToIncludeParent()
    {
        // Act
        var result = _tagProcessor.ProcessTags("parent/child");

        // Assert
        Assert.Contains("  - parent/child", result);
        Assert.Contains("  - parent", result);
    }

    [Fact]
    public void ProcessTags_WithDeepHierarchicalTag_ExpandsAllParents()
    {
        // Act
        var result = _tagProcessor.ProcessTags("grandparent/parent/child");

        // Assert
        Assert.Contains("  - grandparent/parent/child", result);
        Assert.Contains("  - grandparent/parent", result);
        Assert.Contains("  - grandparent", result);
    }

    [Fact]
    public void ProcessTags_WithMixedTags_HandlesBothHierarchicalAndSimple()
    {
        // Act
        var result = _tagProcessor.ProcessTags("simple, parent/child");

        // Assert
        Assert.Contains("  - simple", result);
        Assert.Contains("  - parent/child", result);
        Assert.Contains("  - parent", result);
    }

    [Fact]
    public void ProcessTags_WithDuplicateParentTags_DoesNotDuplicate()
    {
        // Act
        var result = _tagProcessor.ProcessTags("parent/child1, parent/child2");

        // Assert
        var parentCount = result.Split('\n').Count(line => line.Trim() == "- parent");
        Assert.Equal(1, parentCount);
    }

    #endregion
}
