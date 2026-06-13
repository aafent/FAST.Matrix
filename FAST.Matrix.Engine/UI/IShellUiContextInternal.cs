using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.UI;

/// <summary>
/// Shell-internal extension of IShellUiContext.
/// Exposes RaiseNodeSelected for MainLayout's tree click handler.
/// NOT part of the public Contracts — applets never reference this.
/// </summary>
public interface IShellUiContextInternal
{
    void RaiseNodeSelected(TreeViewNode node);
}
