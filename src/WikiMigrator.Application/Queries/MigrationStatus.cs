using MediatR;

namespace WikiMigrator.Application.Queries;

public class MigrationStatus
{
    public string MigrationId { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int SuccessfulFiles { get; set; }
    public int FailedFiles { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public List<string> Errors { get; set; } = new();
}
