using System.Text.RegularExpressions;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Represents a parsed macro definition.
/// </summary>
public class MacroDefinition
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();
    public string Body { get; init; } = string.Empty;
    public int Position { get; init; }
}

/// <summary>
/// Represents a parsed transclusion reference.
/// </summary>
public class TransclusionReference
{
    public string Target { get; init; } = string.Empty;
    public string? Variable { get; init; }
    public int Position { get; init; }
}

/// <summary>
/// Service for parsing advanced wiki syntax (macros, transclusions, code blocks, etc.)
/// </summary>
public class AdvancedWikiParser
{
    // Macro definition: \define name(params) body
    private static readonly Regex MacroDefinitionRegex = new(
        @"\\define\s+(\w+)(?:\(([^)]*)\))?\s*(.*)$", 
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Transclusion: {{TiddlerName}} or {{TiddlerName||field}}
    private static readonly Regex TransclusionRegex = new(
        @"\{\{([^}|]+)(?:\|\|([^\}]+))?\}\}",
        RegexOptions.Compiled);

    // Fenced code block: ```language
    private static readonly Regex FencedCodeBlockRegex = new(
        @"```(\w*)\n([\s\S]*?)```",
        RegexOptions.Compiled);

    // Indented code block (4 spaces or more at start of line)
    private static readonly Regex IndentedCodeBlockRegex = new(
        @"^(\ {4,})(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // HTML table row
    private static readonly Regex HtmlTableRowRegex = new(
        @"<tr>([\s\S]*?)</tr>",
        RegexOptions.Compiled);

    // HTML table cell
    private static readonly Regex HtmlTableCellRegex = new(
        @"<t[dh][^>]*>([\s\S]*?)</t[dh]>",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts all macro definitions from content.
    /// </summary>
    public static IEnumerable<MacroDefinition> ExtractMacroDefinitions(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<MacroDefinition>();

        var macros = new List<MacroDefinition>();
        
        foreach (Match match in MacroDefinitionRegex.Matches(content))
        {
            var name = match.Groups[1].Value.Trim();
            var paramsStr = match.Groups[2].Value;
            var body = match.Groups[3].Value.Trim();

            var parameters = string.IsNullOrEmpty(paramsStr)
                ? Array.Empty<string>()
                : paramsStr.Split(',').Select(p => p.Trim()).ToArray();

            macros.Add(new MacroDefinition
            {
                Name = name,
                Parameters = parameters,
                Body = body,
                Position = match.Index
            });
        }

        return macros;
    }

    /// <summary>
    /// Extracts all transclusion references from content.
    /// </summary>
    public static IEnumerable<TransclusionReference> ExtractTransclusions(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<TransclusionReference>();

        var references = new List<TransclusionReference>();

        foreach (Match match in TransclusionRegex.Matches(content))
        {
            var target = match.Groups[1].Value.Trim();
            var variable = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            references.Add(new TransclusionReference
            {
                Target = target,
                Variable = variable,
                Position = match.Index
            });
        }

        return references;
    }

    /// <summary>
    /// Checks if content contains a fenced code block.
    /// </summary>
    public static bool HasFencedCodeBlock(string content)
    {
        return FencedCodeBlockRegex.IsMatch(content);
    }

    /// <summary>
    /// Extracts all fenced code blocks from content.
    /// </summary>
    public static IEnumerable<(string Language, string Code)> ExtractFencedCodeBlocks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<(string, string)>();

        var blocks = new List<(string, string)>();
        
        foreach (Match match in FencedCodeBlockRegex.Matches(content))
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value;
            blocks.Add((language, code));
        }

        return blocks;
    }

    /// <summary>
    /// Detects if content has nested lists.
    /// </summary>
    public static int GetMaxListDepth(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var maxDepth = 0;
        var currentDepth = 0;
        
        // Match unordered list items: - or * at start of line
        var listRegex = new Regex(@"^(\s*)([-*]|\d+\.)\s+", RegexOptions.Multiline);
        
        foreach (Match match in listRegex.Matches(content))
        {
            var indent = match.Groups[1].Value;
            // Each 2 spaces = one level
            var depth = (indent.Length / 2) + 1;
            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth;
    }

    /// <summary>
    /// Parses an HTML wiki table and converts to markdown.
    /// </summary>
    public static string ConvertHtmlTableToMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Find all tables
        var tableRegex = new Regex(@"<table[^>]*>([\s\S]*?)</table>", RegexOptions.Compiled);
        
        return tableRegex.Replace(content, match =>
        {
            var tableContent = match.Groups[1].Value;
            return ConvertTableToMarkdown(tableContent);
        });
    }

    private static string ConvertTableToMarkdown(string tableContent)
    {
        var rows = new List<string[]>();
        
        foreach (Match rowMatch in HtmlTableRowRegex.Matches(tableContent))
        {
            var rowContent = rowMatch.Groups[1].Value;
            var cells = new List<string>();
            
            foreach (Match cellMatch in HtmlTableCellRegex.Matches(rowContent))
            {
                var cell = cellMatch.Groups[1].Value.Trim();
                // Remove any nested markup that would break markdown
                cell = cell.Replace("|", "\\|");
                cells.Add(cell);
            }
            
            if (cells.Any())
                rows.Add(cells.ToArray());
        }

        if (!rows.Any())
            return string.Empty;

        // Build markdown table
        var md = new System.Text.StringBuilder();
        
        // Header row
        md.AppendLine(string.Join(" | ", rows[0]));
        
        // Separator row
        md.AppendLine(string.Join(" | ", rows[0].Select(_ => "---")));
        
        // Data rows
        for (int i = 1; i < rows.Count; i++)
        {
            // Pad row if needed
            var row = rows[i];
            if (row.Length < rows[0].Length)
            {
                var padded = new string[rows[0].Length];
                Array.Copy(row, padded, row.Length);
                for (int j = row.Length; j < rows[0].Length; j++)
                    padded[j] = "";
                row = padded;
            }
            md.AppendLine(string.Join(" | ", row));
        }

        return md.ToString();
    }

    /// <summary>
    /// Converts transclusion syntax to Obsidian embed syntax.
    /// </summary>
    public static string ConvertTransclusionsToObsidian(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // {{TiddlerName}} -> ![[tiddler-name]]
        // {{TiddlerName||field}} -> ![[tiddler-name]] (field reference is lost)
        return TransclusionRegex.Replace(content, match =>
        {
            var target = match.Groups[1].Value.Trim();
            var sanitized = LinkResolver.SanitizeFilename(target);
            return $"![[{sanitized}]]";
        });
    }

    /// <summary>
    /// Resolves macro calls in content.
    /// </summary>
    public static string ResolveMacros(string content, Dictionary<string, MacroDefinition> macros)
    {
        if (string.IsNullOrEmpty(content) || macros == null || !macros.Any())
            return content;

        // Match macro calls: <<macroName param1 param2>>
        var macroCallRegex = new Regex(@"<<\s*(\w+)(?:\s+(.*?))?>>", RegexOptions.Compiled);
        
        return macroCallRegex.Replace(content, match =>
        {
            var name = match.Groups[1].Value;
            var argsStr = match.Groups[2].Success ? match.Groups[2].Value : "";
            
            if (!macros.TryGetValue(name, out var macro))
                return match.Value; // Keep original if macro not found

            // Simple argument substitution
            var args = string.IsNullOrEmpty(argsStr) 
                ? Array.Empty<string>() 
                : argsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var result = macro.Body;
            
            // Replace $1, $2, etc. with arguments
            for (int i = 0; i < args.Length; i++)
            {
                result = result.Replace($"${i + 1}", args[i]);
            }
            
            return result;
        });
    }
}
