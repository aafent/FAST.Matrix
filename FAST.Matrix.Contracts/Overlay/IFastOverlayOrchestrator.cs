namespace FAST.Matrix.Contracts.Overlay;

/// <summary>
/// Orchestrates asynchronous modal/slide-out utility overlays (e.g., FAST.FileManager, Inbox).
/// Applets call these methods to suspend their execution flow while the overlay resolves.
/// The shell intercepts the invocation, renders the overlay panel, and resumes the caller
/// with a typed result payload once the user dismisses the overlay.
///
/// Registered as Scoped in the global container.
/// </summary>
public interface IFastOverlayOrchestrator
{
    /// <summary>
    /// Opens the FAST.FileManager as a slide-out panel.
    /// Execution suspends at the await point until the user selects a file or cancels.
    /// </summary>
    /// <param name="configuration">Constraints applied to the file picker (MIME types, size, etc.).</param>
    /// <param name="cancellationToken">Allows the applet to cancel the overlay programmatically.</param>
    /// <returns>
    /// An <see cref="OverlayResult{T}"/> where T is <see cref="FileSelectionPayload"/>.
    /// Check <see cref="OverlayResult{T}.IsDetermined"/> before reading payload data.
    /// </returns>
    Task<OverlayResult<FileSelectionPayload>> InvokeFileManagerAsync(
        FileManagerConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a generic overlay panel hosting a custom Blazor component.
    /// Useful for workflow steps, confirmation forms, or any full-panel utility.
    /// </summary>
    /// <typeparam name="TResult">The payload type the panel resolves with.</typeparam>
    /// <param name="configuration">Panel presentation options.</param>
    /// <param name="cancellationToken">Allows programmatic cancellation.</param>
    Task<OverlayResult<TResult>> InvokeCustomOverlayAsync<TResult>(
        CustomOverlayConfiguration<TResult> configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when any overlay panel is currently visible.
    /// Useful for disabling background interaction or showing a backdrop.
    /// </summary>
    bool IsOverlayActive { get; }

    /// <summary>Fired when overlay visibility changes.</summary>
    event Action? OnOverlayStateChanged;
}
