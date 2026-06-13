using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Activation;

public sealed class AppletActivationService : IAppletActivationService
{
    private readonly IShellUiContext      _uiContext;
    private readonly IShellAppletContext  _appletContext;

    private readonly List<(string Prefix, IApplet Applet)> _registrations = new();
    private readonly Dictionary<Type, object>              _appletInstances = new();
    private string? _activePrefix;

    public AppletActivationService(IShellUiContext uiContext, IShellAppletContext appletContext)
    {
        _uiContext     = uiContext;
        _appletContext = appletContext;
    }

    public void RegisterApplet<TApplet>(string routePrefix, TApplet applet)
        where TApplet : class, IApplet
    {
        _registrations.Add((routePrefix.ToLowerInvariant(), applet));
        _appletInstances[typeof(TApplet)] = applet;
    }

    public TApplet? GetApplet<TApplet>() where TApplet : class
    {
        _appletInstances.TryGetValue(typeof(TApplet), out var instance);
        return instance as TApplet;
    }

    public bool ActivateForRoute(string path)
    {
        Console.WriteLine($"[AAS] ActivateForRoute — path={path} instance={GetHashCode()} _activePrefix={_activePrefix ?? "NULL"}");
        var lower = path.ToLowerInvariant();
        var match = _registrations.FirstOrDefault(r =>
            lower == r.Prefix ||
            lower.StartsWith(r.Prefix + '/') ||
            lower.StartsWith(r.Prefix + '?'));

        var newPrefix = match.Prefix;
        if (newPrefix == _activePrefix) return false;

        // Deactivate current
        if (_activePrefix is not null)
        {
            var current = _registrations.FirstOrDefault(r => r.Prefix == _activePrefix);
            current.Applet?.OnDeactivate();
            _uiContext.ClearCustomTree();
            _uiContext.ClearTopToolbar();
            _appletContext.ClearActiveApplet();
        }

        _activePrefix = newPrefix;

        // Activate new
        if (match.Applet is not null)
        {
            match.Applet.OnActivate();
            _appletContext.SetActiveApplet(match.Applet);
        }

        return true;
    }

    public void ForceActivateForRoute(string path)
    {
        Console.WriteLine($"[AAS] ForceActivateForRoute — path={path} instance={GetHashCode()}");
        _activePrefix = null;
        ActivateForRoute(path);
    }
}
