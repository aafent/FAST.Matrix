using Microsoft.Extensions.DependencyInjection;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.UI;

namespace FAST.Matrix.Engine.DI;

/// <summary>
/// Manages isolated DI sandbox containers for each registered applet.
///
/// Resolution order per applet:
///   1. Applet's private container (attribute-declared + IAppletManifestBuilder services)
///   2. Global host container (fallback for shared infrastructure)
///
/// Each private container is keyed by <see cref="IApplet.AppletId"/>.
/// Containers are built once at startup (cold-load) and disposed on host shutdown.
/// </summary>
internal sealed class AppletContainerRegistry : IDisposable
{
    private readonly IServiceProvider _globalProvider;

    // Keyed by AppletId. Stores both the provider and the disposable root scope.
    private readonly Dictionary<string, AppletSandbox> _sandboxes = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public AppletContainerRegistry(IServiceProvider globalProvider)
    {
        _globalProvider = globalProvider ?? throw new ArgumentNullException(nameof(globalProvider));
    }

    // ── Registration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and registers an isolated DI sandbox for the given applet metadata.
    /// Called once per applet during the discovery phase at host startup.
    /// </summary>
    public void RegisterSandbox(AppletMetadata metadata)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(metadata);

        if (_sandboxes.ContainsKey(metadata.AppletId))
            throw new InvalidOperationException(
                $"An applet sandbox with AppletId '{metadata.AppletId}' is already registered. " +
                $"AppletId values must be globally unique across all loaded assemblies.");

        var services = new ServiceCollection();

        // Always forward essential shell contracts into every private sandbox
        // so applets can inject IShellUiContext and IShellAppletContext freely.
        services.AddSingleton(_globalProvider.GetRequiredService<IShellUiContext>());
        services.AddSingleton(_globalProvider.GetRequiredService<IShellAppletContext>());

        // Apply attribute-declared private service registrations
        IList<ServiceDescriptor> serviceList = services;
        foreach (var descriptor in metadata.PrivateServices)
        {
            serviceList.Add(new ServiceDescriptor(
                descriptor.ServiceType,
                descriptor.ImplementationType,
                descriptor.Lifetime));
        }

        // Apply programmatic registrations via IAppletManifestBuilder (optional)
        // Instantiate a temporary instance of the applet type solely to call ConfigureServices.
        // The real applet instance is created later via OnAppletInitAsync.
        if (typeof(IAppletManifestBuilder).IsAssignableFrom(metadata.AppletType))
        {
            IAppletManifestBuilder? builder = null;
            try
            {
                builder = (IAppletManifestBuilder)Activator.CreateInstance(metadata.AppletType)!;
                builder.ConfigureServices(services);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to invoke ConfigureServices on applet '{metadata.AppletId}' " +
                    $"(type: {metadata.AppletType.FullName}). " +
                    $"Ensure the applet has a public parameterless constructor. Inner: {ex.Message}", ex);
            }
        }

        var privateProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,  // Applet sandboxes manage their own scope semantics
            ValidateOnBuild = true
        });

        _sandboxes[metadata.AppletId] = new AppletSandbox(privateProvider);
    }

    // ── Resolution ───────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a service for the given applet.
    /// Checks the applet's private container first, then falls back to the global provider.
    /// </summary>
    public object? GetService(string appletId, Type serviceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sandboxes.TryGetValue(appletId, out var sandbox))
        {
            var service = sandbox.Provider.GetService(serviceType);
            if (service is not null)
                return service;
        }

        // Fallback to global container
        return _globalProvider.GetService(serviceType);
    }

    /// <summary>
    /// Resolves a required service for the given applet. Throws if not found anywhere.
    /// </summary>
    public object GetRequiredService(string appletId, Type serviceType)
    {
        var service = GetService(appletId, serviceType);

        return service ?? throw new InvalidOperationException(
            $"No service of type '{serviceType.FullName}' is registered in the private sandbox " +
            $"for applet '{appletId}' or in the global container.");
    }

    /// <summary>
    /// Returns the private <see cref="IServiceProvider"/> for the given applet.
    /// Used by the engine to pass into <see cref="IApplet.OnAppletInitAsync"/>.
    /// </summary>
    public IServiceProvider GetAppletServiceProvider(string appletId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sandboxes.TryGetValue(appletId, out var sandbox))
            throw new KeyNotFoundException(
                $"No DI sandbox found for appletId '{appletId}'. " +
                $"Ensure the applet was discovered and registered at startup.");

        return new FallbackServiceProvider(sandbox.Provider, _globalProvider);
    }

    /// <summary>Returns true if a sandbox has been registered for the given applet ID.</summary>
    public bool HasSandbox(string appletId) => _sandboxes.ContainsKey(appletId);

    // ── Disposal ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sandbox in _sandboxes.Values)
            sandbox.Dispose();

        _sandboxes.Clear();
    }

    // ── Private Types ─────────────────────────────────────────────────────────

    private sealed class AppletSandbox(ServiceProvider provider) : IDisposable
    {
        public ServiceProvider Provider { get; } = provider;

        public void Dispose() => Provider.Dispose();
    }
}

/// <summary>
/// Wraps an applet's private provider with a fallback to the global container.
/// This is the <see cref="IServiceProvider"/> passed into <see cref="IApplet.OnAppletInitAsync"/>.
/// </summary>
internal sealed class FallbackServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _primary;
    private readonly IServiceProvider _fallback;

    public FallbackServiceProvider(IServiceProvider primary, IServiceProvider fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public object? GetService(Type serviceType) =>
        _primary.GetService(serviceType) ?? _fallback.GetService(serviceType);
}
