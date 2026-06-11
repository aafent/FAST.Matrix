using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Host.Server.Client.Activation;

/// <summary>
/// Registered as Singleton on WASM, Scoped stub on Server.
/// MainLayout calls ActivateForRoute on every navigation.
/// Applets register themselves with RegisterApplet during WASM startup.
/// 
/// GetApplet&lt;T&gt;() is the safe way for page components to get the WASM
/// singleton applet instance — bypasses the DI injection which may have
/// resolved the server-side scoped instance during SSR prerender.
/// </summary>
public sealed class AppletActivationService : IAppletActivationService
{
    /// <summary>
    /// Static reference to the WASM singleton instance.
    /// Set when AppletActivationService is constructed on WASM.
    /// Null on server-side (NullAppletActivationService is used instead).
    /// SampleWorkspace reads this directly to bypass DI injection reuse during hydration.
    /// </summary>
    public static AppletActivationService? Current { get; private set; }
    private readonly IShellUiContext _uiContext;
    private readonly IShellAppletContext _appletContext;

    private readonly List<(string Prefix, Action Activate, Action Deactivate)> _registrations = new();
    private readonly Dictionary<Type, object> _appletInstances = new();

    private string? _activePrefix;

    public AppletActivationService(IShellUiContext uiContext, IShellAppletContext appletContext)
    {
        _uiContext     = uiContext;
        _appletContext = appletContext;
        Current = this;
        AppletActivationServiceLocator.Current = this;
    }

    /// <summary>
    /// Called once per applet during WASM startup (Program.cs).
    /// Stores the applet instance and registers activation/deactivation actions.
    /// </summary>
    public void RegisterApplet<TApplet>(string routePrefix, TApplet applet, Action activate, Action deactivate)
        where TApplet : class
    {
        _registrations.Add((routePrefix.ToLowerInvariant(), activate, deactivate));
        _appletInstances[typeof(TApplet)] = applet;
    }

    /// <summary>
    /// Returns the WASM singleton applet instance registered for this type.
    /// Returns null on the server-side stub (no applets registered).
    /// Use this in page components instead of @inject to guarantee the WASM instance.
    /// </summary>
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

    /// <summary>
    /// Forces re-activation even if the route is already active.
    /// Used by MainLayout.OnAfterRender to ensure the WASM-side IShellUiContext
    /// is properly set up after SSR hydration.
    /// </summary>
    public void ForceActivateForRoute(string path)
    {
        Console.WriteLine($"[AAS] ForceActivateForRoute — path={path} instance={GetHashCode()}");
        _activePrefix = null; // Reset so ActivateForRoute runs unconditionally
        ActivateForRoute(path);
    }
}
