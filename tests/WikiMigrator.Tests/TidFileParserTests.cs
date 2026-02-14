using WikiMigrator.Domain.Entities;
using WikiMigrator.Infrastructure.Parsers;

namespace WikiMigrator.Tests;

public class TidFileParserTests
{
    private readonly TidFileParser _parser;

    public TidFileParserTests()
    {
        _parser = new TidFileParser();
    }

    [Fact]
    public async Task ParseAsync_WithValidTid_ReturnsWikiTiddler()
    {
        // Arrange
        var input = @"title: My Tiddler
created: 20240101
modified: 20240115
tags: tag1, tag2

This is the body content.";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("My Tiddler", tiddler.Title);
    }

    [Fact]
    public async Task ParseAsync_WithTitle_CapturesTitle()
    {
        // Arrange
        var input = @"title: Test Title
created: 20240101

Body content here";

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
        var input = @"title: Test
created: 20240101
modified: 20240115";

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
        var input = @"title: Test
created: 20240101
modified: 20240115";

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
        var input = @"title: Test
tags: tag1, tag2, tag3";

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
        var input = @"title: Test
created: 20240101

This is the body content.
Multiple lines here.";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("This is the body content.\nMultiple lines here.", tiddler.Content);
    }

    [Fact]
    public async Task ParseAsync_WithCustomFields_ParsesCorrectly()
    {
        // Arrange
        var input = @"title: Test
custom-field: custom value
another-field: another value";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(2, tiddler.Fields.Count);
        Assert.Contains(tiddler.Fields, f => f.Name == "custom-field" && f.Value == "custom value");
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
    public async Task ParseAsync_WithoutTitle_GeneratesTitle()
    {
        // Arrange
        var input = @"created: 20240101

Some content without title";

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
        var input = @"title: First Tiddler
created: 20240101

First content


title: Second Tiddler
created: 20240102

Second content";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ParseAsync_DifferentDateFormats_HandlesCorrectly()
    {
        // Arrange
        var input = @"title: Test
created: 2024-01-01
modified: 2024-01-15";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(new DateTime(2024, 1, 1), tiddler.Created);
        Assert.Equal(new DateTime(2024, 1, 15), tiddler.Modified);
    }

    [Fact]
    public async Task ParseAsync_WithFieldDelimiter_ExtractsBodyCorrectly()
    {
        // Arrange
        var input = @"title: Test
created: 20240101
modifier: admin
==
This is the body after ==";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal("This is the body after ==", tiddler.Content);
    }

    [Fact]
    public async Task ParseAsync_TagsWithSemicolons_ParsesCorrectly()
    {
        // Arrange
        var input = @"title: Test
tags: tag1; tag2; tag3";

        // Act
        var result = await _parser.ParseAsync(input);
        var tiddler = result.First();

        // Assert
        Assert.Equal(3, tiddler.Metadata.Tags.Count);
    }
}
