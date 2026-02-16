using WikiMigrator.Application.Services;

namespace WikiMigrator.Tests;

public class AdvancedWikiParserTests
{
    // Task 4.1: Macro Handling
    [Fact]
    public void ExtractMacroDefinitions_WithSimpleMacro_ReturnsDefinition()
    {
        // Arrange
        var content = "\\define greeting() Hello World";

        // Act
        var macros = AdvancedWikiParser.ExtractMacroDefinitions(content).ToList();

        // Assert
        Assert.Single(macros);
        Assert.Equal("greeting", macros[0].Name);
        Assert.Empty(macros[0].Parameters);
        Assert.Equal("Hello World", macros[0].Body);
    }

    [Fact]
    public void ExtractMacroDefinitions_WithParameters_ReturnsParameters()
    {
        // Arrange
        var content = "\\define greet(name) Hello $1";

        // Act
        var macros = AdvancedWikiParser.ExtractMacroDefinitions(content).ToList();

        // Assert
        Assert.Single(macros);
        Assert.Equal("greet", macros[0].Name);
        Assert.Single(macros[0].Parameters);
        Assert.Equal("name", macros[0].Parameters[0]);
    }

    [Fact]
    public void ExtractMacroDefinitions_WithMultipleParams_ReturnsAll()
    {
        // Arrange
        var content = "\\define add(a,b) $1 + $2";

        // Act
        var macros = AdvancedWikiParser.ExtractMacroDefinitions(content).ToList();

        // Assert
        Assert.Single(macros);
        Assert.Equal(2, macros[0].Parameters.Count);
    }

    [Fact]
    public void ResolveMacros_WithDefinedMacro_ReplacesCall()
    {
        // Arrange
        var content = "Say <<greeting>> please";
        var macros = new Dictionary<string, MacroDefinition>
        {
            ["greeting"] = new() { Name = "greeting", Parameters = Array.Empty<string>(), Body = "Hello World" }
        };

        // Act
        var result = AdvancedWikiParser.ResolveMacros(content, macros);

        // Assert
        Assert.Equal("Say Hello World please", result);
    }

    [Fact]
    public void ResolveMacros_WithParameters_ReplacesArguments()
    {
        // Arrange
        var content = "<<greet Bob>>";
        var macros = new Dictionary<string, MacroDefinition>
        {
            ["greet"] = new() { Name = "greet", Parameters = new[] { "name" }, Body = "Hello $1" }
        };

        // Act
        var result = AdvancedWikiParser.ResolveMacros(content, macros);

        // Assert
        Assert.Equal("Hello Bob", result);
    }

    // Task 4.2: Transclusion
    [Fact]
    public void ExtractTransclusions_WithSimpleTransclusion_ReturnsReference()
    {
        // Arrange
        var content = "See {{SomeTiddler}} for details";

        // Act
        var refs = AdvancedWikiParser.ExtractTransclusions(content).ToList();

        // Assert
        Assert.Single(refs);
        Assert.Equal("SomeTiddler", refs[0].Target);
    }

    [Fact]
    public void ExtractTransclusions_WithField_ReturnsField()
    {
        // Arrange
        var content = "{{Tiddler||text}}";

        // Act
        var refs = AdvancedWikiParser.ExtractTransclusions(content).ToList();

        // Assert
        Assert.Single(refs);
        Assert.Equal("Tiddler", refs[0].Target);
        Assert.Equal("text", refs[0].Variable);
    }

    [Fact]
    public void ConvertTransclusionsToObsidian_ConvertsToEmbed()
    {
        // Arrange
        var content = "See {{My Tiddler}} here";

        // Act
        var result = AdvancedWikiParser.ConvertTransclusionsToObsidian(content);

        // Assert
        Assert.Equal("See ![[my-tiddler]] here", result);
    }

    // Task 4.3: Code Blocks
    [Fact]
    public void ExtractFencedCodeBlocks_WithLanguage_ReturnsBlocks()
    {
        // Arrange
        var content = "```csharp\nvar x = 1;\n```";

        // Act
        var blocks = AdvancedWikiParser.ExtractFencedCodeBlocks(content).ToList();

        // Assert
        Assert.Single(blocks);
        Assert.Equal("csharp", blocks[0].Language);
        Assert.Equal("var x = 1;\n", blocks[0].Code);
    }

    [Fact]
    public void HasFencedCodeBlock_ReturnsTrue()
    {
        // Arrange
        var content = "Some text\n```\ncode\n```";

        // Assert
        Assert.True(AdvancedWikiParser.HasFencedCodeBlock(content));
    }

    [Fact]
    public void HasFencedCodeBlock_WithNoCode_ReturnsFalse()
    {
        // Arrange
        var content = "Just plain text";

        // Assert
        Assert.False(AdvancedWikiParser.HasFencedCodeBlock(content));
    }

    // Task 4.4: Complex Formatting - Tables
    [Fact]
    public void ConvertHtmlTableToMarkdown_ConvertsTable()
    {
        // Arrange
        var content = "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>";

        // Act
        var result = AdvancedWikiParser.ConvertHtmlTableToMarkdown(content);

        // Assert
        Assert.Contains("A | B", result);
        Assert.Contains("1 | 2", result);
        Assert.Contains("--- | ---", result);
    }

    // Task 4.4: Complex Formatting - Nested Lists
    [Fact]
    public void GetMaxListDepth_WithNestedLists_ReturnsDepth()
    {
        // Arrange
        var content = "- Item 1\n  - Nested 1\n    - Deep nested";

        // Act
        var depth = AdvancedWikiParser.GetMaxListDepth(content);

        // Assert
        Assert.Equal(3, depth);
    }

    [Fact]
    public void GetMaxListDepth_WithNoLists_ReturnsZero()
    {
        // Arrange
        var content = "Just plain text";

        // Act
        var depth = AdvancedWikiParser.GetMaxListDepth(content);

        // Assert
        Assert.Equal(0, depth);
    }
}
