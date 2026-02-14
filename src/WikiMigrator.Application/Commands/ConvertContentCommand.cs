using MediatR;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Commands;

public record ConvertContentCommand(WikiTiddler Tiddler) : IRequest<string>;
