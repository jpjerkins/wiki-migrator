using WikiMigrator.Domain.Entities;
using WikiMigrator.Infrastructure.Parsers;

namespace WikiMigrator.Tests;

public class HtmlWikiParserTests
{
    private readonly HtmlWikiParser _parser;

    public HtmlWikiParserTests()
    {
        _parser = new HtmlWikiParser();
    }

    [Fact]
    public async Task ParseAsync_WithValidHtml_ReturnsWikiTiddler()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-tags=""tag1 tag2"" data-created=""20240101"" data-modified=""20240115"">
  <div class=""title"" id=""MyTitle"">My Title</div>
  <div class=""body"">
    Content here...
  </div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("My Title", tiddler.Title);
    }

    [Fact]
    public async Task ParseAsync_WithTitle_CapturesTitle()
    {
        // Arrange
        var input = @"<div class=""tiddler"">
  <div class=""title"">Test Title</div>
  <div class=""body"">Body content here</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("Test Title", tiddler.Title);
    }

    [Fact]
    public async Task ParseAsync_WithCreatedDate_ParsesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-created=""20240101"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(new DateTime(2024, 1, 1), tiddler.Created);
    }

    [Fact]
    public async Task ParseAsync_WithModifiedDate_ParsesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-modified=""20240115"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(new DateTime(2024, 1, 15), tiddler.Modified);
    }

    [Fact]
    public async Task ParseAsync_WithTags_ParsesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-tags=""tag1 tag2 tag3"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(3, tiddler.Metadata.Tags.Count);
        Assert.Contains("tag1", tiddler.Metadata.Tags);
        Assert.Contains("tag2", tiddler.Metadata.Tags);
        Assert.Contains("tag3", tiddler.Metadata.Tags);
    }

    [Fact]
    public async Task ParseAsync_WithBodyContent_CapturesContent()
    {
        // Arrange
        var input = @"<div class=""tiddler"">
  <div class=""title"">Test</div>
  <div class=""body"">
    This is the body content.
    Multiple lines here.
  </div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("This is the body content.\n    Multiple lines here.", tiddler.Content);
    }

    [Fact]
    public async Task ParseAsync_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WhitespaceOnly_ReturnsEmpty()
    {
        // Arrange
        var input = "   \n\t  \n  ";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithoutTitle_UsesIdAttribute()
    {
        // Arrange
        var input = @"<div class=""tiddler"" id=""MyTiddlerId"">
  <div class=""body"">Content</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("MyTiddlerId", tiddler.Title);
    }

    [Fact]
    public async Task ParseAsync_WithoutTitle_GeneratesTitle()
    {
        // Arrange
        var input = @"<div class=""tiddler"">
  <div class=""body"">Some content without title</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.False(string.IsNullOrEmpty(tiddler.Title));
    }

    [Fact]
    public async Task ParseAsync_WithMultipleTiddlers_ReturnsAll()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-created=""20240101"">
  <div class=""title"">First Tiddler</div>
  <div class=""body"">First content</div>
</div>
<div class=""tiddler"" data-created=""20240102"">
  <div class=""title"">Second Tiddler</div>
  <div class=""body"">Second content</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ParseAsync_DifferentDateFormats_HandlesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-created=""2024-01-01"" data-modified=""2024-01-15"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(new DateTime(2024, 1, 1), tiddler.Created);
        Assert.Equal(new DateTime(2024, 1, 15), tiddler.Modified);
    }

    [Fact]
    public async Task ParseAsync_NoTiddlerDivs_ReturnsEmpty()
    {
        // Arrange
        var input = @"<div class=""other"">No tiddler here</div>";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithTagsContainingCommas_ParsesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-tags=""tag1, tag2, tag3"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(3, tiddler.Metadata.Tags.Count);
    }

    [Fact]
    public async Task ParseAsync_WithoutBodyDiv_ContentIsEmpty()
    {
        // Arrange
        var input = @"<div class=""tiddler"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(string.Empty, tiddler.Content);
    }

    [Fact]
    public async Task ParseAsync_WithHtmlEntities_DecodesCorrectly()
    {
        // Arrange
        var input = @"<div class=""tiddler"">
  <div class=""title"">Test &amp; Title</div>
  <div class=""body"">Content with &lt;html&gt; entities</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("Test & Title", tiddler.Title);
        Assert.Equal("Content with <html> entities", tiddler.Content);
    }

    [Fact]
    public async Task ParseAsync_MetadataIsPopulated()
    {
        // Arrange
        var input = @"<div class=""tiddler"" data-tags=""tag1 tag2"" data-created=""20240101"" data-modified=""20240115"">
  <div class=""title"">Test</div>
</div>";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.NotNull(tiddler.Metadata);
        Assert.Equal(new DateTime(2024, 1, 1), tiddler.Metadata.Created);
        Assert.Equal(new DateTime(2024, 1, 15), tiddler.Metadata.Modified);
        Assert.Equal(2, tiddler.Metadata.Tags.Count);
    }
}
