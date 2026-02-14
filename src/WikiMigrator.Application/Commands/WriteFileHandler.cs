using MediatR;
using Microsoft.Extensions.Logging;

namespace WikiMigrator.Application.Commands;

public class WriteFileHandler : IRequestHandler<WriteFileCommand, bool>
{
    private readonly ILogger<WriteFileHandler> _logger;

    public WriteFileHandler(ILogger<WriteFileHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Handle(WriteFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Writing file: {FilePath}", request.FilePath);
            
            var directory = Path.GetDirectoryName(request.FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(request.FilePath, request.Content, cancellationToken);
            
            _logger.LogInformation("Successfully wrote file: {FilePath}", request.FilePath);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file: {FilePath}", request.FilePath);
            return false;
        }
    }
}
