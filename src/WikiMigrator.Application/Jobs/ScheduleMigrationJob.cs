using Hangfire;

namespace WikiMigrator.Application.Jobs;

/// <summary>
/// Provides job scheduling support for Hangfire
/// </summary>
public static class ScheduleMigrationJob
{
    /// <summary>
    /// Schedule a one-time migration job to run immediately
    /// </summary>
    public static string Enqueue(IBackgroundJobClient client, string filePath)
    {
        return client.Enqueue<MigrationJob>(job => job.ExecuteAsync(filePath, CancellationToken.None));
    }

    /// <summary>
    /// Schedule a batch migration job to run immediately
    /// </summary>
    public static string EnqueueBatch(
        IBackgroundJobClient client,
        string inputFolder,
        string outputFolder,
        string filePattern = "*.md",
        bool recursive = true)
    {
        return client.Enqueue<MigrationJob>(job => 
            job.ExecuteBatchAsync(inputFolder, outputFolder, filePattern, recursive, CancellationToken.None));
    }

    /// <summary>
    /// Schedule a one-time migration job to run after a delay
    /// </summary>
    public static string Schedule(
        IBackgroundJobClient client,
        string filePath,
        TimeSpan delay)
    {
        return client.Schedule<MigrationJob>(job => job.ExecuteAsync(filePath, CancellationToken.None), delay);
    }

    /// <summary>
    /// Schedule a recurring batch migration job
    /// </summary>
    public static string AddRecurring(
        IRecurringJobManager manager,
        string jobId,
        string inputFolder,
        string outputFolder,
        string cronExpression,
        string filePattern = "*.md",
        bool recursive = true)
    {
        manager.AddOrUpdate<MigrationJob>(
            jobId,
            job => job.ExecuteBatchAsync(inputFolder, outputFolder, filePattern, recursive, CancellationToken.None),
            cronExpression);

        return jobId;
    }

    /// <summary>
    /// Remove a recurring job
    /// </summary>
    public static void RemoveRecurring(IRecurringJobManager manager, string jobId)
    {
        manager.RemoveIfExists(jobId);
    }
}
