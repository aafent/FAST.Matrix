using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.UI;

internal sealed class ShellUiContext : IShellUiContext
{
    public bool IsTreeViewVisible { get; private set; }
    public TreeViewNode? TreeRoot { get; private set; }
    public RenderFragment? TopToolbar { get; private set; }
    public event Action? OnStateChanged;

    private Action<TreeViewNode>? _onNodeSelectedHandler;

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

    internal void RaiseNodeSelected(TreeViewNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TreeRoot is not null) ClearSelectionRecursive(TreeRoot);
        node.IsSelected = true;
        _onNodeSelectedHandler?.Invoke(node);
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    private static void ClearSelectionRecursive(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelectionRecursive(child);
    }
}
