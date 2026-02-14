using MediatR;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Queries;

public record GetMigrationStatusQuery(string MigrationId) : IRequest<MigrationStatus>;
