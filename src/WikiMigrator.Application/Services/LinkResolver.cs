using System.Text.RegularExpressions;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Service for resolving wiki-style links in content.
/// Parses [[link]] and [[link|text]] formats and maps them to sanitized filenames.
/// </summary>
public class LinkResolver : ILinkResolver
{
    // Regex to match wiki links: [[link]] or [[link|text]]
    private static readonly Regex WikiLinkRegex = new(@"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]", RegexOptions.Compiled);
    
    private readonly Dictionary<string, string> _linkMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownTitles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a title with its sanitized filename for link resolution.
    /// </summary>
    /// <param name="title">The original wiki title</param>
    /// <param name="sanitizedFilename">The sanitized filename (without extension)</param>
    public void RegisterLink(string title, string sanitizedFilename)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
            
        if (string.IsNullOrWhiteSpace(sanitizedFilename))
            throw new ArgumentException("Sanitized filename cannot be null or empty", nameof(sanitizedFilename));

        _linkMap[title] = sanitizedFilename;
        _knownTitles.Add(title);
    }

    /// <summary>
    /// Registers a collection of tiddlers, building the link map from their titles.
    /// </summary>
    /// <param name="tiddlers">Collection of wiki tiddlers</param>
    public void RegisterTiddlers(IEnumerable<WikiTiddler> tiddlers)
    {
        foreach (var tiddler in tiddlers)
        {
            var sanitized = SanitizeFilename(tiddler.Title);
            RegisterLink(tiddler.Title, sanitized);
        }
    }

    /// <summary>
    /// Extracts all wiki links from content without resolving them.
    /// </summary>
    /// <param name="content">Content containing wiki links</param>
    /// <returns>List of link targets found in the content</returns>
    public static IEnumerable<string> ExtractLinks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<string>();

        var links = new List<string>();
        var matches = WikiLinkRegex.Matches(content);
        
        foreach (Match match in matches)
        {
            var linkTarget = match.Groups[1].Value.Trim();
            links.Add(linkTarget);
        }

        return links.Distinct();
    }

    /// <summary>
    /// Resolves wiki links in content to markdown-style links using the registered link map.
    /// </summary>
    /// <param name="content">Content containing wiki links</param>
    /// <returns>Content with wiki links converted to markdown links</returns>
    public string ResolveLinks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        return WikiLinkRegex.Replace(content, match =>
        {
            var linkTarget = match.Groups[1].Value.Trim();
            var linkText = match.Groups[2].Success ? match.Groups[2].Value.Trim() : linkTarget;

            // Try to find the sanitized filename for this link
            if (_linkMap.TryGetValue(linkTarget, out var sanitizedFilename))
            {
                return $"[{linkText}]({sanitizedFilename}.md)";
            }

            // If not found in the map, use sanitized version of the link target
            var fallbackSanitized = SanitizeFilename(linkTarget);
            return $"[{linkText}]({fallbackSanitized}.md)";
        });
    }

    /// <summary>
    /// Gets the sanitized filename for a given title.
    /// </summary>
    /// <param name="title">The wiki title</param>
    /// <returns>The sanitized filename, or null if not registered</returns>
    public string? GetSanitizedFilename(string title)
    {
        return _linkMap.GetValueOrDefault(title);
    }

    /// <summary>
    /// Checks if a title exists in the known titles set.
    /// </summary>
    /// <param name="title">The wiki title to check</param>
    /// <returns>True if the title is known</returns>
    public bool HasTitle(string title)
    {
        return _knownTitles.Contains(title);
    }

    /// <summary>
    /// Gets all registered link mappings.
    /// </summary>
    /// <returns>Dictionary of title to sanitized filename</returns>
    public IReadOnlyDictionary<string, string> GetLinkMap()
    {
        return _linkMap;
    }

    /// <summary>
    /// Clears all registered links.
    /// </summary>
    public void Clear()
    {
        _linkMap.Clear();
        _knownTitles.Clear();
    }

    /// <summary>
    /// Sanitizes a filename for file system compatibility.
    /// </summary>
    /// <param name="title">The title to sanitize</param>
    /// <returns>A sanitized filename</returns>
    public static string SanitizeFilename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "untitled";

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(title
            .Where(c => !invalid.Contains(c))
            .ToArray());

        // Replace spaces and common separators with hyphens
        sanitized = Regex.Replace(sanitized, @"[\s_]+", "-");

        // Remove any remaining non-alphanumeric characters (except hyphens)
        sanitized = Regex.Replace(sanitized, @"[^\w\-]", "");

        // Convert to lowercase
        sanitized = sanitized.ToLowerInvariant();

        // Remove leading/trailing hyphens
        sanitized = sanitized.Trim('-');

        // Handle empty result
        if (string.IsNullOrEmpty(sanitized))
            return "untitled";

        return sanitized;
    }
}

public interface ILinkResolver
{
    void RegisterLink(string title, string sanitizedFilename);
    void RegisterTiddlers(IEnumerable<WikiTiddler> tiddlers);
    static IEnumerable<string> ExtractLinks(string content) => LinkResolver.ExtractLinks(content);
    string ResolveLinks(string content);
    string? GetSanitizedFilename(string title);
    bool HasTitle(string title);
    IReadOnlyDictionary<string, string> GetLinkMap();
    void Clear();
    static string SanitizeFilename(string title) => LinkResolver.SanitizeFilename(title);
}
