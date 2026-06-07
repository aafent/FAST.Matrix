using Microsoft.Extensions.DependencyInjection;

namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// Fluent registration surface provided to applets that need programmatic DI control
/// beyond what <see cref="Attributes.AppletServiceAttribute"/> supports
/// (e.g., factory registrations, keyed services, third-party SDK setup).
///
/// This interface is optional. Most applets should prefer attribute-based registration.
/// Implement this interface on the applet class only when attribute-based registration
/// is insufficient.
///
/// The engine calls <see cref="ConfigureServices"/> during sandbox construction,
/// BEFORE <see cref="IApplet.OnAppletInitAsync"/> is invoked.
/// </summary>
public interface IAppletManifestBuilder
{
    /// <summary>
    /// Register services into the applet's private isolated container.
    /// Do NOT register global infrastructure here — use the global host's
    /// Program.cs or a Matrix extension method for that.
    /// </summary>
    /// <param name="services">
    /// The applet's private <see cref="IServiceCollection"/>.
    /// Registrations here are isolated to this applet and do not affect
    /// other applets or the global container.
    /// </param>
    void ConfigureServices(IServiceCollection services);
}
