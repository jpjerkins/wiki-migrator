using WikiMigrator.Application.Interfaces;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Infrastructure.Converters;

public class TiddlerConverter : IConverter
{
    public Task<string> ConvertAsync(WikiTiddler tiddler)
    {
        return Task.FromResult(string.Empty);
    }
}
