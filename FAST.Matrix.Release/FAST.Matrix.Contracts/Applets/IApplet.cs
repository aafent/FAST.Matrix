namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// The primary lifecycle contract every FAST.Matrix Applet must implement.
/// Applets reference FAST.Matrix.Contracts only — never the Engine or Host.
/// </summary>
public interface IApplet
{
    /// <summary>
    /// Stable unique identifier for this applet. Used as DI sandbox key and manifest key.
    /// Convention: reverse-domain style, e.g. "fast.forms.engine", "acme.crm.contacts".
    /// Must be lowercase, no spaces.
    /// </summary>
    string AppletId { get; }

    /// <summary>
    /// Human-readable display name shown in the system management panel.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The root URL path prefix exclusively owned by this applet.
    /// All routes matching "{BaseRoute}/*" are dispatched to this applet.
    /// Example: "/customer-groups" captures "/customer-groups", "/customer-groups/edit/5", etc.
    /// Must start with "/" and must be unique across all registered applets.
    /// </summary>
    string BaseRoute { get; }

    /// <summary>
    /// Invoked by the Matrix engine immediately after the applet's isolated DI sandbox
    /// is constructed and before any component in the applet's route tree renders.
    /// Use this for applet-level startup logic (e.g., seeding caches, loading user preferences).
    /// Do NOT register services here — use <see cref="Attributes.AppletServiceAttribute"/> instead.
    /// </summary>
    /// <param name="appletServices">
    /// The applet's private <see cref="IServiceProvider"/> — already contains private registrations
    /// plus a fallback chain to the global host container.
    /// </param>
    Task OnAppletInitAsync(IServiceProvider appletServices);

    /// <summary>
    /// Evaluated by the global navigation guard before any routing transition or layout teardown.
    /// Return <c>true</c> if the active viewport contains uncommitted transactional state.
    /// Returning <c>true</c> causes the shell to halt navigation and display a confirmation dialog.
    /// </summary>
    Task<bool> HasUnsavedChangesAsync();
}
