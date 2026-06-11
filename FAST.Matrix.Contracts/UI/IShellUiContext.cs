using Microsoft.AspNetCore.Components;

namespace FAST.Matrix.Contracts.UI;

/// <summary>
/// Layout state broker. Applets use this to push contextual UI into the shell zones.
/// The Shell owns selection state — applets declare the tree, the Shell manages it.
/// </summary>
public interface IShellUiContext
{
    // ── Tree View ────────────────────────────────────────────────────────────

    bool IsTreeViewVisible { get; }
    TreeViewNode? TreeRoot { get; }

    /// <summary>
    /// The node currently selected by the user.
    /// Set by the Shell when a node is clicked. Read by applet pages.
    /// </summary>
    TreeViewNode? SelectedNode { get; }

    /// <summary>
    /// Applet declares the tree. No callback — the Shell owns selection.
    /// </summary>
    void SetCustomTree(TreeViewNode rootNode);

    void ClearCustomTree();

    // ── Top Toolbar ──────────────────────────────────────────────────────────

    RenderFragment? TopToolbar { get; }
    void SetTopToolbar(RenderFragment? toolbarTemplate);
    void ClearTopToolbar();

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever any layout state changes (tree set, node selected, toolbar changed).
    /// MainLayout subscribes and calls InvokeAsync(StateHasChanged).
    /// </summary>
    event Action? OnStateChanged;
}
