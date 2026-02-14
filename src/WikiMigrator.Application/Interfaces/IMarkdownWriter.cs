using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Interfaces;

public interface IMarkdownWriter
{
    /// <summary>
    /// Generates YAML frontmatter for a WikiTiddler.
    /// </summary>
    string GenerateFrontmatter(WikiTiddler tiddler);

    /// <summary>
    /// Generates a sanitized filename from a tiddler title.
    /// </summary>
    string SanitizeFilename(string title);

    /// <summary>
    /// Generates the full markdown content including frontmatter and converted content.
    /// </summary>
    string GenerateMarkdown(WikiTiddler tiddler, string convertedContent);

    /// <summary>
    /// Writes a WikiTiddler to a markdown file at the specified path.
    /// </summary>
    Task<bool> WriteMarkdownFileAsync(WikiTiddler tiddler, string convertedContent, string outputPath);
}
