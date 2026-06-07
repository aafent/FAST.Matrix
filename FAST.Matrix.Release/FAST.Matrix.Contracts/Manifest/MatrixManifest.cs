namespace FAST.Matrix.Contracts.Manifest;

/// <summary>
/// Root document served at <c>/_matrix/manifest.json</c>.
/// Consumed by the WASM client's MatrixRouterGuard to determine
/// which assemblies to lazy-load for each route prefix.
/// </summary>
public sealed record MatrixManifest
{
    /// <summary>Schema version for forward-compatibility checks.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Server UTC timestamp of when the manifest was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>All applets registered in this Matrix host instance.</summary>
    public IReadOnlyList<AppletManifestEntry> Applets { get; init; } = Array.Empty<AppletManifestEntry>();
}

/// <summary>
/// One entry in the manifest — describes a single applet's route prefix
/// and binary download location for WASM lazy loading.
/// </summary>
public sealed record AppletManifestEntry
{
    /// <summary>Matches <see cref="Applets.IApplet.AppletId"/>.</summary>
    public required string AppletId { get; init; }

    /// <summary>Matches <see cref="Applets.IApplet.Name"/>.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Matches <see cref="Applets.IApplet.BaseRoute"/>.
    /// The WASM router uses prefix matching: any path starting with this value
    /// triggers lazy loading of this applet's binaries.
    /// </summary>
    public required string BaseRoute { get; init; }

    /// <summary>
    /// Base URL from which the applet's WASM/DLL binaries are served.
    /// May point to the local Matrix host or an external CDN/remote repository.
    /// Examples:
    ///   Local:    "/_matrix/applets/fast.forms.engine"
    ///   External: "https://cdn.acme.com/fast-applets/crm-contacts/v2.3.1"
    /// </summary>
    public required string BinaryBaseUrl { get; init; }

    /// <summary>
    /// When true, the WASM loader attaches a FAST.Gate Bearer token to the
    /// binary download request. Required for applets served from authenticated CDNs
    /// or private remote repositories.
    /// </summary>
    public bool RequiresGateAuth { get; init; } = false;

    /// <summary>
    /// Assembly filename (without path) to request from <see cref="BinaryBaseUrl"/>.
    /// Convention: "{AppletId}.dll" — e.g., "fast.forms.engine.dll".
    /// </summary>
    public required string AssemblyFileName { get; init; }

    /// <summary>
    /// Assembly version string for client-side cache-busting.
    /// The WASM loader appends this as a query parameter: "?v=1.2.3".
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Additional satellite assemblies required by this applet (resource DLLs, etc.).
    /// The loader fetches these in parallel with the main assembly.
    /// </summary>
    public IReadOnlyList<string> SatelliteAssemblies { get; init; } = Array.Empty<string>();
}
