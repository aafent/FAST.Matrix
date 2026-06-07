using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.Navigation;

/// <summary>
/// Handles the <c>OnBeforeInternalNavigation</c> callback fired by Blazor's
/// <c>&lt;NavigationLock&gt;</c> component in the MainLayout.
///
/// Evaluation sequence:
///   1. Check bypass flag (set by confirmation dialog "yes, discard").
///   2. Query active applet for unsaved changes.
///   3. If dirty: halt navigation, show confirmation dialog.
///   4. If user confirms: set bypass flag and re-invoke navigation.
///   5. If user cancels: keep navigation halted, applet remains mounted.
/// </summary>
public sealed class NavigationGuardService
{
    private readonly IShellAppletContext _appletContext;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<NavigationGuardService> _logger;

    public NavigationGuardService(
        IShellAppletContext appletContext,
        IJSRuntime jsRuntime,
        ILogger<NavigationGuardService> logger)
    {
        _appletContext = appletContext;
        _jsRuntime     = jsRuntime;
        _logger        = logger;
    }

    /// <summary>
    /// Bind this method to <c>&lt;NavigationLock OnBeforeInternalNavigation="HandleAsync" /&gt;</c>
    /// in the MainLayout.
    /// </summary>
    public async Task HandleAsync(LocationChangingContext context)
    {
        // Step 1: Bypass flag was set by a prior "confirm discard" — allow navigation immediately
        if (_appletContext.ConsumeBypassFlag())
        {
            _logger.LogDebug(
                "[FAST.Matrix] Navigation guard bypassed for '{Path}'.",
                context.TargetLocation);
            return;
        }

        // Step 2: No active applet — nothing to guard
        if (_appletContext.ActiveApplet is null)
            return;

        // Step 3: Query the active applet for unsaved state
        bool isDirty;
        try
        {
            isDirty = await _appletContext.HasActiveAppletUnsavedChangesAsync().ConfigureAwait(false);
        }
        catch
        {
            // Defensive: treat query failure as clean state (don't trap the user)
            return;
        }

        if (!isDirty)
            return;

        // Step 4: Halt the navigation pipeline
        context.PreventNavigation();

        _logger.LogDebug(
            "[FAST.Matrix] Navigation to '{Path}' halted — applet '{AppletId}' has unsaved changes.",
            context.TargetLocation, _appletContext.ActiveAppletId);

        // Step 5: Show AdminLTE-styled confirmation dialog via JS interop
        bool confirmed;
        try
        {
            confirmed = await _jsRuntime.InvokeAsync<bool>(
                "FastMatrix.confirmNavigation",
                "You have unsaved changes. If you leave now, your changes will be lost. Continue?");
        }
        catch (Exception ex)
        {
            // JS interop failure (e.g., during prerendering) — treat as cancelled
            _logger.LogWarning(ex,
                "[FAST.Matrix] JS confirmation dialog failed for applet '{AppletId}'. Navigation cancelled.",
                _appletContext.ActiveAppletId);
            return;
        }

        if (!confirmed)
        {
            _logger.LogDebug(
                "[FAST.Matrix] User cancelled navigation from applet '{AppletId}'.",
                _appletContext.ActiveAppletId);
            return;
        }

        // Step 6: User confirmed discard — set bypass and re-navigate
        _appletContext.ForceBypassGuardOnce();

        _logger.LogInformation(
            "[FAST.Matrix] User confirmed data discard on applet '{AppletId}'. Allowing navigation to '{Path}'.",
            _appletContext.ActiveAppletId, context.TargetLocation);

        // Re-navigation is handled by the MainLayout after this method returns;
        // the bypass flag is consumed on the next NavigationLock evaluation.
    }
}
