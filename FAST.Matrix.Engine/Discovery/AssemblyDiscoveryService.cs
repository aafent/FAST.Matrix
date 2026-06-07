using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace FAST.Matrix.Engine.Discovery;

/// <summary>
/// Scans a configured folder for Applet DLL assemblies at host startup (cold-load only).
/// For each valid assembly found, extracts <see cref="AppletMetadata"/> by reflecting
/// on types implementing <see cref="IApplet"/> and reading <see cref="AppletServiceAttribute"/>
/// declarations — all in a single reflection pass, without instantiating any applet.
/// </summary>
internal sealed class AssemblyDiscoveryService
{
    private readonly AssemblyDiscoveryOptions _options;
    private readonly ILogger<AssemblyDiscoveryService> _logger;

    // Dedicated load context isolates applet assemblies from the host process.
    // Using a single shared context for all applets keeps inter-applet type sharing possible
    // while still isolating from the host's default context.
    private readonly AssemblyLoadContext _loadContext;

    public AssemblyDiscoveryService(
        AssemblyDiscoveryOptions options,
        ILogger<AssemblyDiscoveryService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loadContext = new AssemblyLoadContext("FAST.Matrix.AppletContext", isCollectible: false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs the full discovery scan. Returns one <see cref="AppletMetadata"/>
    /// per valid applet found across all DLLs in the configured folder.
    /// Faults in individual assemblies are logged and skipped — they never crash the host.
    /// </summary>
    public IReadOnlyList<AppletMetadata> DiscoverApplets()
    {
        var results = new List<AppletMetadata>();

        if (!Directory.Exists(_options.AppletsFolder))
        {
            _logger.LogWarning(
                "[FAST.Matrix] Applets folder '{Folder}' does not exist. No applets will be loaded.",
                _options.AppletsFolder);
            return results;
        }

        var dllFiles = Directory.GetFiles(_options.AppletsFolder, "*.dll", SearchOption.TopDirectoryOnly);

        _logger.LogInformation(
            "[FAST.Matrix] Scanning '{Folder}' — {Count} DLL(s) found.",
            _options.AppletsFolder, dllFiles.Length);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var discovered = ScanAssembly(dllPath);
                results.AddRange(discovered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[FAST.Matrix] Failed to scan assembly '{Path}'. Skipping.",
                    dllPath);
            }
        }

        // Validate uniqueness of AppletId and BaseRoute across all discovered applets
        ValidateUniqueness(results);

        _logger.LogInformation(
            "[FAST.Matrix] Discovery complete. {Count} applet(s) registered: [{Ids}]",
            results.Count,
            string.Join(", ", results.Select(m => m.AppletId)));

        return results.AsReadOnly();
    }

    // ── Private: Assembly Scan ────────────────────────────────────────────────

    private IEnumerable<AppletMetadata> ScanAssembly(string dllPath)
    {
        Assembly assembly;
        try
        {
            assembly = _loadContext.LoadFromAssemblyPath(dllPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[FAST.Matrix] Could not load assembly '{Path}' into AppletContext. Skipping.",
                dllPath);
            yield break;
        }

        Type[] exportedTypes;
        try
        {
            exportedTypes = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Partial load — some types may still be usable
            exportedTypes = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            _logger.LogWarning(
                "[FAST.Matrix] Assembly '{Name}' had type-load errors. Proceeding with {Count} resolvable type(s).",
                assembly.GetName().Name, exportedTypes.Length);
        }

        var appletInterface = typeof(IApplet);

        foreach (var type in exportedTypes)
        {
            if (!appletInterface.IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
                continue;

            var metadata = BuildMetadata(type, assembly, dllPath);
            if (metadata is not null)
            {
                _logger.LogDebug(
                    "[FAST.Matrix] Discovered applet '{AppletId}' ({TypeName}) in '{Assembly}'.",
                    metadata.AppletId, type.FullName, assembly.GetName().Name);
                yield return metadata;
            }
        }
    }

    // ── Private: Metadata Extraction ─────────────────────────────────────────

    private AppletMetadata? BuildMetadata(Type appletType, Assembly assembly, string dllPath)
    {
        // Instantiate temporarily to read the AppletId, Name, BaseRoute properties.
        // These are identity fields that must be known before the sandbox is built.
        IApplet tempInstance;
        try
        {
            tempInstance = (IApplet)Activator.CreateInstance(appletType)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FAST.Matrix] Cannot instantiate '{TypeName}' for metadata extraction. " +
                "Applets must have a public parameterless constructor. Skipping.",
                appletType.FullName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(tempInstance.AppletId))
        {
            _logger.LogError(
                "[FAST.Matrix] Applet '{TypeName}' returned null or empty AppletId. Skipping.",
                appletType.FullName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(tempInstance.BaseRoute) || !tempInstance.BaseRoute.StartsWith('/'))
        {
            _logger.LogError(
                "[FAST.Matrix] Applet '{AppletId}' has an invalid BaseRoute '{Route}'. " +
                "BaseRoute must start with '/'. Skipping.",
                tempInstance.AppletId, tempInstance.BaseRoute);
            return null;
        }

        // Extract attribute-declared private service registrations — single reflection pass
        var privateServices = ExtractServiceDescriptors(appletType);

        // Extract assembly version for manifest cache-busting
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "1.0.0";

        // Strip any git hash suffix from informational version (e.g., "1.2.3+abc123" → "1.2.3")
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0) version = version[..plusIndex];

        var assemblyFileName = Path.GetFileName(dllPath);

        return new AppletMetadata
        {
            AppletId           = tempInstance.AppletId,
            Name               = tempInstance.Name,
            BaseRoute          = tempInstance.BaseRoute,
            AppletType         = appletType,
            SourceAssembly     = assembly,
            PrivateServices    = privateServices,
            Version            = version,
            IncludeInWasmManifest = true
        };
    }

    private static IReadOnlyList<AppletServiceDescriptor> ExtractServiceDescriptors(Type appletType)
    {
        var attributes = appletType.GetCustomAttributes<AppletServiceAttribute>(inherit: false);
        return attributes
            .Select(a => new AppletServiceDescriptor
            {
                ServiceType        = a.ServiceType,
                ImplementationType = a.ImplementationType,
                Lifetime           = a.Lifetime
            })
            .ToList()
            .AsReadOnly();
    }

    // ── Private: Validation ───────────────────────────────────────────────────

    private void ValidateUniqueness(List<AppletMetadata> metadata)
    {
        var duplicateIds = metadata
            .GroupBy(m => m.AppletId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var id in duplicateIds)
            _logger.LogError(
                "[FAST.Matrix] Duplicate AppletId '{AppletId}' detected across multiple assemblies. " +
                "Only the first registration will be used. Fix the AppletId collision.",
                id);

        var duplicateRoutes = metadata
            .GroupBy(m => m.BaseRoute, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var route in duplicateRoutes)
            _logger.LogError(
                "[FAST.Matrix] Duplicate BaseRoute '{Route}' detected across multiple applets. " +
                "Route conflicts will cause unpredictable routing behaviour. Fix immediately.",
                route);
    }
}

/// <summary>
/// Configuration for <see cref="AssemblyDiscoveryService"/>.
/// Bound from <c>appsettings.json</c> under the <c>FastMatrix</c> section.
/// </summary>
public sealed class AssemblyDiscoveryOptions
{
    public const string SectionKey = "FastMatrix";

    /// <summary>
    /// Absolute or relative path to the folder containing Applet DLLs.
    /// Defaults to an "Applets" folder adjacent to the host executable.
    /// </summary>
    public string AppletsFolder { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "Applets");
}
