using System.Text.Json.Serialization;
using WikiMigrator.Domain.Entities;

namespace WikiMigrator.Application.Services;

/// <summary>
/// Represents a graph of internal wiki links between tiddlers.
/// Tracks all links (edges) between tiddlers (nodes) for backlink resolution.
/// </summary>
public class LinkGraph
{
    // Adjacency list: source title -> set of target titles
    private readonly Dictionary<string, HashSet<string>> _outgoingLinks = new(StringComparer.OrdinalIgnoreCase);
    // Reverse adjacency: target title -> set of source titles (backlinks)
    private readonly Dictionary<string, HashSet<string>> _incomingLinks = new(StringComparer.OrdinalIgnoreCase);
    // All nodes in the graph
    private readonly HashSet<string> _nodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a link from source to target in the graph.
    /// </summary>
    /// <param name="source">The source tiddler title</param>
    /// <param name="target">The target tiddler title</param>
    public void AddLink(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return;

        // Add nodes
        _nodes.Add(source);
        _nodes.Add(target);

        // Add outgoing link
        if (!_outgoingLinks.TryGetValue(source, out var outgoing))
        {
            outgoing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _outgoingLinks[source] = outgoing;
        }
        outgoing.Add(target);

        // Add incoming link (backlink)
        if (!_incomingLinks.TryGetValue(target, out var incoming))
        {
            incoming = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _incomingLinks[target] = incoming;
        }
        incoming.Add(source);
    }

    /// <summary>
    /// Adds multiple links from a source tiddler.
    /// </summary>
    /// <param name="source">The source tiddler title</param>
    /// <param name="targets">The target tiddler titles</param>
    public void AddLinks(string source, IEnumerable<string> targets)
    {
        foreach (var target in targets)
        {
            AddLink(source, target);
        }
    }

    /// <summary>
    /// Gets all outgoing links from a tiddler.
    /// </summary>
    /// <param name="source">The source tiddler title</param>
    /// <returns>Set of target titles</returns>
    public IReadOnlySet<string> GetOutgoingLinks(string source)
    {
        return _outgoingLinks.GetValueOrDefault(source) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all incoming links (backlinks) to a tiddler.
    /// </summary>
    /// <param name="target">The target tiddler title</param>
    /// <returns>Set of source titles that link to this target</returns>
    public IReadOnlySet<string> GetBacklinks(string target)
    {
        return _incomingLinks.GetValueOrDefault(target) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all tiddler titles that exist in the graph (as either source or target).
    /// </summary>
    public IReadOnlyCollection<string> GetAllNodes()
    {
        return _nodes;
    }

    /// <summary>
    /// Checks if a tiddler exists in the graph.
    /// </summary>
    /// <param name="title">The tiddler title to check</param>
    /// <returns>True if the title exists in the graph</returns>
    public bool HasNode(string title)
    {
        return _nodes.Contains(title);
    }

    /// <summary>
    /// Checks if there's a link from source to target.
    /// </summary>
    /// <param name="source">The source tiddler title</param>
    /// <param name="target">The target tiddler title</param>
    /// <returns>True if a link exists</returns>
    public bool HasLink(string source, string target)
    {
        return GetOutgoingLinks(source).Contains(target);
    }

    /// <summary>
    /// Gets all links in the graph.
    /// </summary>
    /// <returns>Dictionary of source to targets</returns>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> GetAllLinks()
    {
        return _outgoingLinks.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlySet<string>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all backlinks in the graph.
    /// </summary>
    /// <returns>Dictionary of target to sources</returns>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> GetAllBacklinks()
    {
        return _incomingLinks.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlySet<string>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects cycles in the graph using DFS.
    /// </summary>
    /// <returns>List of cycles, where each cycle is a list of titles</returns>
    public IList<IList<string>> DetectCycles()
    {
        var cycles = new List<IList<string>>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var node in _nodes)
        {
            if (!visited.Contains(node))
            {
                DetectCyclesDFS(node, visited, recursionStack, path, cycles);
            }
        }

        return cycles;
    }

    private void DetectCyclesDFS(
        string node,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        IList<IList<string>> cycles)
    {
        visited.Add(node);
        recursionStack.Add(node);
        path.Add(node);

        foreach (var neighbor in GetOutgoingLinks(node))
        {
            if (!_nodes.Contains(neighbor))
                continue;

            if (!visited.Contains(neighbor))
            {
                DetectCyclesDFS(neighbor, visited, recursionStack, path, cycles);
            }
            else if (recursionStack.Contains(neighbor))
            {
                // Found a cycle
                var cycleStart = path.IndexOf(neighbor);
                if (cycleStart >= 0)
                {
                    var cycle = path.Skip(cycleStart).ToList();
                    cycle.Add(neighbor); // Close the cycle
                    cycles.Add(cycle);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(node);
    }

    /// <summary>
    /// Gets orphaned tiddlers (tiddlers with no incoming links).
    /// </summary>
    /// <returns>List of orphaned tiddler titles</returns>
    public IReadOnlyList<string> GetOrphanedNodes()
    {
        return _nodes.Where(n => !_incomingLinks.ContainsKey(n) || _incomingLinks[n].Count == 0).ToList();
    }

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of edges (links) in the graph.
    /// </summary>
    public int EdgeCount => _outgoingLinks.Values.Sum(s => s.Count);

    /// <summary>
    /// Clears all links and nodes from the graph.
    /// </summary>
    public void Clear()
    {
        _outgoingLinks.Clear();
        _incomingLinks.Clear();
        _nodes.Clear();
    }
}

/// <summary>
/// Builds a LinkGraph from parsed tiddlers.
/// </summary>
public class LinkGraphBuilder
{
    private readonly LinkGraph _graph = new();

    /// <summary>
    /// Builds the link graph from a collection of tiddlers.
    /// </summary>
    /// <param name="tiddlers">Collection of wiki tiddlers</param>
    /// <returns>The built LinkGraph</returns>
    public LinkGraph Build(IEnumerable<WikiTiddler> tiddlers)
    {
        _graph.Clear();
        
        foreach (var tiddler in tiddlers)
        {
            var links = LinkResolver.ExtractLinks(tiddler.Content);
            _graph.AddLinks(tiddler.Title, links);
        }

        return _graph;
    }

    /// <summary>
    /// Gets the built graph.
    /// </summary>
    public LinkGraph GetGraph() => _graph;
}
