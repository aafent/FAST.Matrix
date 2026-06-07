using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FAST.Matrix.Contracts.UI;
using FAST.Matrix.Engine.DI;
using FAST.Matrix.Engine.Discovery;
using FAST.Matrix.Engine.Manifest;
using FAST.Matrix.Engine.Navigation;
using FAST.Matrix.Engine.UI;

namespace FAST.Matrix.Engine.Extensions;

/// <summary>
/// Entry point for integrating FAST.Matrix into an ASP.NET Core host.
/// Call <c>builder.Services.AddFastMatrix(builder.Configuration)</c> in Program.cs.
/// </summary>
public static class MatrixServiceExtensions
{
    /// <summary>
    /// Registers all FAST.Matrix engine services into the host's DI container.
    /// Performs cold-load assembly discovery synchronously during service registration
    /// so the AppletRegistry is available before any request is served.
    /// </summary>
    /// <param name="services">The host's <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The host's <see cref="IConfiguration"/> for options binding.</param>
    /// <param name="configure">Optional delegate to customise discovery and manifest options.</param>
    public static IServiceCollection AddFastMatrix(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MatrixOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ── Bind Options ──────────────────────────────────────────────────────

        var options = new MatrixOptions();
        configuration.GetSection(AssemblyDiscoveryOptions.SectionKey).Bind(options.Discovery);
        configuration.GetSection($"{AssemblyDiscoveryOptions.SectionKey}:Manifest").Bind(options.Manifest);
        configure?.Invoke(options);

        // ── UI Context Services (Scoped on Server, Singleton on WASM) ─────────
        // We register as Scoped here for the Server host.
        // The WASM client project (FAST.Matrix.Host.Client) registers as Singleton in its own Program.cs.

        services.AddScoped<ShellUiContext>();
        services.AddScoped<IShellUiContext>(sp => sp.GetRequiredService<ShellUiContext>());

        services.AddScoped<ShellAppletContext>();
        services.AddScoped<IShellAppletContext>(sp => sp.GetRequiredService<ShellAppletContext>());

        // ── Navigation Guard ──────────────────────────────────────────────────

        services.AddScoped<NavigationGuardService>();

        // ── Discovery & Registry ──────────────────────────────────────────────

        services.AddSingleton(options.Discovery);
        services.AddSingleton(options.Manifest);

        services.AddSingleton<AssemblyDiscoveryService>();

        // Perform discovery and build the registry at registration time.
        // BuildServiceProvider() here is a deliberate one-time bootstrap call.
        // We need the registry before the DI sandbox factory can be initialised.
        services.AddSingleton(sp =>
        {
            var discoveryService = sp.GetRequiredService<AssemblyDiscoveryService>();
            var applets          = discoveryService.DiscoverApplets();
            return new AppletRegistry(applets);
        });

        // ── DI Sandbox Registry ───────────────────────────────────────────────

        services.AddSingleton(sp =>
        {
            var registry  = sp.GetRequiredService<AppletRegistry>();
            var container = new AppletContainerRegistry(sp);

            foreach (var metadata in registry.All)
            {
                try
                {
                    container.RegisterSandbox(metadata);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetRequiredService<ILogger<AppletContainerRegistry>>();
                    logger.LogError(ex,
                        "[FAST.Matrix] Failed to build DI sandbox for applet '{AppletId}'. Skipping.",
                        metadata.AppletId);
                }
            }

            return container;
        });

        // ── Applet Activator ──────────────────────────────────────────────────

        services.AddSingleton<AppletActivator>();

        // ── Manifest Service ──────────────────────────────────────────────────

        services.AddSingleton<ManifestService>();

        return services;
    }
}

/// <summary>
/// Top-level options object passed to the <see cref="MatrixServiceExtensions.AddFastMatrix"/> delegate.
/// </summary>
public sealed class MatrixOptions
{
    public AssemblyDiscoveryOptions Discovery { get; } = new();
    public ManifestOptions Manifest { get; } = new();
}
