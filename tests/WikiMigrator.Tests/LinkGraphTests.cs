using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class LinkGraphTests
{
    private readonly LinkGraph _graph;

    public LinkGraphTests()
    {
        _graph = new LinkGraph();
    }

    [Fact]
    public void AddLink_WithValidLink_AddsToGraph()
    {
        // Act
        _graph.AddLink("Page A", "Page B");

        // Assert
        Assert.True(_graph.HasLink("Page A", "Page B"));
        Assert.Equal(2, _graph.NodeCount);
        Assert.Equal(1, _graph.EdgeCount);
    }

    [Fact]
    public void AddLink_WithNullSource_Ignores()
    {
        // Act
        _graph.AddLink(null!, "Page B");

        // Assert
        Assert.Equal(0, _graph.NodeCount);
    }

    [Fact]
    public void AddLink_WithEmptyTarget_Ignores()
    {
        // Act
        _graph.AddLink("Page A", "");

        // Assert
        Assert.Equal(0, _graph.NodeCount);
    }

    [Fact]
    public void AddLinks_AddsMultipleLinks()
    {
        // Act
        _graph.AddLinks("Page A", new[] { "Page B", "Page C", "Page D" });

        // Assert
        Assert.Equal(4, _graph.NodeCount);
        Assert.Equal(3, _graph.EdgeCount);
        Assert.True(_graph.HasLink("Page A", "Page B"));
        Assert.True(_graph.HasLink("Page A", "Page C"));
        Assert.True(_graph.HasLink("Page A", "Page D"));
    }

    [Fact]
    public void GetOutgoingLinks_ReturnsLinkedTargets()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page A", "Page C");

        // Act
        var outgoing = _graph.GetOutgoingLinks("Page A");

        // Assert
        Assert.Equal(2, outgoing.Count);
        Assert.Contains("Page B", outgoing);
        Assert.Contains("Page C", outgoing);
    }

    [Fact]
    public void GetOutgoingLinks_WithNoLinks_ReturnsEmptySet()
    {
        // Act
        var outgoing = _graph.GetOutgoingLinks("Page A");

        // Assert
        Assert.Empty(outgoing);
    }

    [Fact]
    public void GetBacklinks_ReturnsSources()
    {
        // Arrange
        _graph.AddLink("Page A", "Page C");
        _graph.AddLink("Page B", "Page C");

        // Act
        var backlinks = _graph.GetBacklinks("Page C");

        // Assert
        Assert.Equal(2, backlinks.Count);
        Assert.Contains("Page A", backlinks);
        Assert.Contains("Page B", backlinks);
    }

    [Fact]
    public void GetBacklinks_WithNoBacklinks_ReturnsEmptySet()
    {
        // Act
        var backlinks = _graph.GetBacklinks("Page X");

        // Assert
        Assert.Empty(backlinks);
    }

    [Fact]
    public void HasNode_WithExistingNode_ReturnsTrue()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");

        // Act & Assert
        Assert.True(_graph.HasNode("Page A"));
        Assert.True(_graph.HasNode("Page B"));
    }

    [Fact]
    public void HasNode_WithNonExistingNode_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_graph.HasNode("NonExistent"));
    }

    [Fact]
    public void HasLink_WithExistingLink_ReturnsTrue()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");

        // Act & Assert
        Assert.True(_graph.HasLink("Page A", "Page B"));
    }

    [Fact]
    public void HasLink_WithNonExistingLink_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_graph.HasLink("Page A", "Page B"));
    }

    [Fact]
    public void GetAllNodes_ReturnsAllNodes()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page B", "Page C");

        // Act
        var nodes = _graph.GetAllNodes().ToList();

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.Contains("Page A", nodes);
        Assert.Contains("Page B", nodes);
        Assert.Contains("Page C", nodes);
    }

    [Fact]
    public void GetOrphanedNodes_WithNoIncomingLinks_ReturnsOrphans()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page A", "Page C");
        // Page D has no links at all
        _graph.AddLink("Page D", "Page E");

        // Act
        var orphans = _graph.GetOrphanedNodes();

        // Assert
        Assert.Contains("Page D", orphans);
        Assert.Contains("Page A", orphans); // A has outgoing but no incoming
    }

    [Fact]
    public void GetOrphanedNodes_WithNoOrphans_ReturnsEmpty()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page B", "Page A");

        // Act
        var orphans = _graph.GetOrphanedNodes();

        // Assert
        Assert.Empty(orphans);
    }

    [Fact]
    public void DetectCycles_WithNoCycles_ReturnsEmpty()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page B", "Page C");

        // Act
        var cycles = _graph.DetectCycles();

        // Assert
        Assert.Empty(cycles);
    }

    [Fact]
    public void DetectCycles_WithSimpleCycle_ReturnsCycle()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page B", "Page C");
        _graph.AddLink("Page C", "Page A");

        // Act
        var cycles = _graph.DetectCycles();

        // Assert
        Assert.NotEmpty(cycles);
    }

    [Fact]
    public void DetectCycles_WithSelfLoop_ReturnsCycle()
    {
        // Arrange
        _graph.AddLink("Page A", "Page A");

        // Act
        var cycles = _graph.DetectCycles();

        // Assert
        Assert.NotEmpty(cycles);
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");
        _graph.AddLink("Page B", "Page C");

        // Act
        _graph.Clear();

        // Assert
        Assert.Equal(0, _graph.NodeCount);
        Assert.Equal(0, _graph.EdgeCount);
    }

    [Fact]
    public void CaseInsensitive_TitleMatches()
    {
        // Arrange
        _graph.AddLink("Page A", "Page B");

        // Act & Assert
        Assert.True(_graph.HasLink("page a", "page b"));
        Assert.True(_graph.HasNode("PAGE A"));
        Assert.Contains("page b", _graph.GetOutgoingLinks("PAGE A"));
        Assert.Contains("PAGE A", _graph.GetBacklinks("PAGE B"));
    }
}

public class LinkGraphBuilderTests
{
    [Fact]
    public void Build_FromTiddlers_CreatesGraph()
    {
        // Arrange
        var tiddlers = new List<WikiTiddler>
        {
            new() { Title = "Page A", Content = "Link to [[Page B]] and [[Page C]]" },
            new() { Title = "Page B", Content = "Link to [[Page C]]" },
            new() { Title = "Page C", Content = "No links here" }
        };

        var builder = new LinkGraphBuilder();

        // Act
        var graph = builder.Build(tiddlers);

        // Assert
        Assert.Equal(3, graph.NodeCount);
        Assert.True(graph.HasLink("Page A", "Page B"));
        Assert.True(graph.HasLink("Page A", "Page C"));
        Assert.True(graph.HasLink("Page B", "Page C"));
    }

    [Fact]
    public void Build_WithEmptyTiddlers_ReturnsEmptyGraph()
    {
        // Arrange
        var builder = new LinkGraphBuilder();

        // Act
        var graph = builder.Build(new List<WikiTiddler>());

        // Assert
        Assert.Equal(0, graph.NodeCount);
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void Build_ExtractsBacklinks()
    {
        // Arrange
        var tiddlers = new List<WikiTiddler>
        {
            new() { Title = "Page A", Content = "Link to [[Page B]]" },
            new() { Title = "Page B", Content = "Link to [[Page B]] (self)" }
        };

        var builder = new LinkGraphBuilder();

        // Act
        var graph = builder.Build(tiddlers);

        // Assert
        var backlinksToB = graph.GetBacklinks("Page B");
        Assert.Equal(2, backlinksToB.Count);
        Assert.Contains("Page A", backlinksToB);
        Assert.Contains("Page B", backlinksToB);
    }
}
