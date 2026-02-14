using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Infrastructure.Parsers;

/// <summary>
/// Parser for TID (TiddlyWiki) files.
/// Expected format:
/// title: My Tiddler
/// created: 20240101
/// modified: 20240115
/// tags: tag1, tag2
///
/// This is the body content.
/// </summary>
public class TidFileParser : IParser
{
    private const string TitlePrefix = "title:";
    private const string CreatedPrefix = "created:";
    private const string ModifiedPrefix = "modified:";
    private const string TagsPrefix = "tags:";
    private const string FieldDelimiter = "==";

    public Task<IEnumerable<WikiTiddler>> ParseAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(Enumerable.Empty<WikiTiddler>());
        }

        var tiddlers = new List<WikiTiddler>();
        
        // Split by double newlines to separate tiddlers if multiple are in one file
        var tiddlerBlocks = SplitTiddlerBlocks(input);
        
        foreach (var block in tiddlerBlocks)
        {
            var tiddler = ParseTiddlerBlock(block);
            if (tiddler != null)
            {
                tiddlers.Add(tiddler);
            }
        }

        return Task.FromResult<IEnumerable<WikiTiddler>>(tiddlers);
    }

    private IEnumerable<string> SplitTiddlerBlocks(string input)
    {
        // Split by double newlines, but also handle the case where
        // there's no body content (just metadata)
        var blocks = input.Split(new[] { "\n\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (blocks.Length == 0)
        {
            return new[] { input };
        }
        
        return blocks.Where(b => !string.IsNullOrWhiteSpace(b));
    }

    private WikiTiddler? ParseTiddlerBlock(string block)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return null;
        }

        var lines = block.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var tiddler = new WikiTiddler();
        var metadata = new Domain.ValueObjects.TiddlerMetadata();
        
        int bodyStartIndex = -1;
        
        // Parse metadata lines
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            
            // Check for body delimiter
            if (line.TrimStart().StartsWith(FieldDelimiter))
            {
                bodyStartIndex = i + 1;
                break;
            }
            
            // Parse key-value pairs
            if (line.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase))
            {
                tiddler.Title = line.Substring(TitlePrefix.Length).Trim();
            }
            else if (line.StartsWith(CreatedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var dateStr = line.Substring(CreatedPrefix.Length).Trim();
                tiddler.Created = ParseDate(dateStr);
                metadata.Created = tiddler.Created;
            }
            else if (line.StartsWith(ModifiedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var dateStr = line.Substring(ModifiedPrefix.Length).Trim();
                tiddler.Modified = ParseDate(dateStr);
                metadata.Modified = tiddler.Modified;
            }
            else if (line.StartsWith(TagsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var tagsStr = line.Substring(TagsPrefix.Length).Trim();
                metadata.Tags = ParseTags(tagsStr);
            }
            else if (line.Contains(':') && !string.IsNullOrWhiteSpace(line.Split(':')[0]))
            {
                // Custom field (key:value)
                var colonIndex = line.IndexOf(':');
                var fieldName = line.Substring(0, colonIndex).Trim();
                var fieldValue = line.Substring(colonIndex + 1).Trim();
                
                // Skip if it looks like a header/field delimiter
                if (!string.IsNullOrEmpty(fieldName) && fieldName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                {
                    tiddler.Fields.Add(new WikiField
                    {
                        Name = fieldName,
                        Value = fieldValue
                    });
                }
            }
        }
        
        // Extract body content
        if (bodyStartIndex >= 0 && bodyStartIndex < lines.Count)
        {
            var bodyLines = lines.Skip(bodyStartIndex).TakeWhile(l => !l.TrimStart().StartsWith(FieldDelimiter));
            tiddler.Content = string.Join("\n", bodyLines).Trim();
        }
        else if (bodyStartIndex < 0)
        {
            // No field delimiter, content is everything after the last metadata line
            // Find the first empty line to determine where content starts
            var contentStartIndex = lines.FindIndex(l => string.IsNullOrWhiteSpace(l));
            if (contentStartIndex >= 0)
            {
                var contentLines = lines.Skip(contentStartIndex + 1).Where(l => !string.IsNullOrWhiteSpace(l));
                tiddler.Content = string.Join("\n", contentLines).Trim();
            }
        }
        
        // If no title, generate one from content hash or skip
        if (string.IsNullOrEmpty(tiddler.Title))
        {
            tiddler.Title = GenerateTitle(tiddler.Content);
        }
        
        tiddler.Metadata = metadata;
        
        return tiddler;
    }

    private DateTime ParseDate(string dateStr)
    {
        // Try multiple date formats
        string[] formats = {
            "yyyyMMdd",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss"
        };
        
        if (DateTime.TryParseExact(dateStr.Trim(), formats, 
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, 
            out var date))
        {
            return date;
        }
        
        // Fallback: try general parsing
        if (DateTime.TryParse(dateStr, out date))
        {
            return date;
        }
        
        return DateTime.Now;
    }

    private List<string> ParseTags(string tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr))
        {
            return new List<string>();
        }
        
        return tagsStr
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private string GenerateTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Untitled_{DateTime.Now:yyyyMMddHHmmss}";
        }
        
        // Generate a title from the first line or content hash
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
