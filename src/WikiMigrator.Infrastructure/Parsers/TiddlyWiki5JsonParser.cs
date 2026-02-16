using System.Text.Json;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;
using WikiMigrator.Domain.ValueObjects;

namespace WikiMigrator.Infrastructure.Parsers;

/// <summary>
/// Parser for TiddlyWiki 5.x HTML export files that store tiddlers in JSON format.
/// The JSON is embedded in a &lt;script type="application/json"&gt; tag with id="tiddlers".
/// </summary>
public class TiddlyWiki5JsonParser : IParser
{
    public Task<IEnumerable<WikiTiddler>> ParseAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(Enumerable.Empty<WikiTiddler>());
        }

        var tiddlers = new List<WikiTiddler>();

        // Find the JSON script tag
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(
            input, 
            @"<script[^>]*type\s*=\s*[""']application/json[""'][^>]*>(.*?)</script>",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!jsonMatch.Success)
        {
            // Try alternative pattern - look for id="tiddlers"
            jsonMatch = System.Text.RegularExpressions.Regex.Match(
                input,
                @"<script[^>]*id\s*=\s*[""']tiddlers[""'][^>]*>(.*?)</script>",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        if (!jsonMatch.Success)
        {
            return Task.FromResult<IEnumerable<WikiTiddler>>(tiddlers);
        }

        var jsonContent = jsonMatch.Groups[1].Value;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Check if root is an array directly (TiddlyWiki 5.x format)
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var tiddlerItem in root.EnumerateArray())
                {
                    var tiddler = ParseTiddlerFromArray(tiddlerItem);
                    if (tiddler != null)
                    {
                        tiddlers.Add(tiddler);
                    }
                }
            }
            // Check for "tiddlers" property (older format)
            else if (root.TryGetProperty("tiddlers", out var tiddlersElement))
            {
                if (tiddlersElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var tiddlerEntry in tiddlersElement.EnumerateObject())
                    {
                        var tiddler = ParseTiddlerEntry(tiddlerEntry.Name, tiddlerEntry.Value);
                        if (tiddler != null)
                        {
                            tiddlers.Add(tiddler);
                        }
                    }
                }
                else if (tiddlersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tiddlerItem in tiddlersElement.EnumerateArray())
                    {
                        var tiddler = ParseTiddlerFromArray(tiddlerItem);
                        if (tiddler != null)
                        {
                            tiddlers.Add(tiddler);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, return empty
        }

        return Task.FromResult<IEnumerable<WikiTiddler>>(tiddlers);
    }

    private WikiTiddler? ParseTiddlerEntry(string title, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var tiddler = new WikiTiddler { Title = title };
        var metadata = new TiddlerMetadata();

        // Extract fields
        foreach (var prop in element.EnumerateObject())
        {
            switch (prop.Name.ToLowerInvariant())
            {
                case "title":
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        tiddler.Title = prop.Value.GetString() ?? title;
                    }
                    break;

                case "text":
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        tiddler.Content = prop.Value.GetString() ?? "";
                    }
                    break;

                case "tags":
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var tags = new List<string>();
                        foreach (var tag in prop.Value.EnumerateArray())
                        {
                            if (tag.ValueKind == JsonValueKind.String)
                            {
                                var tagStr = tag.GetString();
                                if (!string.IsNullOrWhiteSpace(tagStr))
                                {
                                    tags.Add(tagStr);
                                }
                            }
                        }
                        metadata.Tags = tags;
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var tagsStr = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(tagsStr))
                        {
                            metadata.Tags = ParseTags(tagsStr);
                        }
                    }
                    break;

                case "created":
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var created = ParseTiddlyWikiDate(prop.Value.GetString());
                        tiddler.Created = created;
                        metadata.Created = created;
                    }
                    break;

                case "modified":
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var modified = ParseTiddlyWikiDate(prop.Value.GetString());
                        tiddler.Modified = modified;
                        metadata.Modified = modified;
                    }
                    break;

                case "type":
                    // Type field - we can store this in wiki field or just skip
                    break;

                case "creator":
                    // Creator field - store in Author if needed
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        metadata.Author = prop.Value.GetString() ?? "";
                    }
                    break;

                case "modifier":
                    // Modifier field - we can store this in wiki field or just skip
                    break;
            }
        }

        tiddler.Metadata = metadata;

        // Generate title if still empty
        if (string.IsNullOrEmpty(tiddler.Title))
        {
            tiddler.Title = GenerateTitle(tiddler.Content);
        }

        return tiddler;
    }

    private WikiTiddler? ParseTiddlerFromArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Try to get title first
        string? title = null;
        if (element.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
        {
            title = titleProp.GetString();
        }

        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        return ParseTiddlerEntry(title, element);
    }

    private List<string> ParseTags(string tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr))
        {
            return new List<string>();
        }

        return tagsStr
            .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private DateTime ParseTiddlyWikiDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return DateTime.Now;
        }

        // TiddlyWiki dates are in format: YYYYMMDDHHMMSSmmm
        // Example: 20220224140634868
        if (dateStr.Length >= 14 && long.TryParse(dateStr.Substring(0, 14), out var ticks))
        {
            try
            {
                // Convert to DateTime
                var year = int.Parse(dateStr.Substring(0, 4));
                var month = int.Parse(dateStr.Substring(4, 2));
                var day = int.Parse(dateStr.Substring(6, 2));
                var hour = int.Parse(dateStr.Substring(8, 2));
                var minute = int.Parse(dateStr.Substring(10, 2));
                var second = int.Parse(dateStr.Substring(12, 2));

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            catch
            {
                // Fall through to standard parsing
            }
        }

        // Try standard parsing
        if (DateTime.TryParse(dateStr, out var date))
        {
            return date;
        }

        return DateTime.Now;
    }

    private string GenerateTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Untitled_{DateTime.Now:yyyyMMddHHmmss}";
        }

        var firstLine = content.Split('\n').FirstOrDefault()?.Trim() ?? "";
        if (firstLine.Length > 50)
        {
            firstLine = firstLine.Substring(0, 47) + "...";
        }

        return string.IsNullOrWhiteSpace(firstLine)
            ? $"Untitled_{DateTime.Now:yyyyMMddHHmmss}"
            : firstLine;
    }
}
