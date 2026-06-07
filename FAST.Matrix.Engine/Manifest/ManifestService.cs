using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FAST.Matrix.Contracts.Manifest;
using FAST.Matrix.Engine.Discovery;

namespace FAST.Matrix.Engine.Manifest;

/// <summary>
/// Generates the <see cref="MatrixManifest"/> from the <see cref="AppletRegistry"/>
/// and serves it at <c>GET /_matrix/manifest.json</c>.
///
/// The manifest is generated once at startup and cached in memory.
/// It is never regenerated during the host's lifetime (cold-load model).
/// </summary>
internal sealed class ManifestService
{
    private readonly AppletRegistry _registry;
    private readonly ManifestOptions _options;
    private readonly ILogger<ManifestService> _logger;

    private MatrixManifest? _cachedManifest;
    private string? _cachedJson;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        WriteIndented               = false,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ManifestService(
        AppletRegistry registry,
        ManifestOptions options,
        ILogger<ManifestService> logger)
    {
        _registry = registry;
        _options  = options;
        _logger   = logger;
    }

    // ── Manifest Generation ───────────────────────────────────────────────────

    /// <summary>
    /// Builds (or returns cached) <see cref="MatrixManifest"/>.
    /// </summary>
    public MatrixManifest GetManifest()
    {
        if (_cachedManifest is not null)
            return _cachedManifest;

        var entries = _registry.All
            .Where(m => m.IncludeInWasmManifest)
            .Select(m => BuildEntry(m))
            .ToList()
            .AsReadOnly();

        _cachedManifest = new MatrixManifest
        {
            SchemaVersion = "1.0",
            GeneratedAt   = DateTimeOffset.UtcNow,
            Applets       = entries
        };

        _logger.LogInformation(
            "[FAST.Matrix] Manifest generated with {Count} applet(s).",
            entries.Count);

        return _cachedManifest;
    }

    /// <summary>Returns the manifest serialised as JSON (cached after first call).</summary>
    public string GetManifestJson()
    {
        if (_cachedJson is not null)
            return _cachedJson;

        _cachedJson = JsonSerializer.Serialize(GetManifest(), JsonOptions);
        return _cachedJson;
    }

    // ── ASP.NET Core Minimal API Handler ─────────────────────────────────────

    /// <summary>
    /// Minimal API delegate for <c>GET /_matrix/manifest.json</c>.
    /// Registered by <see cref="Extensions.MatrixEndpointExtensions.MapMatrixEndpoints"/>.
    /// </summary>
    public IResult HandleManifestRequest(HttpContext context)
    {
        var json = GetManifestJson();

        return Results.Content(
            content:     json,
            contentType: "application/json",
            statusCode:  200);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private AppletManifestEntry BuildEntry(Contracts.Applets.AppletMetadata metadata)
    {
        // Determine binary base URL:
        // If a per-applet override is configured in options, use that.
        // Otherwise, fall back to the default local serving path.
        var binaryBaseUrl = _options.AppletBinaryUrlOverrides.TryGetValue(
            metadata.AppletId, out var overrideUrl)
            ? overrideUrl.TrimEnd('/')
            : $"{_options.DefaultBinaryBaseUrl.TrimEnd('/')}/{metadata.AppletId}";

        var requiresAuth = _options.AppletAuthRequired.TryGetValue(
            metadata.AppletId, out var authRequired) && authRequired;

        return new AppletManifestEntry
        {
            AppletId           = metadata.AppletId,
            Name               = metadata.Name,
            BaseRoute          = metadata.BaseRoute,
            BinaryBaseUrl      = binaryBaseUrl,
            RequiresGateAuth   = requiresAuth,
            AssemblyFileName   = metadata.SourceAssembly.GetName().Name + ".dll",
            Version            = metadata.Version,
            SatelliteAssemblies = Array.Empty<string>()
        };
    }
}

/// <summary>
/// Configuration for <see cref="ManifestService"/>.
/// Bound from <c>appsettings.json</c> under <c>FastMatrix:Manifest</c>.
/// </summary>
public sealed class ManifestOptions
{
    /// <summary>
    /// Default base URL for serving applet binaries when no per-applet override is set.
    /// Defaults to the local Matrix host's applet serving path.
    /// </summary>
    public string DefaultBinaryBaseUrl { get; set; } = "/_matrix/applets";

    /// <summary>
    /// Per-applet binary URL overrides. Key = AppletId, Value = full base URL.
    /// Use for applets hosted on external CDNs or remote repositories.
    /// Example: { "acme.crm.contacts": "https://cdn.acme.com/fast-applets/crm/v2" }
    /// </summary>
    public Dictionary<string, string> AppletBinaryUrlOverrides { get; set; } = new();

    /// <summary>
    /// Applets that require FAST.Gate Bearer token authentication for binary downloads.
    /// Key = AppletId, Value = true to require auth.
    /// Example: { "acme.crm.contacts": true }
    /// </summary>
    public Dictionary<string, bool> AppletAuthRequired { get; set; } = new();
}
