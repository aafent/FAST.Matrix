using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Services;

internal sealed class WasmShellUiContext : IShellUiContext
{
    public WasmShellUiContext()
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
        IsTreeViewVisible = true;
        TreeRoot = rootNode;
        SelectedNode = null;
        OnStateChanged?.Invoke();
    }

    public void ClearCustomTree()
    {
        IsTreeViewVisible = false;
        TreeRoot = null;
        SelectedNode = null;
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
        Console.WriteLine($"[WSUC] RaiseNodeSelected — instance={GetHashCode()} node={node?.Text}");
        if (TreeRoot is not null) ClearSelection(TreeRoot);
        node.IsSelected = true;
        SelectedNode = node;
        OnStateChanged?.Invoke();
    }

    private static void ClearSelection(TreeViewNode node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
            ClearSelection(child);
    }
}
