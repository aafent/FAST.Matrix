using Microsoft.AspNetCore.Components;

namespace FAST.Matrix.Contracts.Overlay;

/// <summary>
/// Typed result returned from all overlay invocations.
/// Always check <see cref="IsDetermined"/> before accessing <see cref="Payload"/>.
/// </summary>
/// <typeparam name="T">The data payload type specific to the overlay type.</typeparam>
public sealed record OverlayResult<T>
{
    /// <summary>
    /// True when the user completed the overlay interaction (e.g., selected a file).
    /// False when the user cancelled or the overlay was dismissed without a selection.
    /// </summary>
    public bool IsDetermined { get; init; }

    /// <summary>
    /// The result payload. Only valid when <see cref="IsDetermined"/> is true.
    /// Accessing this when IsDetermined is false returns default(T).
    /// </summary>
    public T? Payload { get; init; }

    /// <summary>Reason for an undetermined result (cancellation, timeout, error).</summary>
    public OverlayDismissReason DismissReason { get; init; } = OverlayDismissReason.None;

    /// <summary>Factory: a successful, determined result.</summary>
    public static OverlayResult<T> Success(T payload) =>
        new() { IsDetermined = true, Payload = payload, DismissReason = OverlayDismissReason.None };

    /// <summary>Factory: user cancelled without selecting.</summary>
    public static OverlayResult<T> Cancelled() =>
        new() { IsDetermined = false, DismissReason = OverlayDismissReason.UserCancelled };

    /// <summary>Factory: operation timed out or CancellationToken triggered.</summary>
    public static OverlayResult<T> TimedOut() =>
        new() { IsDetermined = false, DismissReason = OverlayDismissReason.Timeout };
}

/// <summary>Why an overlay resolved without a payload.</summary>
public enum OverlayDismissReason
{
    None,
    UserCancelled,
    Timeout,
    Error
}

/// <summary>
/// Payload returned by the FAST.FileManager overlay when the user selects a file.
/// </summary>
public sealed record FileSelectionPayload
{
    /// <summary>Relative or absolute URL to the selected file resource.</summary>
    public required string FileUrl { get; init; }

    /// <summary>Original filename as stored.</summary>
    public required string FileName { get; init; }

    /// <summary>MIME type of the selected file.</summary>
    public required string ContentType { get; init; }

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Arbitrary metadata key/value pairs returned by FAST.FileManager
    /// (e.g., thumbnail URL, storage bucket, CDN path).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Configuration passed to <see cref="IFastOverlayOrchestrator.InvokeFileManagerAsync"/>.
/// </summary>
public sealed record FileManagerConfiguration
{
    /// <summary>
    /// Allowed MIME types. Empty list = all types allowed.
    /// Example: new[] { "application/pdf", "image/png" }
    /// </summary>
    public IReadOnlyList<string> AllowedMimeTypes { get; init; } = Array.Empty<string>();

    /// <summary>Maximum file size in bytes. 0 = no limit enforced by the overlay.</summary>
    public long MaxFileSizeBytes { get; init; } = 0;

    /// <summary>Panel title displayed in the overlay header.</summary>
    public string PanelTitle { get; init; } = "Select File";

    /// <summary>Whether the user may select multiple files. Returns first selection if false.</summary>
    public bool AllowMultipleSelection { get; init; } = false;

    /// <summary>
    /// Initial folder path to open in FAST.FileManager.
    /// Null means the manager opens at the user's default location.
    /// </summary>
    public string? InitialFolderPath { get; init; }
}

/// <summary>
/// Configuration for a custom overlay panel hosting an arbitrary Blazor component.
/// </summary>
/// <typeparam name="TResult">The type of data the panel resolves with on completion.</typeparam>
public sealed record CustomOverlayConfiguration<TResult>
{
    /// <summary>The Blazor component type to render inside the overlay panel.</summary>
    public required Type ComponentType { get; init; }

    /// <summary>Parameters forwarded to the component.</summary>
    public Dictionary<string, object?> ComponentParameters { get; init; } = new();

    /// <summary>Panel title displayed in the overlay header.</summary>
    public string PanelTitle { get; init; } = string.Empty;

    /// <summary>CSS width class applied to the slide-out panel. Defaults to half-screen.</summary>
    public OverlayPanelSize PanelSize { get; init; } = OverlayPanelSize.Half;
}

/// <summary>Presentation size of the overlay slide-out panel.</summary>
public enum OverlayPanelSize
{
    /// <summary>30% of viewport width.</summary>
    Narrow,
    /// <summary>50% of viewport width.</summary>
    Half,
    /// <summary>75% of viewport width.</summary>
    Wide,
    /// <summary>Full viewport width (modal-style).</summary>
    FullScreen
}
