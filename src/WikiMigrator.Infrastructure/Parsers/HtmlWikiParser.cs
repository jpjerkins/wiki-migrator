using HtmlAgilityPack;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;
using WikiMigrator.Domain.ValueObjects;

namespace WikiMigrator.Infrastructure.Parsers;

/// <summary>
/// Parser for TiddlyWiki HTML export files.
/// Expected format:
/// &lt;div class="tiddler" data-tags="tag1 tag2" data-created="20240101" data-modified="20240115"&gt;
///   &lt;div class="title" id="MyTitle"&gt;My Title&lt;/div&gt;
///   &lt;div class="body"&gt;
///     Content here...
///   &lt;/div&gt;
/// &lt;/div&gt;
/// </summary>
public class HtmlWikiParser : IParser
{
    public Task<IEnumerable<WikiTiddler>> ParseAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(Enumerable.Empty<WikiTiddler>());
        }

        var tiddlers = new List<WikiTiddler>();
        var doc = new HtmlDocument();
        doc.LoadHtml(input);

        // Find all tiddler divs
        var tiddlerNodes = doc.DocumentNode.SelectNodes("//div[@class='tiddler']");

        if (tiddlerNodes == null || tiddlerNodes.Count == 0)
        {
            return Task.FromResult<IEnumerable<WikiTiddler>>(tiddlers);
        }

        foreach (var tiddlerNode in tiddlerNodes)
        {
            var tiddler = ParseTiddlerNode(tiddlerNode);
            if (tiddler != null)
            {
                tiddlers.Add(tiddler);
            }
        }

        return Task.FromResult<IEnumerable<WikiTiddler>>(tiddlers);
    }

    private WikiTiddler? ParseTiddlerNode(HtmlNode tiddlerNode)
    {
        if (tiddlerNode == null)
        {
            return null;
        }

        var tiddler = new WikiTiddler();
        var metadata = new TiddlerMetadata();

        // Extract data attributes
        var tagsAttr = tiddlerNode.GetAttributeValue("data-tags", string.Empty);
        var createdAttr = tiddlerNode.GetAttributeValue("data-created", string.Empty);
        var modifiedAttr = tiddlerNode.GetAttributeValue("data-modified", string.Empty);

        // Parse tags
        if (!string.IsNullOrWhiteSpace(tagsAttr))
        {
            metadata.Tags = ParseTags(tagsAttr);
        }

        // Parse dates
        if (!string.IsNullOrWhiteSpace(createdAttr))
        {
            var created = ParseDate(createdAttr);
            tiddler.Created = created;
            metadata.Created = created;
        }

        if (!string.IsNullOrWhiteSpace(modifiedAttr))
        {
            var modified = ParseDate(modifiedAttr);
            tiddler.Modified = modified;
            metadata.Modified = modified;
        }

        // Extract title
        var titleNode = tiddlerNode.SelectSingleNode(".//div[@class='title']");
        if (titleNode != null)
        {
            tiddler.Title = HtmlEntity.DeEntitize(titleNode.InnerText.Trim());
        }

        // If no title from div, try id attribute
        if (string.IsNullOrEmpty(tiddler.Title))
        {
            var idAttr = tiddlerNode.GetAttributeValue("id", string.Empty);
            if (!string.IsNullOrWhiteSpace(idAttr))
            {
                tiddler.Title = HtmlEntity.DeEntitize(idAttr);
            }
        }

        // Extract body content
        var bodyNode = tiddlerNode.SelectSingleNode(".//div[@class='body']");
        if (bodyNode != null)
        {
            tiddler.Content = HtmlEntity.DeEntitize(bodyNode.InnerText.Trim());
        }

        // If still no title, generate one
        if (string.IsNullOrEmpty(tiddler.Title))
        {
            tiddler.Title = GenerateTitle(tiddler.Content);
        }

        tiddler.Metadata = metadata;

        return tiddler;
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
