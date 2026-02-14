namespace WikiMigrator.Domain.ValueObjects;

public class TiddlerMetadata
{
    public string Author { get; set; } = string.Empty;
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Wiki { get; set; } = string.Empty;
}

public class TagCollection
{
    private readonly List<string> _tags = new();
    
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    
    public void Add(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag))
        {
            _tags.Add(tag);
        }
    }
    
    public void AddRange(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            Add(tag);
        }
    }
    
    public bool Contains(string tag) => _tags.Contains(tag);
    
    public int Count => _tags.Count;
}
