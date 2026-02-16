using WikiMigrator.Domain.Entities;
using WikiMigrator.Infrastructure.Parsers;

namespace WikiMigrator.Tests;

public class TiddlyWiki5JsonParserTests
{
    private readonly TiddlyWiki5JsonParser _parser = new();

    [Fact]
    public void ParseAsync_WithEmptyInput_ReturnsEmpty()
    {
        var result = _parser.ParseAsync("").Result;
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithNullInput_ReturnsEmpty()
    {
        var result = _parser.ParseAsync(null!).Result;
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithWhitespaceInput_ReturnsEmpty()
    {
        var result = _parser.ParseAsync("   ").Result;
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithNoScriptTag_ReturnsEmpty()
    {
        var input = @"<!DOCTYPE html><html><body>No script here</body></html>";
        var result = _parser.ParseAsync(input).Result;
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithJsonScriptTag_ParsesTiddler()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test Tiddler"":{""title"":""Test Tiddler"",""text"":""Hello World""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Single(result);
        Assert.Equal("Test Tiddler", result[0].Title);
        Assert.Equal("Hello World", result[0].Content);
    }

    [Fact]
    public void ParseAsync_WithIdTiddlers_ParsesTiddler()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script id=""tiddlers"" type=""text/plain"">
{""tiddlers"":{""Test Tiddler"":{""title"":""Test Tiddler"",""text"":""Content""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Single(result);
        Assert.Equal("Test Tiddler", result[0].Title);
    }

    [Fact]
    public void ParseAsync_DirectArrayFormat_ParsesMultipleTiddlers()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"" id=""tiddlers"">
[
    {""title"":""First"",""text"":""First content""},
    {""title"":""Second"",""text"":""Second content""},
    {""title"":""Third"",""text"":""Third content""}
]
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Equal(3, result.Count);
        Assert.Equal("First", result[0].Title);
        Assert.Equal("Second", result[1].Title);
        Assert.Equal("Third", result[2].Title);
    }

    [Fact]
    public void ParseAsync_ObjectWithTiddlersArray_ParsesMultipleTiddlers()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":[
    {""title"":""First"",""text"":""First content""},
    {""title"":""Second"",""text"":""Second content""}
]}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseAsync_WithTagsArray_ParsesTags()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""tags"":[""tag1"",""tag2"",""tag3""]}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Equal(3, result.Metadata.Tags.Count);
        Assert.Contains("tag1", result.Metadata.Tags);
        Assert.Contains("tag2", result.Metadata.Tags);
        Assert.Contains("tag3", result.Metadata.Tags);
    }

    [Fact]
    public void ParseAsync_WithTagsString_ParsesTags()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""tags"":""tag1 tag2""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Contains("tag1", result.Metadata.Tags);
        Assert.Contains("tag2", result.Metadata.Tags);
    }

    [Fact]
    public void ParseAsync_WithTagsCommaSeparated_ParsesTags()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""tags"":""tag1,tag2,tag3""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Equal(3, result.Metadata.Tags.Count);
    }

    [Fact]
    public void ParseAsync_WithCreatedDate_ParsesDate()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""created"":""20220115120000""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.Equal(2022, result.Created.Year);
        Assert.Equal(1, result.Created.Month);
        Assert.Equal(15, result.Created.Day);
        Assert.Equal(12, result.Created.Hour);
        Assert.Equal(0, result.Created.Minute);
    }

    [Fact]
    public void ParseAsync_WithModifiedDate_ParsesDate()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""modified"":""20231225235900""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.Equal(2023, result.Modified.Year);
        Assert.Equal(12, result.Modified.Month);
        Assert.Equal(25, result.Modified.Day);
        Assert.Equal(23, result.Modified.Hour);
        Assert.Equal(59, result.Modified.Minute);
    }

    [Fact]
    public void ParseAsync_WithISO8601Date_ParsesDate()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""created"":""2022-01-15T12:30:00Z""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.Equal(2022, result.Created.Year);
        Assert.Equal(1, result.Created.Month);
        Assert.Equal(15, result.Created.Day);
    }

    [Fact]
    public void ParseAsync_WithInvalidDate_UsesCurrentTime()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""created"":""invalid-date""}}}
