using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.Navigation;

public sealed class NavigationGuardService
{
    private readonly IShellAppletContext _appletContext;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navManager;
    private readonly ILogger<NavigationGuardService> _logger;

    public NavigationGuardService(
        IShellAppletContext appletContext,
        IJSRuntime jsRuntime,
        NavigationManager navManager,
        ILogger<NavigationGuardService> logger)
    {
        _appletContext = appletContext;
        _jsRuntime     = jsRuntime;
        _navManager    = navManager;
        _logger        = logger;
    }

    public async Task HandleAsync(LocationChangingContext context)
    {
        // Bypass flag set by prior confirm — allow through
        if (_appletContext.ConsumeBypassFlag())
            return;

        if (_appletContext.ActiveApplet is null)
            return;

        bool isDirty;
        try { isDirty = await _appletContext.HasActiveAppletUnsavedChangesAsync().ConfigureAwait(false); }
        catch { return; }

        if (!isDirty)
            return;

        // Halt routing pipeline
        context.PreventNavigation();

        bool confirmed;
        try
        {
            confirmed = await _jsRuntime.InvokeAsync<bool>(
                "FastMatrix.confirmNavigation",
                "You have unsaved changes. If you leave now, your changes will be lost. Continue?");
        }
        catch { return; }

        if (!confirmed)
            return;

        // User confirmed — set bypass then programmatically re-navigate
        _appletContext.ForceBypassGuardOnce();
        _navManager.NavigateTo(context.TargetLocation);
    }
}
