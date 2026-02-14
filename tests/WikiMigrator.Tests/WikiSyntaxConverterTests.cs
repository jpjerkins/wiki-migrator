using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Tests;

public class WikiSyntaxConverterTests
{
    private readonly WikiSyntaxConverter _converter;

    public WikiSyntaxConverterTests()
    {
        _converter = new WikiSyntaxConverter();
    }

    [Fact]
    public async Task ConvertAsync_WithNullTiddler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _converter.ConvertAsync(null!));
    }

    [Fact]
    public async Task ConvertAsync_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsBoldText()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "This is ''bold'' text" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("This is **bold** text", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsItalicText()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "This is //italic// text" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("This is *italic* text", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsSingleHeading()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "!Heading 1" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("# Heading 1", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsDoubleHeading()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "!!Heading 2" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("## Heading 2", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsTripleHeading()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "!!!Heading 3" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("### Heading 3", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsUnorderedList()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "* Item 1\n* Item 2\n* Item 3" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("- Item 1\n- Item 2\n- Item 3", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsOrderedList()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "# Item 1\n# Item 2\n# Item 3" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("1. Item 1\n1. Item 2\n1. Item 3", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsCodeBlocks()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "Code: {{{some code}}}" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("Code: `some code`", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsTable()
    {
        // Arrange
        var tiddler = new WikiTiddler 
        { 
            Title = "Test", 
            Content = "|!Header1|!Header2|\n|cell1|cell2|" 
        };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Contains("| Header1 | Header2 |", result);
        Assert.Contains("| --- | --- |", result);
        Assert.Contains("| cell1 | cell2 |", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsBoldAndItalic()
    {
        // Arrange
        var tiddler = new WikiTiddler { Title = "Test", Content = "This is ''bold'' and //italic//" };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("This is **bold** and *italic*", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsComplexContent()
    {
        // Arrange
        var tiddler = new WikiTiddler 
        { 
            Title = "Test", 
            Content = @"!Title
This is ''bold'' and //italic// text.

* List item 1
* List item 2

# Ordered item 1
# Ordered item 2

Code: {{{hello world}}}
" 
        };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Contains("# Title", result);
        Assert.Contains("**bold**", result);
        Assert.Contains("*italic*", result);
        Assert.Contains("- List item 1", result);
        Assert.Contains("1. Ordered item 1", result);
        Assert.Contains("`hello world`", result);
    }

    [Fact]
    public async Task ConvertAsync_PreservesNonWikiSyntax()
    {
        // Arrange
        var tiddler = new WikiTiddler 
        { 
            Title = "Test", 
            Content = "Regular text without wiki syntax" 
        };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Equal("Regular text without wiki syntax", result);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsTableWithMultipleColumns()
    {
        // Arrange
        var tiddler = new WikiTiddler 
        { 
            Title = "Test", 
            Content = "|!Col1|!Col2|!Col3|\n|val1|val2|val3|" 
        };

        // Act
        var result = await _converter.ConvertAsync(tiddler);

        // Assert
        Assert.Contains("| Col1 | Col2 | Col3 |", result);
        Assert.Contains("| val1 | val2 | val3 |", result);
    }
}
