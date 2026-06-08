using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Activation;

/// <summary>
/// Registered as Singleton on WASM.
/// MainLayout calls ActivateForRoute on every navigation.
/// Applets register themselves with RegisterApplet during startup.
/// </summary>
public sealed class AppletActivationService
{
    private readonly IShellUiContext _uiContext;
    private readonly IShellAppletContext _appletContext;

    // Route prefix → activation action
    private readonly List<(string Prefix, Action Activate, Action Deactivate)> _registrations = new();

    private string? _activePrefix;

    public AppletActivationService(IShellUiContext uiContext, IShellAppletContext appletContext)
    {
        _uiContext = uiContext;
        _appletContext = appletContext;
    }

    /// <summary>
    /// Called once per applet during WASM startup (Program.cs).
    /// Registers the route prefix and the activation/deactivation actions.
    /// </summary>
    public void RegisterApplet(string routePrefix, Action activate, Action deactivate)
    {
        _registrations.Add((routePrefix.ToLowerInvariant(), activate, deactivate));
    }

    /// <summary>
    /// Called by MainLayout on every navigation — including the initial render.
    /// Activates the matching applet, deactivates the previous one.
    /// Returns true if state changed (MainLayout should re-render).
    /// </summary>
    public bool ActivateForRoute(string path)
    {
        var lower = path.ToLowerInvariant();
        var match = _registrations.FirstOrDefault(r =>
            lower == r.Prefix ||
            lower.StartsWith(r.Prefix + '/') ||
            lower.StartsWith(r.Prefix + '?'));

        var newPrefix = match.Prefix;

        if (newPrefix == _activePrefix)
            return false;

        // Deactivate current
        if (_activePrefix is not null)
        {
            var current = _registrations.FirstOrDefault(r => r.Prefix == _activePrefix);
            current.Deactivate?.Invoke();
            _uiContext.ClearCustomTree();
            _uiContext.ClearTopToolbar();
            _appletContext.ClearActiveApplet();
        }

        _activePrefix = newPrefix;

        // Activate new
        if (newPrefix is not null)
            match.Activate?.Invoke();

        return true;
    }
}
