using MediatR;

namespace WikiMigrator.Application.Commands;

public record WriteFileCommand(string FilePath, string Content) : IRequest<bool>;
