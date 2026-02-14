using WikiMigrator.Application.Interfaces;
using WikiMigrator.Infrastructure.Parsers;

namespace WikiMigrator.Tests;

public class ParserFactoryTests
{
    private readonly ParserFactory _factory;

    public ParserFactoryTests()
    {
        _factory = new ParserFactory();
    }

    [Fact]
    public void GetParser_WithTidExtension_ReturnsTidFileParser()
    {
        // Arrange
        var filePath = "test.tid";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.NotNull(parser);
        Assert.IsType<TidFileParser>(parser);
    }

    [Fact]
    public void GetParser_WithHtmlExtension_ReturnsHtmlWikiParser()
    {
        // Arrange
        var filePath = "test.html";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.NotNull(parser);
        Assert.IsType<HtmlWikiParser>(parser);
    }

    [Fact]
    public void GetParser_WithUppercaseExtension_ReturnsCorrectParser()
    {
        // Arrange
        var tidFilePath = "test.TID";
        var htmlFilePath = "test.HTML";

        // Act
        var tidParser = _factory.GetParser(tidFilePath);
        var htmlParser = _factory.GetParser(htmlFilePath);

        // Assert
        Assert.NotNull(tidParser);
        Assert.IsType<TidFileParser>(tidParser);
        Assert.NotNull(htmlParser);
        Assert.IsType<HtmlWikiParser>(htmlParser);
    }

    [Fact]
    public void GetParser_WithPathContainingExtension_ReturnsCorrectParser()
    {
        // Arrange
        var filePath = "/path/to/myfile.tid";
        var htmlPath = "/path/to/document.html";

        // Act
        var parser = _factory.GetParser(filePath);
        var htmlParser = _factory.GetParser(htmlPath);

        // Assert
        Assert.NotNull(parser);
        Assert.IsType<TidFileParser>(parser);
        Assert.NotNull(htmlParser);
        Assert.IsType<HtmlWikiParser>(htmlParser);
    }

    [Fact]
    public void GetParser_WithUnknownExtension_ReturnsNull()
    {
        // Arrange
        var filePath = "test.txt";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithEmptyPath_ReturnsNull()
    {
        // Arrange
        var filePath = "";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithWhitespace_ReturnsNull()
    {
        // Arrange
        var filePath = "   ";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithNoExtension_ReturnsNull()
    {
        // Arrange
        var filePath = "filename";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_ReturnsNewInstanceEachTime()
    {
        // Arrange
        var filePath = "test.tid";

        // Act
        var parser1 = _factory.GetParser(filePath);
        var parser2 = _factory.GetParser(filePath);

        // Assert
        Assert.NotSame(parser1, parser2);
    }

    [Fact]
    public void GetParser_WithTidExtension_ParserCanParse()
    {
        // Arrange
        var filePath = "test.tid";
        var input = @"title: Test Tiddler
created: 20240101

Test content";

        // Act
        var parser = _factory.GetParser(filePath);
        var result = parser!.ParseAsync(input).Result;
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("Test Tiddler", tiddler.Title);
    }

    [Fact]
    public void GetParser_WithHtmlExtension_ParserCanParse()
    {
        // Arrange
        var filePath = "test.html";
        var input = @"<div class=""tiddler"" data-created=""20240101"">
  <div class=""title"">HTML Tiddler</div>
  <div class=""body"">HTML content</div>
</div>";

        // Act
        var parser = _factory.GetParser(filePath);
        var result = parser!.ParseAsync(input).Result;
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("HTML Tiddler", tiddler.Title);
    }
}
