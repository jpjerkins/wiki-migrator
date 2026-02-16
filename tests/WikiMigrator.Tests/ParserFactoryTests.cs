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
    public void GetParser_WithHtmlExtension_ReturnsTiddlyWiki5JsonParser()
    {
        // Arrange
        var filePath = "test.html";

        // Act
        var parser = _factory.GetParser(filePath);

        // Assert
        Assert.NotNull(parser);
        // Default HTML parser is now TiddlyWiki5JsonParser
        Assert.IsType<TiddlyWiki5JsonParser>(parser);
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
        // Default HTML parser is now TiddlyWiki5JsonParser
        Assert.IsType<TiddlyWiki5JsonParser>(htmlParser);
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
        // Default HTML parser is now TiddlyWiki5JsonParser
        Assert.IsType<TiddlyWiki5JsonParser>(htmlParser);
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
    public void GetParser_WithLegacyHtmlContent_UsesLegacyParser()
    {
        // This test verifies that old-style HTML format can still be parsed
        // by using the legacy parser directly
        // Arrange - old-style HTML format (no JSON)
        var input = @"<div class=""tiddler"" data-created=""20240101"">
  <div class=""title"">HTML Tiddler</div>
  <div class=""body"">HTML content</div>
</div>";

        // Act - use legacy parser directly
        var parser = new HtmlWikiParser();
        var result = parser.ParseAsync(input).Result;
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("HTML Tiddler", tiddler.Title);
    }

    [Fact]
    public void TiddlyWiki5JsonParser_CanParseJsonFormat()
    {
        // This test verifies the new TiddlyWiki 5.x JSON format parser works
        // Arrange - TiddlyWiki 5.x JSON format
        var input = @"<!DOCTYPE html>
<html>
<head></head>
<body>
<script type=""application/json"" id=""tiddlers"">
{
    ""tiddlers"": {
        ""JSON Tiddler"": {
            ""title"": ""JSON Tiddler"",
            ""text"": ""JSON content"",
            ""tags"": [""tag1"", ""tag2""],
            ""created"": ""20240101120000"",
            ""modified"": ""20240115150000""
        }
    }
}
</script>
</body>
</html>";

        // Act
        var parser = new TiddlyWiki5JsonParser();
        var result = parser.ParseAsync(input).Result;
        var tiddler = result.FirstOrDefault();

        // Assert
        Assert.NotNull(tiddler);
        Assert.Equal("JSON Tiddler", tiddler.Title);
        Assert.Equal("JSON content", tiddler.Content);
    }

    // Additional tests for better branch coverage
    
    [Fact]
    public void GetParser_WithMultipleDotsInFilename_UsesLastExtension()
    {
        // Arrange
        var filePath = "file.name.tid";
        
        // Act
        var parser = _factory.GetParser(filePath);
        
        // Assert
        Assert.IsType<TidFileParser>(parser);
    }

    [Fact]
    public void GetParser_WithHtmlExtensionCaseInsensitive()
    {
        // Arrange - various case combinations for .html
        var paths = new[] { "test.html", "test.HTML", "test.HtMl", "test.html" };
        
        foreach (var path in paths)
        {
            var parser = _factory.GetParser(path);
            Assert.IsType<TiddlyWiki5JsonParser>(parser);
        }
    }

    [Fact]
    public void GetParser_WithMixedCaseExtension_MatchesCorrectly()
    {
        // Arrange
        var paths = new[] { "test.Tid", "test.TID", "test.Html", "test.HTML" };
        
        // Act & Assert
        Assert.IsType<TidFileParser>(_factory.GetParser("test.Tid"));
        Assert.IsType<TidFileParser>(_factory.GetParser("test.TID"));
        Assert.IsType<TiddlyWiki5JsonParser>(_factory.GetParser("test.Html"));
        Assert.IsType<TiddlyWiki5JsonParser>(_factory.GetParser("test.HTML"));
    }
}
