namespace WikiMigrator.Application.Services;

/// <summary>
/// Represents a PARA folder type for categorizing notes.
/// </summary>
public enum ParaFolderType
{
    None,
    Project,
    Area,
    ResourceTopic
}

/// <summary>
/// Represents a task status tag.
/// </summary>
public enum TaskStatus
{
    None,
    Task,
    Done,
    Parked
}

/// <summary>
/// Service for classifying notes based on PARA tags.
/// </summary>
public class ParaTagClassifier
{
    private static readonly HashSet<string> ProjectTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "project"
    };

    private static readonly HashSet<string> AreaTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area"
    };

    private static readonly HashSet<string> ResourceTopicTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "resourcetopic",
        "resource",
        "topic"
    };

    private static readonly HashSet<string> StatusTaskTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "task"
    };

    private static readonly HashSet<string> StatusDoneTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "done",
        "completed"
    };

    private static readonly HashSet<string> StatusParkedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "parked",
        "someday"
    };

    /// <summary>
    /// Determines the PARA folder type based on tags.
    /// </summary>
    public ParaFolderType ClassifyParaFolder(IEnumerable<string> tags)
    {
        var tagSet = new HashSet<string>(tags.Select(t => t.Trim().ToLowerInvariant()));
        
        if (tagSet.Overlaps(ProjectTags))
            return ParaFolderType.Project;
        
        if (tagSet.Overlaps(AreaTags))
            return ParaFolderType.Area;
        
        if (tagSet.Overlaps(ResourceTopicTags))
            return ParaFolderType.ResourceTopic;
        
        return ParaFolderType.None;
    }

    /// <summary>
    /// Determines the task status based on tags.
    /// </summary>
    public TaskStatus GetTaskStatus(IEnumerable<string> tags)
    {
        var tagSet = new HashSet<string>(tags.Select(t => t.Trim().ToLowerInvariant()));
        
        // Check Done first (if done, it's not an active task)
        if (tagSet.Overlaps(StatusDoneTags))
            return TaskStatus.Done;
        
        // Check Parked
        if (tagSet.Overlaps(StatusParkedTags))
            return TaskStatus.Parked;
        
        // Check Task
        if (tagSet.Overlaps(StatusTaskTags))
            return TaskStatus.Task;
        
        return TaskStatus.None;
    }

    /// <summary>
    /// Determines if a note is a task source (has Task tag but not Done or Parked).
    /// </summary>
    public bool IsTaskSource(IEnumerable<string> tags)
    {
        var status = GetTaskStatus(tags);
        return status == TaskStatus.Task;
    }

    /// <summary>
    /// Gets the target PARA folder path for a given folder type.
    /// </summary>
    public string GetParaFolderPath(ParaFolderType folderType)
    {
        return folderType switch
        {
            ParaFolderType.Project => "Notes/1 Projects",
            ParaFolderType.Area => "Notes/2 Areas",
            ParaFolderType.ResourceTopic => "Notes/3 Resources",
            _ => "Notes/Orphans"
        };
    }
}
