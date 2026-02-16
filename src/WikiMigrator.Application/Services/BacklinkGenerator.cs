using System.Text.Json.Serialization;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Service for generating backlinks from a LinkGraph.
/// </summary>
public class BacklinkGenerator
{
    private readonly LinkGraph _graph;

    public BacklinkGenerator(LinkGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Generates backlinks for a specific tiddler.
    /// </summary>
    /// <param name="title">The tiddler title</param>
    /// <returns>List of titles that link to this tiddler</returns>
    public IReadOnlyList<string> GetBacklinksFor(string title)
    {
        return _graph.GetBacklinks(title).ToList();
    }

    /// <summary>
    /// Generates backlinks for all tiddlers in the graph.
    /// </summary>
    /// <returns>Dictionary mapping title to list of backlinks</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GenerateAllBacklinks()
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var node in _graph.GetAllNodes())
        {
            var backlinks = GetBacklinksFor(node);
            if (backlinks.Any())
            {
                result[node] = backlinks;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets orphaned tiddlers (no incoming links).
    /// </summary>
    public IReadOnlyList<string> GetOrphanedTiddlers()
    {
        return _graph.GetOrphanedNodes();
    }

    /// <summary>
    /// Generates YAML frontmatter for backlinks.
    /// </summary>
    /// <param name="title">The tiddler title</param>
    /// <returns>YAML-formatted backlinks string</returns>
    public string GenerateBacklinksYaml(string title)
    {
        var backlinks = GetBacklinksFor(title);
        
        if (!backlinks.Any())
            return string.Empty;

        // Use Obsidian's backlinks format
        var yaml = "---";
        yaml += $"\nbacklinks:";
        
        foreach (var backlink in backlinks)
        {
            var sanitized = LinkResolver.SanitizeFilename(backlink);
            yaml += $"\n  - [[{sanitized}|{backlink}]]";
        }
        
        yaml += "\n---";
        
        return yaml;
    }
}

/// <summary>
/// Extension methods for adding backlinks to WikiTiddler.
/// </summary>
public static class WikiTiddlerBacklinksExtensions
{
    /// <summary>
    /// Adds backlinks to a tiddler from the link graph.
    /// </summary>
    /// <param name="tiddler">The tiddler to update</param>
    /// <param name="backlinks">List of backlink titles</param>
    public static void AddBacklinks(this WikiTiddler tiddler, IReadOnlyList<string> backlinks)
    {
        // The WikiTiddler class needs a Backlinks property
        // This is a placeholder - actual implementation depends on the model
    }
}
