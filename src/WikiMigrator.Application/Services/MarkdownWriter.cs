using System.Text;
using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Application.Services;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

public class MarkdownWriter : IMarkdownWriter
{
    private readonly ILogger<MarkdownWriter> _logger;
    private readonly ITagProcessor _tagProcessor;

    public MarkdownWriter(ILogger<MarkdownWriter> logger, ITagProcessor tagProcessor)
    {
        _logger = logger;
        _tagProcessor = tagProcessor;
    }

    /// <inheritdoc/>
    public string GenerateFrontmatter(WikiTiddler tiddler)
    {
        _logger.LogDebug("Generating frontmatter for tiddler: {Title}", tiddler.Title);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");

        // Add title
        sb.AppendLine($"title: \"{EscapeYamlString(tiddler.Title)}\"");

        // Add dates
        sb.AppendLine($"created: {tiddler.Created:yyyy-MM-dd}");
        sb.AppendLine($"modified: {tiddler.Modified:yyyy-MM-dd}");

        // Add tags if any
        var tags = tiddler.Fields.FirstOrDefault(f => f.Name.Equals("tags", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var yamlTags = _tagProcessor.ProcessTags(tags);
            sb.AppendLine(yamlTags);
        }

        // Add custom fields (excluding standard ones already handled)
        foreach (var field in tiddler.Fields)
        {
            var fieldNameLower = field.Name.ToLowerInvariant();
            if (fieldNameLower != "title" && fieldNameLower != "tags" && 
                fieldNameLower != "created" && fieldNameLower != "modified")
            {
                sb.AppendLine($"{field.Name}: \"{EscapeYamlString(field.Value)}\"");
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string SanitizeFilename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "untitled";
        }

        // Characters that are invalid in filenames
        // Path.GetInvalidFileNameChars() doesn't include < > : | ? * on Linux/.NET 10, so add them manually
        var invalidChars = Path.GetInvalidFileNameChars().ToList();
        invalidChars.AddRange(new[] { '<', '>', ':', '|', '?', '*' });
        invalidChars = invalidChars.Distinct().ToList();

        var sanitized = new StringBuilder();
        foreach (var c in title)
        {
            if (invalidChars.Contains(c))
            {
                sanitized.Append('_');
            }
            else
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString().Trim();

        // Handle edge cases
        if (string.IsNullOrWhiteSpace(result))
        {
            return "untitled";
        }

        // Ensure filename isn't too long (Windows max is 260, leave some buffer)
        if (result.Length > 200)
        {
            result = result.Substring(0, 200);
        }

        // Don't start or end with dots or spaces
        result = result.TrimStart('.', ' ');
        result = result.TrimEnd('.', ' ');

        return result;
    }

    /// <inheritdoc/>
    public string GenerateMarkdown(WikiTiddler tiddler, string convertedContent)
    {
        _logger.LogDebug("Generating markdown for tiddler: {Title}", tiddler.Title);

        var frontmatter = GenerateFrontmatter(tiddler);
        return frontmatter + convertedContent;
    }

    /// <inheritdoc/>
    public async Task<bool> WriteMarkdownFileAsync(WikiTiddler tiddler, string convertedContent, string outputPath)
    {
        try
        {
            _logger.LogInformation("Writing markdown file: {OutputPath}", outputPath);

            // Generate the full markdown content
            var markdown = GenerateMarkdown(tiddler, convertedContent);

            // Get the directory path
            var directory = Path.GetDirectoryName(outputPath);

            // Create directory if it doesn't exist
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Write the file
            await File.WriteAllTextAsync(outputPath, markdown);

            _logger.LogInformation("Successfully wrote markdown file: {OutputPath}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write markdown file: {OutputPath}", outputPath);
            return false;
        }
    }

    private static string EscapeYamlString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Escape characters that need to be escaped in YAML strings
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
