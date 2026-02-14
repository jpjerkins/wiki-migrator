using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;
using System.Text.RegularExpressions;

namespace WikiMigrator.Application.Services;

public class WikiSyntaxConverter : IConverter
{
    public Task<string> ConvertAsync(WikiTiddler tiddler)
    {
        if (tiddler == null)
            throw new ArgumentNullException(nameof(tiddler));

        var markdown = tiddler.Content;

        // Convert in order: code first, tables, lists (before headings!), headings, formatting
        // Lists must come before headings because # is used for both ordered lists and headings in different contexts
        
        markdown = ConvertCodeBlocks(markdown);
        markdown = ConvertTables(markdown);
        markdown = ConvertLists(markdown);
        markdown = ConvertHeadings(markdown);
        markdown = ConvertBold(markdown);
        markdown = ConvertItalic(markdown);

        return Task.FromResult(markdown);
    }

    private string ConvertCodeBlocks(string content)
    {
        // Convert {{{code}}} to ```code```
        return Regex.Replace(content, @"\{\{\{(.+?)\}\}\}", match => {
            var code = match.Groups[1].Value;
            return $"`{code}`";
        });
    }

    private string ConvertTables(string content)
    {
        // Wiki table format:
        // |!Header1|!Header2|
        // |cell1|cell2|
        
        var lines = content.Split('\n');
        var result = new List<string>();
        var inTable = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check if line is a wiki table row (starts and ends with |)
            if (trimmedLine.StartsWith("|") && trimmedLine.EndsWith("|") && trimmedLine.Contains("|"))
            {
                if (!inTable)
                {
                    inTable = true;
                }

                // Check if this is a header row (contains |! or |~)
                var isHeader = trimmedLine.Contains("|!") || trimmedLine.Contains("|~");
                
                // Parse cells
                var cells = trimmedLine
                    .Trim('|')
                    .Split('|')
                    .Select(c => c.Trim())
                    .ToList();

                // Remove formatting markers from header cells
                if (isHeader)
                {
                    cells = cells.Select(c => 
                        c.StartsWith("!") ? c.Substring(1) : 
                        c.StartsWith("~") ? c.Substring(1) : c
                    ).ToList();
                }

                var mdCells = cells.Select(c => ConvertBold(ConvertItalic(c)));
                
                if (isHeader && !result.Any(r => r.StartsWith("|")))
                {
                    // Add header separator after header row
                    result.Add("| " + string.Join(" | ", mdCells) + " |");
                    result.Add("| " + string.Join(" | ", cells.Select(_ => "---")) + " |");
                }
                else
                {
                    result.Add("| " + string.Join(" | ", mdCells) + " |");
                }
            }
            else
            {
                if (inTable)
                {
                    // Add blank line to close table
                    result.Add("");
                }
                inTable = false;
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }

    private string ConvertBold(string content)
    {
        // Convert ''text'' to **text**
        return Regex.Replace(content, @"''(.*?)''", "**$1**");
    }

    private string ConvertItalic(string content)
    {
        // Convert //text// to *text* (but not inside URLs or already converted)
        // Use negative lookbehind to avoid matching already converted markdown
        return Regex.Replace(content, @"(?<![\*])//(.*?)//(?![\*])", "*$1*");
    }

    private string ConvertHeadings(string content)
    {
        var lines = content.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            
            if (trimmed.StartsWith("!!!"))
            {
                result.Add("### " + trimmed.Substring(3).TrimStart());
            }
            else if (trimmed.StartsWith("!!"))
            {
                result.Add("## " + trimmed.Substring(2).TrimStart());
            }
            else if (trimmed.StartsWith("!"))
            {
                result.Add("# " + trimmed.Substring(1).TrimStart());
            }
            else
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }

    private string ConvertLists(string content)
    {
        var lines = content.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            
            // Unordered list: *text -> - text
            if (trimmed.StartsWith("*"))
            {
                var listContent = trimmed.Substring(1).TrimStart();
                result.Add("- " + ConvertBold(ConvertItalic(listContent)));
            }
            // Ordered list: #text -> 1. text (but not ##, ### etc - those are headings)
            else if (trimmed.StartsWith("#") && !trimmed.StartsWith("##"))
            {
                var listContent = trimmed.Substring(1).TrimStart();
                result.Add("1. " + ConvertBold(ConvertItalic(listContent)));
            }
            else
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }
}
