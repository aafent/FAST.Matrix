namespace FAST.Matrix.Contracts.UI;

/// <summary>
/// A node in the applet-driven contextual left sidebar tree.
/// Supports unlimited nesting depth: Group → Company → Office → Contact, etc.
/// </summary>
public sealed class TreeViewNode
{
    /// <summary>
    /// Unique identifier within the tree. Used by the event bus to route selection signals.
    /// Must be unique across the entire tree for a given applet instance.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label rendered in the sidebar.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Semantic type tag used by applet code to differentiate node roles.
    /// Suggested conventions: "Group", "Company", "Office", "Contact", "Document".
    /// </summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// Font Awesome icon class applied to the node.
    /// Defaults to a folder icon. Override per node for semantic clarity.
    /// Example: "fas fa-building", "fas fa-user", "fas fa-file-alt".
    /// </summary>
    public string IconClass { get; set; } = "fas fa-folder";

    /// <summary>
    /// Arbitrary key/value bag for applet-specific metadata.
    /// Useful for carrying entity IDs, URLs, or type discriminators
    /// without polluting the tree model itself.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>Whether this node renders in an expanded state on initial render.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Whether this node is currently selected (active state).
    /// Managed by the Shell's TreeViewRenderer — do not set manually.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>Child nodes. Empty list means the node is a leaf.</summary>
    public List<TreeViewNode> Children { get; set; } = new();

    /// <summary>
    /// Convenience: returns true if this node has no children.
    /// </summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>
    /// Recursively searches the subtree for a node with the given <paramref name="id"/>.
    /// Returns null if not found.
    /// </summary>
    public TreeViewNode? FindById(string id)
    {
        if (Id == id) return this;
        foreach (var child in Children)
        {
            var found = child.FindById(id);
            if (found is not null) return found;
        }
        return null;
    }
}
