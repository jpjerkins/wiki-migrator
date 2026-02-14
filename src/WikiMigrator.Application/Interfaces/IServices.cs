using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Interfaces;

public interface IParser
{
    Task<IEnumerable<WikiTiddler>> ParseAsync(string input);
}

public interface IParserFactory
{
    IParser? GetParser(string filePath);
}

public interface IConverter
{
    Task<string> ConvertAsync(WikiTiddler tiddler);
}

public interface IMigrationService
{
    Task<MigrationResult> MigrateAsync(string sourcePath, string targetPath);
}
