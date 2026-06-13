namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// The primary lifecycle contract every FAST.Matrix Applet must implement.
/// Applets reference FAST.Matrix.Contracts only — never the Engine or Host.
/// </summary>
public interface IApplet
{
    /// <summary>
    /// Stable unique identifier for this applet.
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
    /// Must start with "/" and must be unique across all registered applets.
    /// </summary>
    string BaseRoute { get; }

    /// <summary>
    /// Called by the shell when this applet's route becomes active.
    /// Use to push tree structure, toolbar, and other shell state.
    /// </summary>
    void OnActivate();

    /// <summary>
    /// Called by the shell when the user navigates away from this applet's route.
    /// Use to clean up transient state.
    /// </summary>
    void OnDeactivate();

    /// <summary>
    /// Evaluated by the global navigation guard before any routing transition.
    /// Return true if the active viewport contains uncommitted transactional state.
    /// </summary>
    Task<bool> HasUnsavedChangesAsync();
}
