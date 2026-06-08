using Microsoft.AspNetCore.Components;

namespace FAST.Matrix.Contracts.UI;

/// <summary>
/// Layout state broker. Applets use this to push contextual UI into the AdminLTE shell zones:
/// the left sidebar tree view and the top navbar toolbar.
///
/// Scoped per SignalR circuit on Blazor Server; singleton on WASM.
/// Applet components MUST call ClearCustomTree() and ClearTopToolbar() in their Dispose()
/// to prevent layout pollution when navigating away.
/// </summary>
public interface IShellUiContext
{
    // ── Tree View ────────────────────────────────────────────────────────────

    /// <summary>
    /// True when an applet has pushed a custom tree, suppressing the standard system menu.
    /// </summary>
    bool IsTreeViewVisible { get; }

    /// <summary>
    /// The root node of the applet's contextual tree. Null when <see cref="IsTreeViewVisible"/> is false.
    /// </summary>
    TreeViewNode? TreeRoot { get; }

    /// <summary>
    /// Activates the contextual left sidebar tree view.
    /// </summary>
    /// <param name="rootNode">Root of the hierarchy to render.</param>
    /// <param name="onNodeSelected">
    /// Async callback invoked when the user clicks a tree node.
    /// Must NOT call NavigationManager.NavigateTo(). URL must not change.
    /// </param>
    void SetCustomTree(TreeViewNode rootNode, Action<TreeViewNode> onNodeSelected);

    /// <summary>
    /// Removes the contextual tree and restores standard system navigation.
    /// Call in the applet component's Dispose() method.
    /// </summary>
    void ClearCustomTree();

    // ── Top Toolbar ──────────────────────────────────────────────────────────

    /// <summary>
    /// The RenderFragment currently injected into the top AdminLTE navbar workspace.
    /// Null when no applet has registered a toolbar.
    /// </summary>
    RenderFragment? TopToolbar { get; }

    /// <summary>
    /// Injects a Blazor component tree (buttons, dropdowns, etc.) into the top navbar.
    /// The fragment is rendered inline inside the navbar's left action zone.
    /// </summary>
    /// <param name="toolbarTemplate">
    /// A RenderFragment declared in the applet's component. Must not hold references
    /// to disposed component state. Call <see cref="ClearTopToolbar"/> on disposal.
    /// </param>
    void SetTopToolbar(RenderFragment? toolbarTemplate);

    /// <summary>
    /// Clears the top toolbar injection zone.
    /// Call in the applet component's Dispose() method.
    /// </summary>
    void ClearTopToolbar();

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever any layout state changes.
    /// The MainLayout subscribes to this and calls InvokeAsync(StateHasChanged).
    /// </summary>
    event Action? OnStateChanged;
}
