using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.UI;

public sealed class ShellUiContext : IShellUiContext, IShellUiContextInternal
{
    public ShellUiContext()
    {
        Console.WriteLine($"[WSUC] Constructor — instance={GetHashCode()}");
    }

    public bool IsTreeViewVisible { get; private set; }
    public TreeViewNode? TreeRoot { get; private set; }
    public TreeViewNode? SelectedNode { get; private set; }
    public RenderFragment? TopToolbar { get; private set; }
    public event Action? OnStateChanged;

    public void SetCustomTree(TreeViewNode rootNode)
    {
        Console.WriteLine($"[WSUC] SetCustomTree — instance={GetHashCode()} root={rootNode?.Text}");
        ArgumentNullException.ThrowIfNull(rootNode);
        IsTreeViewVisible = true;
        TreeRoot = rootNode;
        SelectedNode = null;
        NotifyStateChanged();
    }

    public void ClearCustomTree()
    {
        IsTreeViewVisible = false;
        TreeRoot = null;
        SelectedNode = null;
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

    void IShellUiContextInternal.RaiseNodeSelected(TreeViewNode node)
    {
        Console.WriteLine($"[WSUC] RaiseNodeSelected — instance={GetHashCode()} node={node?.Text}");
        ArgumentNullException.ThrowIfNull(node);
        if (TreeRoot is not null) ClearSelectionRecursive(TreeRoot);
        node.IsSelected = true;
        SelectedNode = node;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    private static void ClearSelectionRecursive(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelectionRecursive(child);
    }
}
