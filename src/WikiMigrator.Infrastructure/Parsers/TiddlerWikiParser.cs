using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Infrastructure.Parsers;

public class TiddlerWikiParser : IParser
{
    private readonly ILogger<TiddlerWikiParser> _logger;

    public TiddlerWikiParser(ILogger<TiddlerWikiParser> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<WikiTiddler>> ParseAsync(string input)
    {
        _logger.LogDebug("Parsing tiddler wiki input");
        
        var result = Enumerable.Empty<WikiTiddler>();
        
        _logger.LogDebug("Parsed {Count} tiddlers", result.Count());
        
        return await Task.FromResult(result);
    }
}
