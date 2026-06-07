using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.UI;

/// <summary>
/// Concrete implementation of <see cref="IShellAppletContext"/>.
/// Tracks the currently mounted applet and controls the NavigationLock bypass flag.
///
/// Registration: AddScoped on Blazor Server, AddSingleton on WASM.
/// </summary>
internal sealed class ShellAppletContext : IShellAppletContext
{
    // ── State ────────────────────────────────────────────────────────────────

    public IApplet? ActiveApplet { get; private set; }
    public string? ActiveAppletId => ActiveApplet?.AppletId;

    public event Action? OnActiveAppletChanged;

    // Single-use bypass flag — consumed by the NavigationLock handler after one read.
    private volatile bool _bypassFlagPending;

    // ── Applet Lifecycle ─────────────────────────────────────────────────────

    public void SetActiveApplet(IApplet applet)
    {
        ArgumentNullException.ThrowIfNull(applet);

        if (string.IsNullOrWhiteSpace(applet.AppletId))
            throw new InvalidOperationException(
                $"Applet of type '{applet.GetType().FullName}' returned a null or empty AppletId.");

        ActiveApplet = applet;
        OnActiveAppletChanged?.Invoke();
    }

    public void ClearActiveApplet()
    {
        ActiveApplet = null;
        OnActiveAppletChanged?.Invoke();
    }

    // ── Unsaved Changes Guard ────────────────────────────────────────────────

    public async Task<bool> HasActiveAppletUnsavedChangesAsync()
    {
        if (ActiveApplet is null)
            return false;

        try
        {
            return await ActiveApplet.HasUnsavedChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Defensive: if the applet's check throws, treat as safe to navigate.
            // Log the fault without crashing the navigation pipeline.
            Console.Error.WriteLine(
                $"[FAST.Matrix] HasUnsavedChangesAsync threw on applet '{ActiveAppletId}': {ex.Message}");
            return false;
        }
    }

    // ── Navigation Guard Bypass ──────────────────────────────────────────────

    public void ForceBypassGuardOnce()
    {
        _bypassFlagPending = true;
    }

    /// <summary>
    /// Reads and resets the bypass flag atomically.
    /// Returns true once after <see cref="ForceBypassGuardOnce"/> is called,
    /// then resets to false automatically.
    /// </summary>
    public bool ConsumeBypassFlag()
    {
        if (!_bypassFlagPending)
            return false;

        _bypassFlagPending = false;
        return true;
    }
}