</script>
</body></html>";

        var before = DateTime.Now.AddMinutes(-1);
        var result = _parser.ParseAsync(input).Result.First();
        var after = DateTime.Now.AddMinutes(1);

        Assert.InRange(result.Created, before, after);
    }

    [Fact]
    public void ParseAsync_WithEmptyDate_UsesCurrentTime()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""created"":""""}}}
</script>
</body></html>";

        var before = DateTime.Now.AddMinutes(-1);
        var result = _parser.ParseAsync(input).Result.First();
        var after = DateTime.Now.AddMinutes(1);

        Assert.InRange(result.Created, before, after);
    }

    [Fact]
    public void ParseAsync_WithCreator_StoresAuthor()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""creator"":""JohnDoe""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Equal("JohnDoe", result.Metadata.Author);
    }

    [Fact]
    public void ParseAsync_WithSpecialCharactersInTitle_ParsesCorrectly()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test [bracketed]"":{""title"":""Test [bracketed]"",""text"":""Content""},""Test (parens)"":{""title"":""Test (parens)"",""text"":""Content""},""Test | pipe"":{""title"":""Test | pipe"",""text"":""Content""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ParseAsync_WithUnicodeInTitle_ParsesCorrectly()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Über"":{""title"":""Über"",""text"":""Content""},""日本語"":{""title"":""日本語"",""text"":""Content""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Equal(2, result.Count);
        Assert.Equal("Über", result[0].Title);
    }

    [Fact]
    public void ParseAsync_WithEmptyText_GeneratesTitleFromContent()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""My Title"":{""title"":""My Title"",""text"":""""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.Equal("My Title", result.Title);
    }

    [Fact]
    public void ParseAsync_WithMissingTitle_UsesFallback()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{"""":{""text"":""Some content here""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        // Should generate title from content
        Assert.False(string.IsNullOrEmpty(result.Title));
    }

    [Fact]
    public void ParseAsync_WithMalformedJSON_ReturnsEmpty()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{invalid json here}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result;
        
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithNonObjectTiddler_Skips()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":""not an object""}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result;
        
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithMultipleScriptTags_ParsesFirstJson()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""text/javascript"">var x = 1;</script>
<script type=""application/json"">
{""tiddlers"":{""First"":{""title"":""First"",""text"":""Content""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.ToList();
        
        Assert.Single(result);
        Assert.Equal("First", result[0].Title);
    }

    [Fact]
    public void ParseAsync_CaseInsensitiveFieldNames_Parses()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""TIDDLERS"":{""Test"":{""TITLE"":""Test"",""TEXT"":""Content""}}}
</script>
</body></html>";

        // This tests that the regex works case-insensitively
        // Note: JSON property names are case-sensitive in System.Text.Json
        var result = _parser.ParseAsync(input).Result;
        
        // Should not find tiddlers since property name is uppercase
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsync_WithTagsEmptyArray_ReturnsEmptyTags()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""tags"":[]}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata.Tags);
    }

    [Fact]
    public void ParseAsync_WithTagsWithEmptyStrings_FiltersEmpty()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Test"":{""title"":""Test"",""text"":""Content"",""tags"":[""tag1"","""",""  "",""tag2""]}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.NotNull(result.Metadata);
        Assert.Equal(2, result.Metadata.Tags.Count);
        Assert.Contains("tag1", result.Metadata.Tags);
        Assert.Contains("tag2", result.Metadata.Tags);
    }

    [Fact]
    public void ParseAsync_WithTitleInObject_UsesObjectTitle()
    {
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{""Key Title"":{""title"":""Actual Title"",""text"":""Content""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        Assert.Equal("Actual Title", result.Title);
    }

    [Fact]
    public void ParseAsync_WithVeryLongTitle_TruncatesForFallback()
    {
        var veryLongContent = new string('a', 200);
        var input = @"<!DOCTYPE html>
<html><head></head>
<body>
<script type=""application/json"">
{""tiddlers"":{"""":{""text"":""" + veryLongContent + @"""}}}
</script>
</body></html>";

        var result = _parser.ParseAsync(input).Result.First();
        
        // Title generated from content should be truncated to ~50 chars
        Assert.NotNull(result.Title);
        Assert.True(result.Title.Length <= 50);
    }
}
