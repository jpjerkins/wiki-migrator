using WikiMigrator.Application.Interfaces;

namespace WikiMigrator.Infrastructure.Parsers;

/// <summary>
/// Factory for creating the appropriate parser based on file extension and content analysis.
/// </summary>
public class ParserFactory : IParserFactory
{
    private readonly Dictionary<string, Func<IParser>> _parsers;

    public ParserFactory()
    {
        _parsers = new Dictionary<string, Func<IParser>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".tid", () => new TidFileParser() },
            { ".html", () => new TiddlyWiki5JsonParser() }  // Default to JSON parser for .html
        };
    }

    public ParserFactory(IEnumerable<IParser> parsers)
    {
        _parsers = new Dictionary<string, Func<IParser>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var parser in parsers)
        {
            // Register based on parser type name
            var parserName = parser.GetType().Name;
            if (parserName.Contains("Tid", StringComparison.OrdinalIgnoreCase))
            {
                _parsers[".tid"] = () => parser;
            }
            else if (parserName.Contains("Html", StringComparison.OrdinalIgnoreCase))
            {
                _parsers[".html"] = () => parser;
            }
            else if (parserName.Contains("TiddlyWiki5", StringComparison.OrdinalIgnoreCase))
            {
                // JSON parser takes precedence for .html files
                _parsers[".html"] = () => parser;
            }
        }
    }

    public IParser? GetParser(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var extension = Path.GetExtension(filePath);
        
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            // For HTML files, detect if it's TiddlyWiki 5.x JSON format or older HTML format
            return DetectHtmlParser(filePath);
        }

        if (_parsers.TryGetValue(extension, out var parserFactory))
        {
            return parserFactory();
        }

        return null;
    }

    private IParser DetectHtmlParser(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            
            // Check for TiddlyWiki 5.x JSON format
            if (IsTiddlyWiki5JsonFormat(content))
            {
                return new TiddlyWiki5JsonParser();
            }

            // Fall back to legacy HTML parser
            return new HtmlWikiParser();
        }
        catch
        {
            // Default to JSON parser if we can't read the file
            return new TiddlyWiki5JsonParser();
        }
    }

    private bool IsTiddlyWiki5JsonFormat(string content)
    {
        // Check for TiddlyWiki 5.x JSON script tag
        // Format: <script class="tiddlywiki-tiddler-store" type="application/json">
        return content.Contains("type=\"application/json\"", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("type='application/json'", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("tiddlywiki-tiddler-store", StringComparison.OrdinalIgnoreCase);
    }
}
