namespace WikiMigrator.Domain.Scanning;

/// <summary>
/// Represents a discovered wiki file with its metadata for processing
/// </summary>
public class WikiFileInfo
{
    public required string FullPath { get; set; }
    public required string RelativePath { get; set; }
    public required string FileName { get; set; }
    public required WikiFileType FileType { get; set; }
    public long Size { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime? TiddlerCreated { get; set; }
    public DateTime? TiddlerModified { get; set; }
}

public enum WikiFileType
{
    Tid,
    Html
}

/// <summary>
/// Result of scanning a directory for wiki files
/// </summary>
public class ScanResult
{
    public List<WikiFileInfo> Files { get; set; } = new();
    public int TotalFiles => Files.Count;
    public int TidFileCount => Files.Count(f => f.FileType == WikiFileType.Tid);
    public int HtmlFileCount => Files.Count(f => f.FileType == WikiFileType.Html);
}

/// <summary>
/// Scans directories recursively to discover TiddlyWiki files (.tid and .html)
/// </summary>
public class WikiDirectoryScanner
{
    private static readonly string[] SupportedExtensions = { ".tid", ".html", ".TID", ".HTML" };

    /// <summary>
    /// Scans a directory recursively for wiki files
    /// </summary>
    /// <param name="directoryPath">The root directory to scan</param>
    /// <returns>Scan result containing all discovered files sorted by modification date</returns>
    public ScanResult Scan(string directoryPath)
    {
        return Scan(directoryPath, string.Empty);
    }

    /// <summary>
    /// Scans a directory recursively for wiki files
    /// </summary>
    /// <param name="directoryPath">The root directory to scan</param>
    /// <param name="basePath">Base path for computing relative paths</param>
    /// <returns>Scan result containing all discovered files sorted by modification date</returns>
    public ScanResult Scan(string directoryPath, string basePath)
    {
        var result = new ScanResult();

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return result;
        }

        if (!Directory.Exists(directoryPath))
        {
            return result;
        }

        var baseDir = string.IsNullOrEmpty(basePath) ? directoryPath : basePath;

        // Discover all .tid and .html files recursively
        foreach (var extension in SupportedExtensions)
        {
            var files = Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var fileInfo = CreateWikiFileInfo(file, baseDir);
                if (fileInfo != null)
                {
                    result.Files.Add(fileInfo);
                }
            }
        }

        // Sort by date (oldest first for processing order) - prefer TiddlerCreated, then TiddlerModified, then file Modified
        result.Files = result.Files
            .OrderBy(f => f.TiddlerCreated ?? f.TiddlerModified ?? f.Modified)
            .ThenBy(f => f.FileName)
            .ToList();

        return result;
    }

    /// <summary>
    /// Gets the processing order for discovered files (by date)
    /// </summary>
    public IEnumerable<WikiFileInfo> GetProcessingOrder(ScanResult scanResult)
    {
        return scanResult.Files.OrderBy(f => f.TiddlerModified ?? f.Modified);
    }

    private WikiFileInfo? CreateWikiFileInfo(string filePath, string basePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLowerInvariant();
            var fileType = extension == ".tid" ? WikiFileType.Tid : WikiFileType.Html;

            var relativePath = Path.GetRelativePath(basePath, filePath);

            var wikiFile = new WikiFileInfo
            {
                FullPath = filePath,
                RelativePath = relativePath,
                FileName = fileInfo.Name,
                FileType = fileType,
                Size = fileInfo.Length,
                Created = fileInfo.CreationTime,
                Modified = fileInfo.LastWriteTime
            };

            // Try to extract tiddler metadata from file content
            ExtractTiddlerMetadata(wikiFile);

            return wikiFile;
        }
        catch
        {
            return null;
        }
    }

    private void ExtractTiddlerMetadata(WikiFileInfo wikiFile)
    {
        try
        {
            if (wikiFile.FileType == WikiFileType.Tid)
            {
                ExtractTidMetadata(wikiFile);
            }
            else if (wikiFile.FileType == WikiFileType.Html)
            {
                ExtractHtmlMetadata(wikiFile);
            }
        }
        catch
        {
            // If we can't extract metadata, use file system dates
        }
    }

    private void ExtractTidMetadata(WikiFileInfo wikiFile)
    {
        try
        {
            var lines = File.ReadLines(wikiFile.FullPath);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool inMetadata = true;
            
            foreach (var line in lines)
            {
                if (inMetadata && line.Contains(':'))
                {
                    var colonIndex = line.IndexOf(':');
                    var key = line[..colonIndex].Trim();
                    var value = line[(colonIndex + 1)..].Trim();
                    
                    if (key.Equals("created", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("modified", StringComparison.OrdinalIgnoreCase))
                    {
                        metadata[key] = value;
                    }
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line marks end of metadata
                    continue;
                }
                else
                {
                    // Non-empty, non-metadata line - content starts
                    inMetadata = false;
                }

                // Only check first few lines for metadata
                if (!inMetadata) break;
            }

            if (metadata.TryGetValue("created", out var createdStr) && !string.IsNullOrEmpty(createdStr))
            {
                if (TryParseTiddlerDate(createdStr, out var created))
                {
                    wikiFile.TiddlerCreated = created;
                }
            }

            if (metadata.TryGetValue("modified", out var modifiedStr) && !string.IsNullOrEmpty(modifiedStr))
            {
                if (TryParseTiddlerDate(modifiedStr, out var modified))
                {
                    wikiFile.TiddlerModified = modified;
                }
            }
        }
        catch
        {
            // Ignore metadata extraction errors
        }
    }

    private void ExtractHtmlMetadata(WikiFileInfo wikiFile)
    {
        try
        {
            var html = File.ReadAllText(wikiFile.FullPath);
            
            // Try to extract from common TiddlyWiki attributes
            var createdMatch = System.Text.RegularExpressions.Regex.Match(
                html, 
                @"data-created=""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            var modifiedMatch = System.Text.RegularExpressions.Regex.Match(
                html, 
                @"data-modified=""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (createdMatch.Success && DateTime.TryParse(createdMatch.Groups[1].Value, out var created))
            {
                wikiFile.TiddlerCreated = created;
            }
            
            if (modifiedMatch.Success && DateTime.TryParse(modifiedMatch.Groups[1].Value, out var modified))
            {
                wikiFile.TiddlerModified = modified;
            }
        }
        catch
        {
            // Ignore metadata extraction errors
        }
    }

    private bool TryParseTiddlerDate(string dateStr, out DateTime result)
    {
        result = default;
        
        // Try standard DateTime parsing first
        if (DateTime.TryParse(dateStr, out result))
        {
            return true;
        }
        
        // Try TiddlyWiki format: YYYYMMDDHHmm
        if (dateStr.Length >= 8 && dateStr.All(char.IsDigit))
        {
            if (dateStr.Length >= 12)
            {
                // Full format: YYYYMMDDHHmm
                if (DateTime.TryParseExact(dateStr[..12], "yyyyMMddHHmm", null, 
                    System.Globalization.DateTimeStyles.None, out result))
                {
                    return true;
                }
            }
            
            // Try date only: YYYYMMDD
            if (DateTime.TryParseExact(dateStr[..8], "yyyyMMdd", null, 
                System.Globalization.DateTimeStyles.None, out result))
            {
                return true;
            }
        }
        
        return false;
    }
}
