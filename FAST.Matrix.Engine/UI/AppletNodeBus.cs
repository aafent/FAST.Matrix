namespace FAST.Matrix.Engine.UI;

/// <summary>
/// Scoped event bus that carries tree node selection signals within a single
/// Blazor Server circuit or WASM session.
/// Both MainLayout (which receives the click) and SampleWorkspace (which renders
/// the form) share the same scoped instance — so the event reliably crosses
/// component boundaries without threading issues.
/// </summary>
public sealed class AppletNodeBus
{
    public FAST.Matrix.Contracts.UI.TreeViewNode? CurrentNode { get; private set; }

    public event Func<Task>? OnNodeSelected;

    public async Task RaiseNodeSelectedAsync(FAST.Matrix.Contracts.UI.TreeViewNode node)
    {
        CurrentNode = node;

        if (OnNodeSelected is not null)
        {
            foreach (var handler in OnNodeSelected.GetInvocationList()
                .Cast<Func<Task>>())
            {
                await handler();
            }
        }
    }
}
