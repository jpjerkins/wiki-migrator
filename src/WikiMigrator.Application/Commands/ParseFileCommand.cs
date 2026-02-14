using MediatR;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Commands;

public record ParseFileCommand(string FilePath) : IRequest<IEnumerable<WikiTiddler>>;
