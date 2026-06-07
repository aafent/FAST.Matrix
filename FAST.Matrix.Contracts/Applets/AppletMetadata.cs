namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// Immutable descriptor produced by the engine's assembly scanner during startup.
/// Captures all static metadata needed to build the DI sandbox, manifest JSON entry,
/// and routing table — without instantiating the applet.
/// </summary>
public sealed record AppletMetadata
{
    /// <summary>Matches <see cref="IApplet.AppletId"/>.</summary>
    public required string AppletId { get; init; }

    /// <summary>Matches <see cref="IApplet.Name"/>.</summary>
    public required string Name { get; init; }

    /// <summary>Matches <see cref="IApplet.BaseRoute"/>.</summary>
    public required string BaseRoute { get; init; }

    /// <summary>
    /// The concrete <see cref="Type"/> implementing <see cref="IApplet"/>.
    /// Used by the engine to instantiate the applet after the sandbox is ready.
    /// </summary>
    public required Type AppletType { get; init; }

    /// <summary>
    /// The <see cref="System.Reflection.Assembly"/> that was loaded to discover this applet.
    /// Retained for WASM lazy-load verification on the server side.
    /// </summary>
    public required System.Reflection.Assembly SourceAssembly { get; init; }

    /// <summary>
    /// All private service registrations declared via <see cref="Attributes.AppletServiceAttribute"/>
    /// on the applet class. Populated by the scanner in a single reflection pass.
    /// </summary>
    public required IReadOnlyList<AppletServiceDescriptor> PrivateServices { get; init; }

    /// <summary>
    /// The version of the applet assembly, sourced from <c>AssemblyInformationalVersionAttribute</c>.
    /// Used in the manifest JSON for client-side cache-busting.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// When true, the engine includes this applet in the <c>/_matrix/manifest.json</c>
    /// WASM lazy-load manifest. Set to false for server-only applets.
    /// </summary>
    public bool IncludeInWasmManifest { get; init; } = true;
}
