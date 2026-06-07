using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Services;

/// <summary>
/// WASM-side implementation of <see cref="IShellUiContext"/>.
/// Mirrors <c>ShellUiContext</c> in the Engine but carries no server-side dependencies.
/// Registered as Singleton in the WASM client's DI container.
/// </summary>
internal sealed class WasmShellUiContext : IShellUiContext
{
    public bool IsTreeViewVisible { get; private set; }
    public TreeViewNode? TreeRoot { get; private set; }
    public RenderFragment? TopToolbar { get; private set; }
    public event Action? OnStateChanged;

    private Action<TreeViewNode>? _onNodeSelectedHandler;

    public void SetCustomTree(TreeViewNode rootNode, Action<TreeViewNode> onNodeSelected)
    {
        IsTreeViewVisible    = true;
        TreeRoot             = rootNode;
        _onNodeSelectedHandler = onNodeSelected;
        OnStateChanged?.Invoke();
    }

    public void ClearCustomTree()
    {
        IsTreeViewVisible    = false;
        TreeRoot             = null;
        _onNodeSelectedHandler = null;
        OnStateChanged?.Invoke();
    }

    public void SetTopToolbar(RenderFragment? toolbarTemplate)
    {
        TopToolbar = toolbarTemplate;
        OnStateChanged?.Invoke();
    }

    public void ClearTopToolbar()
    {
        TopToolbar = null;
        OnStateChanged?.Invoke();
    }

    internal void RaiseNodeSelected(TreeViewNode node)
    {
        if (TreeRoot is not null) ClearSelection(TreeRoot);
        node.IsSelected = true;
        _onNodeSelectedHandler?.Invoke(node);
    }

    private static void ClearSelection(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelection(child);
    }
}
