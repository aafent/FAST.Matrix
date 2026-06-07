using FAST.Matrix.Contracts.Applets;

namespace FAST.Matrix.Engine.Discovery;

/// <summary>
/// Immutable runtime registry of all discovered applets.
/// Built once at startup from <see cref="AssemblyDiscoveryService"/> output.
/// Consumed by the manifest endpoint, the WASM router guard, and the DI sandbox factory.
/// Thread-safe for concurrent reads (no mutations after construction).
/// </summary>
public sealed class AppletRegistry
{
    private readonly Dictionary<string, AppletMetadata> _byId;
    private readonly List<AppletMetadata> _all;

    // Sorted by BaseRoute length descending for longest-prefix-wins matching
    private readonly List<AppletMetadata> _routeSorted;

    public AppletRegistry(IReadOnlyList<AppletMetadata> applets)
    {
        ArgumentNullException.ThrowIfNull(applets);

        _all = applets.ToList();

        _byId = applets.ToDictionary(
            m => m.AppletId,
            m => m,
            StringComparer.OrdinalIgnoreCase);

        _routeSorted = applets
            .OrderByDescending(m => m.BaseRoute.Length)
            .ToList();
    }

    /// <summary>All registered applets in discovery order.</summary>
    public IReadOnlyList<AppletMetadata> All => _all;

    /// <summary>Total count of registered applets.</summary>
    public int Count => _all.Count;

    /// <summary>Returns the metadata for the given applet ID, or null if not found.</summary>
    public AppletMetadata? FindById(string appletId) =>
        _byId.TryGetValue(appletId, out var m) ? m : null;

    /// <summary>
    /// Returns the applet whose BaseRoute is the longest prefix of the given path.
    /// Returns null if no applet owns the path.
    /// Example: path "/customer-groups/edit/5" matches BaseRoute "/customer-groups".
    /// </summary>
    public AppletMetadata? FindByRoute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Normalise: lowercase, ensure leading slash
        var normalised = path.ToLowerInvariant();
        if (!normalised.StartsWith('/'))
            normalised = '/' + normalised;

        foreach (var metadata in _routeSorted)
        {
            var route = metadata.BaseRoute.ToLowerInvariant();

            // Exact match or prefix match (ensure we match whole path segments)
            if (normalised == route ||
                normalised.StartsWith(route + '/') ||
                normalised.StartsWith(route + '?'))
            {
                return metadata;
            }
        }

        return null;
    }

    /// <summary>Returns true if any registered applet owns the given path.</summary>
    public bool IsAppletRoute(string path) => FindByRoute(path) is not null;
}
