using MediatR;
using Microsoft.Extensions.Logging;
using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Commands;

public class ConvertContentHandler : IRequestHandler<ConvertContentCommand, string>
{
    private readonly IConverter _converter;
    private readonly ILogger<ConvertContentHandler> _logger;

    public ConvertContentHandler(IConverter converter, ILogger<ConvertContentHandler> logger)
    {
        _converter = converter;
        _logger = logger;
    }

    public async Task<string> Handle(ConvertContentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Converting content for tiddler: {Title}", request.Tiddler.Title);
        
        var result = await _converter.ConvertAsync(request.Tiddler);
        
        _logger.LogDebug("Converted content for tiddler: {Title}", request.Tiddler.Title);
        
        return result;
    }
}
