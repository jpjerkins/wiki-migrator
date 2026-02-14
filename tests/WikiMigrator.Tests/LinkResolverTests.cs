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
}
