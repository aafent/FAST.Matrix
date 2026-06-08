using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Services;

internal sealed class WasmShellUiContext : IShellUiContext
{
    public bool IsTreeViewVisible { get; private set; }
    public TreeViewNode? TreeRoot { get; private set; }
    public RenderFragment? TopToolbar { get; private set; }
    public event Action? OnStateChanged;

    private Action<TreeViewNode>? _onNodeSelectedHandler;

    public void SetCustomTree(TreeViewNode rootNode, Action<TreeViewNode> onNodeSelected)
    {
        IsTreeViewVisible = true;
        TreeRoot = rootNode;
        _onNodeSelectedHandler = onNodeSelected;
        OnStateChanged?.Invoke();
    }

    public void ClearCustomTree()
    {
        IsTreeViewVisible = false;
        TreeRoot = null;
        _onNodeSelectedHandler = null;
        OnStateChanged?.Invoke();
    }

    public void SetTopToolbar(RenderFragment? toolbarTemplate)
    {
        TopToolbar = toolbarTemplate;
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
        OnStateChanged?.Invoke();
    }

    private static void ClearSelection(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelection(child);
    }
}
