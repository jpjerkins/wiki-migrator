namespace WikiMigrator.Application.Interfaces;

public interface ITagProcessor
{
    /// <summary>
    /// Parses comma-separated tags from wiki format.
    /// </summary>
    /// <param name="tags">Comma-separated tag string from wiki.</param>
    /// <returns>List of parsed tags.</returns>
    List<string> ParseTags(string tags);

    /// <summary>
    /// Generates YAML frontmatter tags array from parsed tags.
    /// </summary>
    /// <param name="tags">List of tags.</param>
    /// <returns>YAML-formatted tags array.</returns>
    string GenerateYamlTags(IEnumerable<string> tags);

    /// <summary>
    /// Processes tags and generates YAML frontmatter, handling hierarchy.
    /// </summary>
    /// <param name="tags">Comma-separated tag string from wiki.</param>
    /// <returns>YAML-formatted tags array.</returns>
    string ProcessTags(string tags);
}
