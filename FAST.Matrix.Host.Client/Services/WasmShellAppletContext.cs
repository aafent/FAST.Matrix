using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Services;

/// <summary>
/// WASM-side implementation of <see cref="IShellAppletContext"/>.
/// </summary>
internal sealed class WasmShellAppletContext : IShellAppletContext
{
    public IApplet? ActiveApplet { get; private set; }
    public string? ActiveAppletId => ActiveApplet?.AppletId;
    public event Action? OnActiveAppletChanged;

    private volatile bool _bypassFlag;

    public void SetActiveApplet(IApplet applet)
    {
        ActiveApplet = applet;
        OnActiveAppletChanged?.Invoke();
    }

    public void ClearActiveApplet()
    {
        ActiveApplet = null;
        OnActiveAppletChanged?.Invoke();
    }

    public async Task<bool> HasActiveAppletUnsavedChangesAsync()
    {
        if (ActiveApplet is null) return false;
        try { return await ActiveApplet.HasUnsavedChangesAsync(); }
        catch { return false; }
    }

    public void ForceBypassGuardOnce() => _bypassFlag = true;

    public bool ConsumeBypassFlag()
    {
        if (!_bypassFlag) return false;
        _bypassFlag = false;
        return true;
    }
}
