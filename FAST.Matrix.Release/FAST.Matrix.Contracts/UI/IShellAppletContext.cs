namespace FAST.Matrix.Contracts.UI;

/// <summary>
/// Tracks the runtime lifecycle of the currently active applet.
/// Consumed by the NavigationLock interceptor and the overlay orchestrator.
/// Kept separate from <see cref="IShellUiContext"/> to enforce single responsibility:
/// layout concerns stay in IShellUiContext; applet runtime concerns live here.
///
/// Scoped per SignalR circuit on Blazor Server; singleton on WASM.
/// </summary>
public interface IShellAppletContext
{
    /// <summary>
    /// The <see cref="Applets.IApplet"/> instance currently mounted in the main viewport.
    /// Null during the initial shell load before any applet activates.
    /// </summary>
    Applets.IApplet? ActiveApplet { get; }

    /// <summary>
    /// The AppletId of the currently active applet. Null when no applet is active.
    /// Convenience alias to avoid null-checking ActiveApplet.AppletId everywhere.
    /// </summary>
    string? ActiveAppletId { get; }

    /// <summary>
    /// Registers the applet instance as the current viewport occupant.
    /// Called by the engine's dynamic router when an applet route is activated.
    /// </summary>
    void SetActiveApplet(Applets.IApplet applet);

    /// <summary>
    /// Clears the active applet registration.
    /// Called by the engine when the user navigates away from all applet routes
    /// back to shell-native pages (e.g., dashboard, settings).
    /// </summary>
    void ClearActiveApplet();

    /// <summary>
    /// Evaluates the active applet's unsaved state without throwing if no applet is active.
    /// Returns false (safe to navigate) when <see cref="ActiveApplet"/> is null.
    /// </summary>
    Task<bool> HasActiveAppletUnsavedChangesAsync();

    /// <summary>
    /// Bypasses the navigation guard for the next routing transition only.
    /// Used by the confirmation dialog: when the user accepts data loss, the guard
    /// must not re-trigger on the programmatic re-navigation that follows.
    /// Auto-resets after one navigation event.
    /// </summary>
    void ForceBypassGuardOnce();

    /// <summary>
    /// True when a bypass was requested via <see cref="ForceBypassGuardOnce"/>.
    /// Read by the NavigationLock handler; consuming it resets it to false.
    /// </summary>
    bool ConsumeBypassFlag();

    /// <summary>
    /// Fired when the active applet changes (activation or deactivation).
    /// </summary>
    event Action? OnActiveAppletChanged;
}
