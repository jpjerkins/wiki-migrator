using WikiMigrator.Application.Services;
using TaskStatus = WikiMigrator.Application.Services.TaskStatus;

namespace WikiMigrator.Tests;

public class ParaTagClassifierTests
{
    private readonly ParaTagClassifier _classifier;

    public ParaTagClassifierTests()
    {
        _classifier = new ParaTagClassifier();
    }

    #region ClassifyParaFolder Tests

    [Fact]
    public void ClassifyParaFolder_WithProjectTag_ReturnsProject()
    {
        // Arrange
        var tags = new[] { "some_tag", "Project", "another_tag" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.Project, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithProjectTagCaseInsensitive_ReturnsProject()
    {
        // Arrange
        var tags = new[] { "PROJECT", "project", "Project" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.Project, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithAreaTag_ReturnsArea()
    {
        // Arrange
        var tags = new[] { "Area", "personal" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.Area, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithResourceTopicTag_ReturnsResourceTopic()
    {
        // Arrange
        var tags = new[] { "ResourceTopic", "notes" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.ResourceTopic, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithResourceAlias_ReturnsResourceTopic()
    {
        // Arrange
        var tags = new[] { "resource", "learning" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.ResourceTopic, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithNoParaTags_ReturnsNone()
    {
        // Arrange
        var tags = new[] { "personal", "notes", "ideas" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.None, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithEmptyTags_ReturnsNone()
    {
        // Arrange
        var tags = Array.Empty<string>();

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert
        Assert.Equal(ParaFolderType.None, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithMultipleParaTags_PriorityProjectOverArea()
    {
        // Arrange - has both Project and Area
        var tags = new[] { "Project", "Area", "work" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert - Project takes priority
        Assert.Equal(ParaFolderType.Project, result);
    }

    #endregion

    #region GetTaskStatus Tests

    [Fact]
    public void GetTaskStatus_WithTaskTag_ReturnsTask()
    {
        // Arrange
        var tags = new[] { "Task", "work" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Task, result);
    }

    [Fact]
    public void GetTaskStatus_WithDoneTag_ReturnsDone()
    {
        // Arrange
        var tags = new[] { "Done", "work" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Done, result);
    }

    [Fact]
    public void GetTaskStatus_WithCompletedAlias_ReturnsDone()
    {
        // Arrange
        var tags = new[] { "completed", "work" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Done, result);
    }

    [Fact]
    public void GetTaskStatus_WithParkedTag_ReturnsParked()
    {
        // Arrange
        var tags = new[] { "Parked", "ideas" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Parked, result);
    }

    [Fact]
    public void GetTaskStatus_WithSomedayAlias_ReturnsParked()
    {
        // Arrange
        var tags = new[] { "someday", "ideas" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Parked, result);
    }

    [Fact]
    public void GetTaskStatus_WithNoStatusTags_ReturnsNone()
    {
        // Arrange
        var tags = new[] { "personal", "notes" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.None, result);
    }

    [Fact]
    public void GetTaskStatus_WithEmptyTags_ReturnsNone()
    {
        // Arrange
        var tags = Array.Empty<string>();

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.None, result);
    }

    [Fact]
    public void GetTaskStatus_WithDoneAndTask_ReturnsDone()
    {
        // Arrange - Done takes priority
        var tags = new[] { "Task", "Done" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert
        Assert.Equal(TaskStatus.Done, result);
    }

    #endregion

    #region IsTaskSource Tests

    [Fact]
    public void IsTaskSource_WithTaskOnly_ReturnsTrue()
    {
        // Arrange
        var tags = new[] { "Task", "Project" };

        // Act
        var result = _classifier.IsTaskSource(tags);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTaskSource_WithTaskAndDone_ReturnsFalse()
    {
        // Arrange
        var tags = new[] { "Task", "Done" };

        // Act
        var result = _classifier.IsTaskSource(tags);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTaskSource_WithTaskAndParked_ReturnsFalse()
    {
        // Arrange
        var tags = new[] { "Task", "Parked" };

        // Act
        var result = _classifier.IsTaskSource(tags);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTaskSource_WithNoTaskTag_ReturnsFalse()
    {
        // Arrange
        var tags = new[] { "Project", "personal" };

        // Act
        var result = _classifier.IsTaskSource(tags);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Combined Project + Task Tests

    [Fact]
    public void ClassifyParaFolder_WithProjectAndTaskTags_ReturnsProject()
    {
        // Arrange - note has both Project and Task tags
        var tags = new[] { "Project", "Task", "work" };

        // Act
        var result = _classifier.ClassifyParaFolder(tags);

        // Assert - should classify as Project
        Assert.Equal(ParaFolderType.Project, result);
    }

    [Fact]
    public void IsTaskSource_WithProjectAndTaskTags_ReturnsTrue()
    {
        // Arrange - note has both Project and Task tags
        var tags = new[] { "Project", "Task", "work" };

        // Act
        var result = _classifier.IsTaskSource(tags);

        // Assert - should be a task source (active task)
        Assert.True(result);
    }

    [Fact]
    public void GetTaskStatus_WithProjectAndTaskTags_ReturnsTask()
    {
        // Arrange - note has both Project and Task tags
        var tags = new[] { "Project", "Task", "work" };

        // Act
        var result = _classifier.GetTaskStatus(tags);

        // Assert - should have Task status
        Assert.Equal(TaskStatus.Task, result);
    }

    [Fact]
    public void ClassifyParaFolder_WithProjectTaskAndDone_ReturnsProject()
    {
        // Arrange - completed project task
        var tags = new[] { "Project", "Task", "Done" };

        // Act
        var folderResult = _classifier.ClassifyParaFolder(tags);
        var taskResult = _classifier.IsTaskSource(tags);

        // Assert - should be Project folder but NOT a task source (it's done)
        Assert.Equal(ParaFolderType.Project, folderResult);
        Assert.False(taskResult);
    }

    #endregion

    #region GetParaFolderPath Tests

    [Fact]
    public void GetParaFolderPath_WithProject_ReturnsProjectsPath()
    {
        // Act
        var result = _classifier.GetParaFolderPath(ParaFolderType.Project);

        // Assert
        Assert.Equal("Notes/1 Projects", result);
    }

    [Fact]
    public void GetParaFolderPath_WithArea_ReturnsAreasPath()
    {
        // Act
        var result = _classifier.GetParaFolderPath(ParaFolderType.Area);

        // Assert
        Assert.Equal("Notes/2 Areas", result);
    }

    [Fact]
    public void GetParaFolderPath_WithResourceTopic_ReturnsResourcesPath()
    {
        // Act
        var result = _classifier.GetParaFolderPath(ParaFolderType.ResourceTopic);

        // Assert
        Assert.Equal("Notes/3 Resources", result);
    }

    [Fact]
    public void GetParaFolderPath_WithNone_ReturnsOrphansPath()
    {
        // Act
        var result = _classifier.GetParaFolderPath(ParaFolderType.None);

        // Assert
        Assert.Equal("Notes/Orphans", result);
    }

    #endregion
}
