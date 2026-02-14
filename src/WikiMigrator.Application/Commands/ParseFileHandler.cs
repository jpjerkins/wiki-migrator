using MediatR;
using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Commands;

public class ParseFileHandler : IRequestHandler<ParseFileCommand, IEnumerable<WikiTiddler>>
{
    private readonly IParser _parser;
    private readonly ILogger<ParseFileHandler> _logger;

    public ParseFileHandler(IParser parser, ILogger<ParseFileHandler> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    public async Task<IEnumerable<WikiTiddler>> Handle(ParseFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parsing file: {FilePath}", request.FilePath);
        
        var result = await _parser.ParseAsync(request.FilePath);
        
        _logger.LogInformation("Parsed {Count} tiddlers from {FilePath}", result.Count(), request.FilePath);
        
        return result;
    }
}
