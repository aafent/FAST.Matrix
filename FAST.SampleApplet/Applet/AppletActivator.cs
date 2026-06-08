using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.UI;

namespace FAST.SampleApplet.Applet;

/// <summary>
/// Lightweight component mounted at the top of every applet page.
/// Responsible for activating the applet instance with <see cref="IShellAppletContext"/>
/// so the navigation guard and overlay system know which applet is active.
///
/// Usage in applet pages:
/// <code>
/// &lt;AppletActivator AppletInstance="@_applet" /&gt;
/// </code>
/// </summary>
public sealed class AppletActivator : ComponentBase, IDisposable
{
    [Inject] private IShellAppletContext AppletContext { get; set; } = default!;
    [Inject] private IShellUiContext     ShellUi       { get; set; } = default!;

    [Parameter, EditorRequired]
    public FAST.Matrix.Contracts.Applets.IApplet? AppletInstance { get; set; }

    protected override void OnParametersSet()
    {
        if (AppletInstance is not null)
            AppletContext.SetActiveApplet(AppletInstance);
    }

    public void Dispose()
    {
        AppletContext.ClearActiveApplet();
        ShellUi.ClearCustomTree();
        ShellUi.ClearTopToolbar();
    }
}
