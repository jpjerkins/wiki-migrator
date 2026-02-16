using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class BacklinkGeneratorTests
{
    private readonly LinkGraph _graph;
    private readonly BacklinkGenerator _generator;

    public BacklinkGeneratorTests()
    {
        _graph = new LinkGraph();
        _generator = new BacklinkGenerator(_graph);
    }

    private void BuildGraph()
    {
        // Build a sample graph:
        // Page A -> Page B
        // Page A -> Page C
        // Page B -> Page C
        // Page C -> Page A
        // Page D has no links
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page A", "Page C");
        _graph.AddLink("Page B", "Page C");
        _graph.AddLink("Page C", "Page A");
    }

    [Fact]
    public void GetBacklinksFor_WithBacklinks_ReturnsBacklinks()
    {
        // Arrange
        BuildGraph();

        // Act
        var backlinks = _generator.GetBacklinksFor("Page C");

        // Assert
        Assert.Equal(2, backlinks.Count);
        Assert.Contains("Page A", backlinks);
        Assert.Contains("Page B", backlinks);
    }

    [Fact]
    public void GetBacklinksFor_WithNoBacklinks_ReturnsEmpty()
    {
        // Arrange
        BuildGraph();

        // Act
        var backlinks = _generator.GetBacklinksFor("Page D");

        // Assert
        Assert.Empty(backlinks);
    }

    [Fact]
    public void GetBacklinksFor_WithNonExistentNode_ReturnsEmpty()
    {
        // Act
        var backlinks = _generator.GetBacklinksFor("NonExistent");

        // Assert
        Assert.Empty(backlinks);
    }

    [Fact]
    public void GenerateAllBacklinks_ReturnsAllBacklinks()
    {
        // Arrange
        BuildGraph();

        // Act
        var allBacklinks = _generator.GenerateAllBacklinks();

        // Assert
        Assert.NotEmpty(allBacklinks);
        Assert.True(allBacklinks.ContainsKey("Page C"));
        Assert.True(allBacklinks.ContainsKey("Page A"));
    }

    [Fact]
    public void GenerateAllBacklinks_ExcludesNodesWithoutBacklinks()
    {
        // Arrange
        // Page X -> Page Y, so Page Y has a backlink but Page X doesn't
        _graph.AddLink("Page X", "Page Y");

        // Act
        var allBacklinks = _generator.GenerateAllBacklinks();

        // Assert - only Page Y has backlinks
        Assert.Single(allBacklinks);
        Assert.True(allBacklinks.ContainsKey("Page Y"));
    }

    [Fact]
    public void GetOrphanedTiddlers_ReturnsNodesWithNoIncomingLinks()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        // Page A has outgoing but no incoming - it's an orphan

        // Act
        var orphans = _generator.GetOrphanedTiddlers();

        // Assert
        Assert.Contains("Page A", orphans);
    }

    [Fact]
    public void GenerateBacklinksYaml_WithBacklinks_GeneratesYaml()
    {
        // Arrange
        BuildGraph();

        // Act
        var yaml = _generator.GenerateBacklinksYaml("Page C");

        // Assert
        Assert.NotEmpty(yaml);
        Assert.Contains("backlinks:", yaml);
        Assert.Contains("[[", yaml);
    }

    [Fact]
    public void GenerateBacklinksYaml_WithNoBacklinks_ReturnsEmpty()
    {
        // Arrange
        BuildGraph();

        // Act
        var yaml = _generator.GenerateBacklinksYaml("Page D");

        // Assert
        Assert.Empty(yaml);
    }

    [Fact]
    public void GenerateBacklinksYaml_FormatsCorrectly()
    {
        // Arrange
        _graph.AddLink("Source 1", "Target");
        _graph.AddLink("Source 2", "Target");

        // Act
        var yaml = _generator.GenerateBacklinksYaml("Target");

        // Assert
        Assert.Contains("---", yaml);
        Assert.Contains("backlinks:", yaml);
        Assert.Contains("[[", yaml);
        Assert.Contains("Source 1", yaml);
        Assert.Contains("Source 2", yaml);
    }

    [Fact]
    public void Backlinks_AreCaseInsensitive()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");

        // Act
        var backlinks = _generator.GetBacklinksFor("PAGE B");

        // Assert
        Assert.Single(backlinks);
        Assert.Contains("Page A", backlinks);
    }
}
