namespace FAST.Matrix.Engine.DI;

/// <summary>
/// Minimal <see cref="IServiceProvider"/> used exclusively in development mode
/// to bootstrap applet instances registered via project reference (not DLL scan).
/// In production this path is never hit — the full <see cref="AppletContainerRegistry"/>
/// handles init via the scanned sandbox provider.
/// </summary>
public sealed class DevAppletServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    /// <param name="services">
    /// Service instances to expose. Each is registered under every interface it implements
    /// plus its concrete type, so both <c>GetService(typeof(IFoo))</c> and
    /// <c>GetService(typeof(FooImpl))</c> resolve correctly.
    /// </param>
    public DevAppletServiceProvider(params object[] services)
    {
        foreach (var svc in services)
        {
            _services.TryAdd(svc.GetType(), svc);
            foreach (var iface in svc.GetType().GetInterfaces())
                _services.TryAdd(iface, svc);
        }
    }

    public object? GetService(Type serviceType) =>
        _services.TryGetValue(serviceType, out var svc) ? svc : null;
}
