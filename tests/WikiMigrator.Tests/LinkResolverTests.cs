using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class LinkResolverTests
{
    private readonly LinkResolver _resolver;

    public LinkResolverTests()
    {
        _resolver = new LinkResolver();
    }

    [Fact]
    public void RegisterLink_WithValidInputs_RegistersSuccessfully()
    {
        // Act
        _resolver.RegisterLink("Test Page", "test-page");

        // Assert
        Assert.Equal("test-page", _resolver.GetSanitizedFilename("Test Page"));
    }

    [Fact]
    public void RegisterLink_WithNullTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.RegisterLink(null!, "test"));
    }

    [Fact]
    public void RegisterLink_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.RegisterLink("", "test"));
    }

    [Fact]
    public void RegisterLink_WithNullFilename_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.RegisterLink("Test", null!));
    }

    [Fact]
    public void RegisterTiddlers_RegistersAllTiddlers()
    {
        // Arrange
        var tiddlers = new List<WikiTiddler>
        {
            new() { Title = "Page One", Content = "Content 1" },
            new() { Title = "Page Two", Content = "Content 2" },
            new() { Title = "Page Three", Content = "Content 3" }
        };

        // Act
        _resolver.RegisterTiddlers(tiddlers);

        // Assert
        Assert.True(_resolver.HasTitle("Page One"));
        Assert.True(_resolver.HasTitle("Page Two"));
        Assert.True(_resolver.HasTitle("Page Three"));
        Assert.Equal(3, _resolver.GetLinkMap().Count);
    }

    [Fact]
    public void ExtractLinks_WithSimpleLink_ReturnsLink()
    {
        // Arrange
        var content = "This is a [[link]] to a page";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Single(links);
        Assert.Equal("link", links[0]);
    }

    [Fact]
    public void ExtractLinks_WithLinkAndText_ReturnsLinkTarget()
    {
        // Arrange
        var content = "This is a [[target|display text]] link";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Single(links);
        Assert.Equal("target", links[0]);
    }

    [Fact]
    public void ExtractLinks_WithMultipleLinks_ReturnsAllLinks()
    {
        // Arrange
        var content = "Links to [[Page One]] and [[Page Two]] and [[Page Three]]";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Equal(3, links.Count);
        Assert.Contains("Page One", links);
        Assert.Contains("Page Two", links);
        Assert.Contains("Page Three", links);
    }

    [Fact]
    public void ExtractLinks_WithNoLinks_ReturnsEmpty()
    {
        // Arrange
        var content = "This is plain text without links";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Empty(links);
    }

    [Fact]
    public void ExtractLinks_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = "";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Empty(links);
    }

    [Fact]
    public void ExtractLinks_WithNullContent_ReturnsEmpty()
    {
        // Arrange
        string? content = null;

        // Act
        var links = LinkResolver.ExtractLinks(content!).ToList();

        // Assert
        Assert.Empty(links);
    }

    [Fact]
    public void ResolveLinks_WithSimpleLink_ConvertsToMarkdown()
    {
        // Arrange
        _resolver.RegisterLink("Test Page", "test-page");
        var content = "Link to [[Test Page]]";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("Link to [Test Page](test-page.md)", result);
    }

    [Fact]
    public void ResolveLinks_WithLinkText_UsesLinkText()
    {
        // Arrange
        _resolver.RegisterLink("Target Page", "target-page");
        var content = "Click [[Target Page|here]] to go";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("Click [here](target-page.md) to go", result);
    }

    [Fact]
    public void ResolveLinks_WithUnknownLink_UsesSanitizedTarget()
    {
        // Arrange
        var content = "Link to [[Unknown Page]]";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("Link to [Unknown Page](unknown-page.md)", result);
    }

    [Fact]
    public void ResolveLinks_WithMultipleLinks_ResolvesAll()
    {
        // Arrange
        _resolver.RegisterLink("Page One", "page-one");
        _resolver.RegisterLink("Page Two", "page-two");
        var content = "See [[Page One]] and [[Page Two]]";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("See [Page One](page-one.md) and [Page Two](page-two.md)", result);
    }

    [Fact]
    public void ResolveLinks_WithNoLinks_ReturnsOriginalContent()
    {
        // Arrange
        var content = "Plain text without links";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("Plain text without links", result);
    }

    [Fact]
    public void ResolveLinks_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = "";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeFilename_WithValidTitle_ReturnsSanitized()
    {
        // Act
        var result = LinkResolver.SanitizeFilename("My Test Page");

        // Assert
        Assert.Equal("my-test-page", result);
    }

    [Fact]
    public void SanitizeFilename_WithSpecialCharacters_RemovesThem()
    {
        // Act
        var result = LinkResolver.SanitizeFilename("Page: Test (1)");

        // Assert
        Assert.Equal("page-test-1", result);
    }

    [Fact]
    public void SanitizeFilename_WithEmptyString_ReturnsUntitled()
    {
        // Act
        var result = LinkResolver.SanitizeFilename("   ");

        // Assert
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GetSanitizedFilename_WithRegisteredTitle_ReturnsFilename()
    {
        // Arrange
        _resolver.RegisterLink("Test Page", "test-page");

        // Act
        var result = _resolver.GetSanitizedFilename("Test Page");

        // Assert
        Assert.Equal("test-page", result);
    }

    [Fact]
    public void GetSanitizedFilename_WithUnregisteredTitle_ReturnsNull()
    {
        // Act
        var result = _resolver.GetSanitizedFilename("Non Existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void HasTitle_WithRegisteredTitle_ReturnsTrue()
    {
        // Arrange
        _resolver.RegisterLink("Test Page", "test-page");

        // Act
        var result = _resolver.HasTitle("Test Page");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasTitle_WithUnregisteredTitle_ReturnsFalse()
    {
        // Act
        var result = _resolver.HasTitle("Non Existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_RemovesAllLinks()
    {
        // Arrange
        _resolver.RegisterLink("Page One", "page-one");
        _resolver.RegisterLink("Page Two", "page-two");

        // Act
        _resolver.Clear();

        // Assert
        Assert.Empty(_resolver.GetLinkMap());
    }

    [Fact]
    public void ExtractLinks_WithDuplicateLinks_ReturnsDistinct()
    {
        // Arrange
        var content = "Links to [[Page]] and [[Page]] again";

        // Act
        var links = LinkResolver.ExtractLinks(content).ToList();

        // Assert
        Assert.Single(links);
    }

    [Fact]
    public void ResolveLinks_WithLinksContainingPipes_HandlesCorrectly()
    {
        // Arrange
        _resolver.RegisterLink("Test", "test");
        var content = "[[Test|text with | pipe]]";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("[text with | pipe](test.md)", result);
    }

    [Fact]
    public void ResolveLinks_WithCaseInsensitiveTitle_ResolvesCorrectly()
    {
        // Arrange
        _resolver.RegisterLink("Test Page", "test-page");
        var content = "Link to [[test page]]";

        // Act
        var result = _resolver.ResolveLinks(content);

        // Assert
        Assert.Equal("Link to [test page](test-page.md)", result);
    }

    [Fact]
    public void ResolveLinks_WithTracking_TracksBrokenLinks()
    {
        // Arrange
        _resolver.RegisterLink("Page A", "page-a");
        var content = "Links to [[Page A]] and [[Missing Page]]";

        // Act
        var result = _resolver.ResolveLinks(content, "Page B", trackBrokenLinks: true);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Equal("Links to [Page A](page-a.md) and [Missing Page](missing-page.md)", result);
        Assert.Single(brokenLinks);
        Assert.Equal("Missing Page", brokenLinks[0].LinkTarget);
        Assert.Equal("Page B", brokenLinks[0].SourceTitle);
    }

    [Fact]
    public void ResolveLinks_WithNoTracking_DoesNotTrackBrokenLinks()
    {
        // Arrange
        var content = "Link to [[Missing Page]]";

        // Act
        _resolver.ResolveLinks(content, "Page A", trackBrokenLinks: false);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Empty(brokenLinks);
    }

    [Fact]
    public void GetBrokenLinksForSource_ReturnsOnlyLinksFromSource()
    {
        // Arrange
        var content1 = "Links to [[Missing One]] and [[Missing Two]]";
        var content2 = "Link to [[Missing Three]]";
        
        _resolver.ResolveLinks(content1, "Source A", trackBrokenLinks: true);
        _resolver.ResolveLinks(content2, "Source B", trackBrokenLinks: true);

        // Act
        var brokenLinksForA = _resolver.GetBrokenLinksForSource("Source A");
        var brokenLinksForB = _resolver.GetBrokenLinksForSource("Source B");

        // Assert
        Assert.Equal(2, brokenLinksForA.Count);
        Assert.Single(brokenLinksForB);
    }

    [Fact]
    public void ClearBrokenLinks_RemovesTrackedLinks()
    {
        // Arrange
        _resolver.ResolveLinks("Link to [[Missing]]", "Page A", trackBrokenLinks: true);
        Assert.NotEmpty(_resolver.GetBrokenLinks());

        // Act
        _resolver.ClearBrokenLinks();

        // Assert
        Assert.Empty(_resolver.GetBrokenLinks());
    }

    [Fact]
    public void Clear_AlsoClearsBrokenLinks()
    {
        // Arrange
        _resolver.RegisterLink("Page A", "page-a");
        _resolver.ResolveLinks("Link to [[Missing]]", "Page A", trackBrokenLinks: true);
        Assert.NotEmpty(_resolver.GetBrokenLinks());

        // Act
        _resolver.Clear();

        // Assert
        Assert.Empty(_resolver.GetBrokenLinks());
        Assert.Empty(_resolver.GetLinkMap());
    }

    [Fact]
    public void ResolveLinks_WithMultipleBrokenLinks_TracksAll()
    {
        // Arrange
        var content = "[[Missing One]] and [[Missing Two]] and [[Missing Three]]";

        // Act
        _resolver.ResolveLinks(content, "Source", trackBrokenLinks: true);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Equal(3, brokenLinks.Count);
    }

    [Fact]
    public void ResolveLinks_WithValidLinks_DoesNotTrackThemAsBroken()
    {
        // Arrange
        _resolver.RegisterLink("Valid Page", "valid-page");
        var content = "Link to [[Valid Page]]";

        // Act
        _resolver.ResolveLinks(content, "Source", trackBrokenLinks: true);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Empty(brokenLinks);
    }

    // Task 1.3: Obsidian Link Conversion
    [Fact]
    public void ResolveLinksToObsidian_WithSimpleLink_ConvertsToObsidianFormat()
    {
        // Arrange
        _resolver.RegisterLink("My Tiddler", "my-tiddler");
        var content = "Link to [[My Tiddler]]";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("Link to [[my-tiddler|My Tiddler]]", result);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithLinkText_PreservesOriginalText()
    {
        // Arrange
        _resolver.RegisterLink("Target Page", "target-page");
        var content = "Click [[Target Page|here]] to go";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("Click [[target-page|here]] to go", result);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithUnknownLink_UsesSanitizedTarget()
    {
        // Arrange
        var content = "Link to [[Unknown Page]]";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("Link to [[unknown-page|Unknown Page]]", result);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithMultipleLinks_ResolvesAll()
    {
        // Arrange
        _resolver.RegisterLink("Page One", "page-one");
        _resolver.RegisterLink("Page Two", "page-two");
        var content = "See [[Page One]] and [[Page Two]]";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("See [[page-one|Page One]] and [[page-two|Page Two]]", result);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithNoLinks_ReturnsOriginalContent()
    {
        // Arrange
        var content = "Plain text without links";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("Plain text without links", result);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithCaseInsensitiveTitle_ResolvesCorrectly()
    {
        // Arrange
        _resolver.RegisterLink("Test Page", "test-page");
        var content = "Link to [[test page]]";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert
        Assert.Equal("Link to [[test-page|test page]]", result);
    }

    // Task 1.4: Broken Link Detection
    [Fact]
    public void ResolveLinks_TracksBrokenLinks_WhenTrackingEnabled()
    {
        // Arrange
        _resolver.RegisterLink("Valid Page", "valid-page");
        var content = "Links to [[Valid Page]] and [[Missing Page]]";

        // Act
        var result = _resolver.ResolveLinks(content, "Source Page", trackBrokenLinks: true);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Single(brokenLinks);
        Assert.Equal("Missing Page", brokenLinks[0].LinkTarget);
        Assert.Equal("Source Page", brokenLinks[0].SourceTitle);
    }

    [Fact]
    public void ResolveLinks_DoesNotTrackBrokenLinks_WhenTrackingDisabled()
    {
        // Arrange
        var content = "Links to [[Missing Page]]";

        // Act
        _resolver.ResolveLinks(content, "Source Page", trackBrokenLinks: false);
        var brokenLinks = _resolver.GetBrokenLinks();

        // Assert
        Assert.Empty(brokenLinks);
    }

    [Fact]
    public void ResolveLinksToObsidian_WithBrokenLink_UsesFallbackSanitized()
    {
        // Arrange - no links registered
        var content = "Link to [[Unknown Page]]";

        // Act
        var result = _resolver.ResolveLinksToObsidian(content);

        // Assert - should still convert to Obsidian format with sanitized fallback
        Assert.Equal("Link to [[unknown-page|Unknown Page]]", result);
    }

    [Fact]
    public void GetBrokenLinksForSource_ReturnsOnlyLinksFromSpecifiedSource()
    {
        // Arrange
        _resolver.ResolveLinks("[[Missing1]]", "PageA", trackBrokenLinks: true);
        _resolver.ResolveLinks("[[Missing2]]", "PageB", trackBrokenLinks: true);

        // Act
        var brokenLinksA = _resolver.GetBrokenLinksForSource("PageA");
        var brokenLinksB = _resolver.GetBrokenLinksForSource("PageB");

        // Assert
        Assert.Single(brokenLinksA);
        Assert.Equal("Missing1", brokenLinksA[0].LinkTarget);
        Assert.Single(brokenLinksB);
        Assert.Equal("Missing2", brokenLinksB[0].LinkTarget);
    }

    // Task 3.1-3.2: Backlinks
    [Fact]
    public void LinkGraph_GetBacklinks_ReturnsIncomingLinks()
    {
        // Arrange
        var graph = new LinkGraph();
        graph.AddLink("Page A", "Page B");
        graph.AddLink("Page C", "Page B");

        // Act
        var backlinks = graph.GetBacklinks("Page B");

        // Assert
        Assert.Equal(2, backlinks.Count);
        Assert.Contains(backlinks, b => b.Equals("Page A", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(backlinks, b => b.Equals("Page C", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LinkGraph_GetOrphanedNodes_ReturnsTiddlersWithNoIncomingLinks()
    {
        // Arrange
        var graph = new LinkGraph();
        graph.AddLink("Page A", "Page B");
        graph.AddLink("Page C", "Page B");
        // Page D has no incoming links

        // Act
        var orphans = graph.GetOrphanedNodes();

        // Assert
        // Note: nodes are only added when they appear as source or target
        Assert.Contains(orphans, o => o.Equals("Page A", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(orphans, o => o.Equals("Page C", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LinkGraphBuilder_BuildsGraphFromTiddlers()
    {
        // Arrange
        var tiddlers = new List<WikiTiddler>
        {
            new() { Title = "Page A", Content = "See [[Page B]] and [[Page C]]" },
            new() { Title = "Page B", Content = "Link to [[Page C]]" },
            new() { Title = "Page C", Content = "No links" }
        };

        // Act
        var builder = new LinkGraphBuilder();
        var graph = builder.Build(tiddlers);

        // Assert
        var backlinksB = graph.GetBacklinks("Page B");
        Assert.Single(backlinksB);
        Assert.Contains("Page A", backlinksB);

        var backlinksC = graph.GetBacklinks("Page C");
        Assert.Equal(2, backlinksC.Count);
    }
}
