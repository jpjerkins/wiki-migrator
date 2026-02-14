using WikiMigrator.Application.Interfaces;

namespace WikiMigrator.Application.Services;

public class TagProcessor : ITagProcessor
{
    /// <inheritdoc/>
    public List<string> ParseTags(string tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return new List<string>();
        }

        // Split by comma and clean up each tag
        var parsedTags = tags
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        return parsedTags;
    }

    /// <inheritdoc/>
    public string GenerateYamlTags(IEnumerable<string> tags)
    {
        if (tags == null || !tags.Any())
        {
            return "tags: []";
        }

        var tagList = tags.ToList();
        
        // If only one tag, keep it inline
        if (tagList.Count == 1)
        {
            return $"tags: [{tagList[0]}]";
        }

        // Multiple tags - use multiline format
        var yaml = new System.Text.StringBuilder();
        yaml.AppendLine("tags:");
        foreach (var tag in tagList)
        {
            yaml.AppendLine($"  - {tag}");
        }

        return yaml.ToString().TrimEnd();
    }

    /// <inheritdoc/>
    public string ProcessTags(string tags)
    {
        var parsedTags = ParseTags(tags);
        
        // Handle tag hierarchy - expand parent/child tags with /
        var expandedTags = new List<string>();
        
        foreach (var tag in parsedTags)
        {
            if (tag.Contains('/'))
            {
                // Tag has hierarchy like "parent/child"
                // Keep both the full path and the parent for better categorization
                var parts = tag.Split('/');
                
                // Add the full hierarchy path
                expandedTags.Add(tag);
                
                // Add parent tags (but not duplicates)
                var currentPath = "";
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) 
                        ? parts[i] 
                        : $"{currentPath}/{parts[i]}";
                    
                    if (!expandedTags.Contains(currentPath))
                    {
                        expandedTags.Add(currentPath);
                    }
                }
            }
            else
            {
                expandedTags.Add(tag);
            }
        }

        return GenerateYamlTags(expandedTags);
    }
}
