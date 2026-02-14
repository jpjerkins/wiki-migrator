using WikiMigrator.Application.Interfaces;

namespace WikiMigrator.Infrastructure.Parsers;

/// <summary>
/// Factory for creating the appropriate parser based on file extension.
/// </summary>
public class ParserFactory : IParserFactory
{
    private readonly Dictionary<string, Func<IParser>> _parsers;

    public ParserFactory()
    {
        _parsers = new Dictionary<string, Func<IParser>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".tid", () => new TidFileParser() },
            { ".html", () => new HtmlWikiParser() }
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

        if (_parsers.TryGetValue(extension, out var parserFactory))
        {
            return parserFactory();
        }

        return null;
    }
}
