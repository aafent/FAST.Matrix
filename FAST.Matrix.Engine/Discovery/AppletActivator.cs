using Microsoft.Extensions.Logging;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Engine.DI;

namespace FAST.Matrix.Engine.Discovery;

/// <summary>
/// Activates applet instances after their DI sandboxes are ready.
/// Handles the full init sequence: instantiate → pass sandbox provider → OnAppletInitAsync.
/// Caches live instances by AppletId for the duration of the host lifetime.
/// </summary>
internal sealed class AppletActivator
{
    private readonly AppletRegistry _registry;
    private readonly AppletContainerRegistry _containers;
    private readonly ILogger<AppletActivator> _logger;

    // Live applet instances keyed by AppletId — created lazily on first route activation
    private readonly Dictionary<string, IApplet> _instances = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AppletActivator(
        AppletRegistry registry,
        AppletContainerRegistry containers,
        ILogger<AppletActivator> logger)
    {
        _registry   = registry;
        _containers = containers;
        _logger     = logger;
    }

    /// <summary>
    /// Returns the live, fully initialised applet instance for the given AppletId.
    /// First call triggers instantiation and <see cref="IApplet.OnAppletInitAsync"/>.
    /// Subsequent calls return the cached instance.
    /// Thread-safe via async semaphore.
    /// </summary>
    public async Task<IApplet> GetOrActivateAsync(string appletId)
    {
        // Fast path — already activated
        if (_instances.TryGetValue(appletId, out var existing))
            return existing;

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_instances.TryGetValue(appletId, out existing))
                return existing;

            var metadata = _registry.FindById(appletId)
                ?? throw new InvalidOperationException(
                    $"No applet metadata found for AppletId '{appletId}'. " +
                    $"Ensure the assembly was present in the Applets folder at startup.");

            _logger.LogInformation(
                "[FAST.Matrix] Activating applet '{AppletId}' ({TypeName}).",
                appletId, metadata.AppletType.FullName);

            IApplet instance;
            try
            {
                instance = (IApplet)Activator.CreateInstance(metadata.AppletType)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate applet '{appletId}' (type: {metadata.AppletType.FullName}). " +
                    $"Ensure a public parameterless constructor exists. Inner: {ex.Message}", ex);
            }

            var appletServiceProvider = _containers.GetAppletServiceProvider(appletId);

            try
            {
                await instance.OnAppletInitAsync(appletServiceProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"OnAppletInitAsync threw for applet '{appletId}'. " +
                    $"Inner: {ex.Message}", ex);
            }

            _instances[appletId] = instance;

            _logger.LogInformation(
                "[FAST.Matrix] Applet '{AppletId}' activated successfully.",
                appletId);

            return instance;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>Returns a cached applet instance without triggering activation. Null if not yet active.</summary>
    public IApplet? GetIfActive(string appletId) =>
        _instances.TryGetValue(appletId, out var instance) ? instance : null;
}
