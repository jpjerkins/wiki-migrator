using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Service for resolving PARA folder paths based on tiddler tags.
/// </summary>
public class ParaFolderResolver
{
    private readonly ParaTagClassifier _classifier;

    public ParaFolderResolver(ParaTagClassifier classifier)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <summary>
    /// Gets the target folder path for a tiddler based on its PARA tags.
    /// </summary>
    public string GetTargetFolderPath(WikiTiddler tiddler, string baseOutputPath, string? subFolder = null)
    {
        if (tiddler == null)
            throw new ArgumentNullException(nameof(tiddler));
        
        if (string.IsNullOrWhiteSpace(baseOutputPath))
            throw new ArgumentNullException(nameof(baseOutputPath));

        // Extract tags from tiddler
        var tags = ExtractTags(tiddler);
        
        // Classify the PARA folder type
        var folderType = _classifier.ClassifyParaFolder(tags);
        
        // Get the PARA folder path
        var paraFolder = _classifier.GetParaFolderPath(folderType);
        
        // Combine with base output path and optional subfolder
        var fullPath = string.IsNullOrEmpty(subFolder) 
            ? Path.Combine(baseOutputPath, paraFolder)
            : Path.Combine(baseOutputPath, paraFolder, subFolder);
            
        return fullPath;
    }

    /// <summary>
    /// Resolves the full output path including the filename.
    /// </summary>
    public string ResolveFullOutputPath(WikiTiddler tiddler, string baseOutputPath, string? subFolder = null, string extension = ".md")
    {
        var folderPath = GetTargetFolderPath(tiddler, baseOutputPath, subFolder);
        var fileName = SanitizeFileName(tiddler.Title) + extension;
        return Path.Combine(folderPath, fileName);
    }

    /// <summary>
    /// Ensures the target directory exists, creating it if necessary.
    /// </summary>
    public string EnsureDirectoryExists(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath))
            throw new ArgumentException("Target path cannot be null or empty.", nameof(targetPath));

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        return targetPath;
    }

    /// <summary>
    /// Gets the full file path for a tiddler in its PARA folder.
    /// </summary>
    public string GetFilePath(WikiTiddler tiddler, string baseOutputPath, string extension = ".md")
    {
        var folderPath = GetTargetFolderPath(tiddler, baseOutputPath);
        
        // Sanitize the filename
        var fileName = SanitizeFileName(tiddler.Title) + extension;
        
        return Path.Combine(folderPath, fileName);
    }

    /// <summary>
    /// Handles filename conflicts by appending a number.
    /// </summary>
    public string ResolveFileConflict(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentNullException(nameof(targetPath));

        if (!File.Exists(targetPath))
            return targetPath;

        var directory = Path.GetDirectoryName(targetPath) ?? "";
        var fileName = Path.GetFileNameWithoutExtension(targetPath);
        var extension = Path.GetExtension(targetPath);

        int counter = 1;
        string newPath;
        
        do
        {
            newPath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Extracts tags from a WikiTiddler's fields.
    /// </summary>
    public List<string> GetTags(WikiTiddler tiddler)
    {
        return ExtractTags(tiddler);
    }

    /// <summary>
    /// Extracts tags from a WikiTiddler's fields.
    /// </summary>
    private List<string> ExtractTags(WikiTiddler tiddler)
    {
        var tags = new List<string>();
        
        if (tiddler.Fields == null)
            return tags;

        var tagField = tiddler.Fields.FirstOrDefault(f => 
            f.Name.Equals("tags", StringComparison.OrdinalIgnoreCase));
        
        if (tagField?.Value != null)
        {
            var tagValue = tagField.Value.ToString();
            if (!string.IsNullOrWhiteSpace(tagValue))
            {
                tags.AddRange(tagValue.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t)));
            }
        }

        return tags;
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "untitled";

        // Characters invalid in Windows filenames - more comprehensive list
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        var sanitized = fileName;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }

        // Also replace dashes with spaces (WebDAV sync issue)
        sanitized = sanitized.Replace("-", " ");

        return sanitized.Trim();
    }
}
