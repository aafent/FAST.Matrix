using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.UI;

/// <summary>
/// Concrete implementation of <see cref="IShellUiContext"/>.
/// Manages layout state mutations (tree view + top toolbar) and notifies
/// the AdminLTE MainLayout to re-render via the OnStateChanged event.
///
/// Registration: AddScoped on Blazor Server (one per SignalR circuit),
///               AddSingleton on WASM (one per browser tab).
/// Both registrations are handled by <see cref="Extensions.MatrixServiceExtensions"/>.
/// </summary>
internal sealed class ShellUiContext : IShellUiContext
{
    // ── State ────────────────────────────────────────────────────────────────

    public bool IsTreeViewVisible { get; private set; }
    public TreeViewNode? TreeRoot { get; private set; }
    public RenderFragment? TopToolbar { get; private set; }

    public event Action? OnStateChanged;

    // Internal: the callback registered by the active applet for tree node clicks.
    // Exposed as internal so MainLayout can invoke it via the engine cast.
    private Action<TreeViewNode>? _onNodeSelectedHandler;

    // ── Tree View ────────────────────────────────────────────────────────────

    public void SetCustomTree(TreeViewNode rootNode, Action<TreeViewNode> onNodeSelected)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(onNodeSelected);

        IsTreeViewVisible = true;
        TreeRoot = rootNode;
        _onNodeSelectedHandler = onNodeSelected;

        NotifyStateChanged();
    }

    public void ClearCustomTree()
    {
        IsTreeViewVisible = false;
        TreeRoot = null;
        _onNodeSelectedHandler = null;

        NotifyStateChanged();
    }

    // ── Top Toolbar ──────────────────────────────────────────────────────────

    public void SetTopToolbar(RenderFragment? toolbarTemplate)
    {
        TopToolbar = toolbarTemplate;
        NotifyStateChanged();
    }

    public void ClearTopToolbar()
    {
        TopToolbar = null;
        NotifyStateChanged();
    }

    // ── Internal Engine API ──────────────────────────────────────────────────

    /// <summary>
    /// Called by the MainLayout's TreeViewRenderer when the user clicks a node.
    /// Routes the selection signal to the applet's registered callback without
    /// triggering any URL navigation.
    /// </summary>
    internal void RaiseNodeSelected(TreeViewNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Mark the selected node and clear previously selected nodes in the tree
        if (TreeRoot is not null)
            ClearSelectionRecursive(TreeRoot);

        node.IsSelected = true;
        _onNodeSelectedHandler?.Invoke(node);
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    private static void ClearSelectionRecursive(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelectionRecursive(child);
    }
}
