using System.Text.RegularExpressions;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Represents a reference to an image or attachment in wiki content.
/// </summary>
public class ImageReference
{
    public string OriginalPath { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public int Position { get; init; }
    public ImageReferenceType Type { get; init; }
    public string? ResolvedPath { get; set; }
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// The type of image reference.
    /// </summary>
    public enum ImageReferenceType
    {
        Markdown,    // ![alt](path)
        Html,        // <img src="path" alt="alt">
        Wiki,        // {{path}}
        Unknown
    }
}

/// <summary>
/// Service for extracting and processing image references from wiki content.
/// </summary>
public class ImageReferenceParser
{
    // Markdown image: ![alt](path) or ![alt](path "title")
    private static readonly Regex MarkdownImageRegex = new(@"!\[([^\]]*)\]\(([^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);
    
    // HTML image: <img src="path" alt="alt"> or <img alt="alt" src="path">
    private static readonly Regex HtmlImageRegex = new(@"<img\s+[^>]*src\s*=\s*[""']([^""']+)[""'][^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Wiki transclusion/image: {{path}} or {{path||alt}}
    private static readonly Regex WikiImageRegex = new(@"\{\{([^}|]+)(?:\|\|([^\}]+))?\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Extracts all image references from content.
    /// </summary>
    /// <param name="content">Content to parse</param>
    /// <returns>List of image references</returns>
    public static IEnumerable<ImageReference> ExtractImages(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<ImageReference>();

        var references = new List<ImageReference>();

        // Extract markdown images
        foreach (Match match in MarkdownImageRegex.Matches(content))
        {
            var alt = match.Groups[1].Value;
            var path = match.Groups[2].Value.Trim();
            
            references.Add(new ImageReference
            {
                OriginalPath = path,
                AltText = string.IsNullOrEmpty(alt) ? null : alt,
                Position = match.Index,
                Type = ImageReference.ImageReferenceType.Markdown
            });
        }

        // Extract HTML images
        foreach (Match match in HtmlImageRegex.Matches(content))
        {
            var path = match.Groups[1].Value;
            var alt = ExtractHtmlAlt(match.Value);
            
            references.Add(new ImageReference
            {
                OriginalPath = path,
                AltText = alt,
                Position = match.Index,
                Type = ImageReference.ImageReferenceType.Html
            });
        }

        // Extract wiki images/transclusions
        foreach (Match match in WikiImageRegex.Matches(content))
        {
            var path = match.Groups[1].Value.Trim();
            var alt = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            
            // Only include if it looks like an image (has extension)
            if (IsImagePath(path))
            {
                references.Add(new ImageReference
                {
                    OriginalPath = path,
                    AltText = alt,
                    Position = match.Index,
                    Type = ImageReference.ImageReferenceType.Wiki
                });
            }
        }

        return references.OrderBy(r => r.Position);
    }

    private static string? ExtractHtmlAlt(string imgTag)
    {
        var altMatch = Regex.Match(imgTag, @"alt\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        return altMatch.Success ? altMatch.Groups[1].Value : null;
    }

    private static bool IsImagePath(string path)
    {
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".bmp", ".ico" };
        var lowerPath = path.ToLowerInvariant();
        return imageExtensions.Any(ext => lowerPath.EndsWith(ext));
    }
}

/// <summary>
/// Service for copying attachments to the output folder.
/// </summary>
public class AttachmentCopier
{
    private readonly string _outputFolder;
    private readonly string? _attachmentSubfolder;
    private readonly Dictionary<string, string> _copiedFiles = new(StringComparer.OrdinalIgnoreCase);

    public AttachmentCopier(string outputFolder, string? attachmentSubfolder = "attachments")
    {
        _outputFolder = outputFolder ?? throw new ArgumentNullException(nameof(outputFolder));
        _attachmentSubfolder = attachmentSubfolder;
    }

    /// <summary>
    /// Copies an attachment to the output folder.
    /// </summary>
    /// <param name="sourcePath">The source file path (can be relative to wiki root)</param>
    /// <param name="wikiRoot">The wiki root folder path</param>
    /// <returns>The resolved path in the output folder</returns>
    public string CopyAttachment(string sourcePath, string wikiRoot)
    {
        if (string.IsNullOrEmpty(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));

        // Normalize the source path
        var normalizedSource = sourcePath.Replace('\\', '/');
        
        // Check if already copied
        if (_copiedFiles.TryGetValue(normalizedSource, out var existingPath))
            return existingPath;

        // Resolve full source path
        var fullSourcePath = Path.IsPathRooted(normalizedSource)
            ? normalizedSource
            : Path.Combine(wikiRoot, normalizedSource);

        if (!File.Exists(fullSourcePath))
        {
            // Return the expected path even if file doesn't exist
            var expectedPath = GetDestinationPath(normalizedSource);
            return expectedPath;
        }

        // Create destination directory
        var destDir = Path.GetDirectoryName(GetDestinationPath(normalizedSource)) ?? _outputFolder;
        Directory.CreateDirectory(destDir);

        // Handle duplicate filenames
        var destPath = GetUniqueDestinationPath(normalizedSource, fullSourcePath);

        // Copy the file
        File.Copy(fullSourcePath, destPath, overwrite: true);
        
        // Track the copy
        _copiedFiles[normalizedSource] = destPath;

        return destPath;
    }

    /// <summary>
    /// Gets the destination path for a file.
    /// </summary>
    private string GetDestinationPath(string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath);
        
        if (string.IsNullOrEmpty(_attachmentSubfolder))
            return Path.Combine(_outputFolder, fileName);

        return Path.Combine(_outputFolder, _attachmentSubfolder, fileName);
    }

    /// <summary>
    /// Gets a unique destination path for duplicate filenames.
    /// </summary>
    private string GetUniqueDestinationPath(string sourcePath, string fullSourcePath)
    {
        var baseDestPath = GetDestinationPath(sourcePath);
        
        if (!File.Exists(baseDestPath))
            return baseDestPath;

        // Get file hash to create unique name
        using var stream = File.OpenRead(fullSourcePath);
        var hash = stream.ComputeSHA256Hash();
        var shortHash = hash[..8];
        
        var dir = Path.GetDirectoryName(baseDestPath) ?? _outputFolder;
        var name = Path.GetFileNameWithoutExtension(baseDestPath);
        var ext = Path.GetExtension(baseDestPath);
        
        return Path.Combine(dir, $"{name}-{shortHash}{ext}");
    }

    /// <summary>
    /// Gets all copied files.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetCopiedFiles() => _copiedFiles;

    /// <summary>
    /// Rewrites image references in content to point to the attachments folder.
    /// </summary>
    /// <param name="content">Content with image references</param>
    /// <param name="wikiRoot">The wiki root folder</param>
    /// <returns>Content with rewritten image paths</returns>
    public string RewriteImagePaths(string content, string wikiRoot)
    {
        var references = ImageReferenceParser.ExtractImages(content).ToList();
        
        if (!references.Any())
            return content;

        // Rewrite in reverse order to maintain position indices
        foreach (var reference in references.OrderByDescending(r => r.Position))
        {
            var destPath = CopyAttachment(reference.OriginalPath, wikiRoot);
            reference.ResolvedPath = destPath;

            // Get relative path from output folder
            var relativePath = GetRelativePath(destPath);
            
            // Replace based on type
            content = reference.Type switch
            {
                ImageReference.ImageReferenceType.Markdown => 
                    Regex.Replace(content, $@"!\[([^\]]*)\]\({EscapeRegex(reference.OriginalPath)}(?:\s+""[^""]*"")?\)",
                        m => $"![{m.Groups[1].Value}]({relativePath})"),
                
                ImageReference.ImageReferenceType.Html =>
                    Regex.Replace(content, $@"<img\s+[^>]*src\s*=\s*[""']" + EscapeRegex(reference.OriginalPath) + @"[""'][^>]*>",
                        $"<img src=\"{relativePath}\" alt=\"{reference.AltText ?? ""}\">"),
                
                ImageReference.ImageReferenceType.Wiki =>
                    Regex.Replace(content, $@"\{{\{{" + EscapeRegex(reference.OriginalPath) + @"(?:\|\|[^\}}]+)?\}}\}}",
                        $"![]({relativePath})"),
                
                _ => content
            };
        }

        return content;
    }

    private string GetRelativePath(string destPath)
    {
        if (string.IsNullOrEmpty(_attachmentSubfolder))
            return Path.GetFileName(destPath);
        
        return Path.Combine(_attachmentSubfolder, Path.GetFileName(destPath)).Replace('\\', '/');
    }

    private static string EscapeRegex(string s)
    {
        return Regex.Escape(s).Replace(@"\\", @"\");
    }
}

// Helper extension
public static class StreamExtensions
{
    public static string ComputeSHA256Hash(this Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
