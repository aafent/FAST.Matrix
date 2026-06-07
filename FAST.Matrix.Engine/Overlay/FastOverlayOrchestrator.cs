using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.Overlay;

namespace FAST.Matrix.Engine.Overlay;

/// <summary>
/// Concrete implementation of <see cref="IFastOverlayOrchestrator"/>.
/// Uses <see cref="TaskCompletionSource{T}"/> to suspend applet execution
/// while the overlay panel is open, resuming with the typed result payload
/// once the user resolves or cancels the overlay.
///
/// Registration: Scoped (one per circuit on Server, singleton on WASM).
/// </summary>
internal sealed class FastOverlayOrchestrator : IFastOverlayOrchestrator
{
    // ── Public State (read by OverlayContainer) ───────────────────────────────

    public bool IsOverlayActive { get; private set; }
    public event Action? OnOverlayStateChanged;

    // Exposed internally so OverlayContainer can read panel metadata
    internal string CurrentTitle     { get; private set; } = string.Empty;
    internal string CurrentSizeClass { get; private set; } = "half";
    internal RenderFragment? CurrentContent { get; private set; }

    // ── Active session state ──────────────────────────────────────────────────

    // Stores the cancel delegate for the currently open overlay.
    // Only one overlay may be open at a time.
    private Action? _cancelCurrentOverlay;

    // ── IFastOverlayOrchestrator ──────────────────────────────────────────────

    public async Task<OverlayResult<FileSelectionPayload>> InvokeFileManagerAsync(
        FileManagerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var tcs = new TaskCompletionSource<OverlayResult<FileSelectionPayload>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // Register cancellation token support
        using var ctReg = cancellationToken.Register(() =>
            tcs.TrySetResult(OverlayResult<FileSelectionPayload>.TimedOut()));

        _cancelCurrentOverlay = () =>
            tcs.TrySetResult(OverlayResult<FileSelectionPayload>.Cancelled());

        // Build a RenderFragment that hosts a placeholder FileManager UI.
        // In production this will be replaced by the actual FAST.FileManager applet panel.
        RenderFragment content = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "fast-loading");
            builder.OpenElement(2, "i");
            builder.AddAttribute(3, "class", "fas fa-folder-open fa-2x text-primary");
            builder.CloseElement();
            builder.OpenElement(4, "p");
            builder.AddContent(5, "FAST.FileManager will mount here.");
            builder.CloseElement();
            builder.OpenElement(6, "small");
            builder.AddAttribute(7, "class", "text-muted");
            builder.AddContent(8, $"Allowed types: {(configuration.AllowedMimeTypes.Count > 0 ? string.Join(", ", configuration.AllowedMimeTypes) : "All")}");
            builder.CloseElement();
            builder.CloseElement();
        };

        OpenOverlay(configuration.PanelTitle, SizeToClass(OverlayPanelSize.Half), content);

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<OverlayResult<TResult>> InvokeCustomOverlayAsync<TResult>(
        CustomOverlayConfiguration<TResult> configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var tcs = new TaskCompletionSource<OverlayResult<TResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var ctReg = cancellationToken.Register(() =>
            tcs.TrySetResult(OverlayResult<TResult>.TimedOut()));

        _cancelCurrentOverlay = () =>
            tcs.TrySetResult(OverlayResult<TResult>.Cancelled());

        // Build the component dynamically from the configuration
        // Sequence numbers must be compile-time constants per ASP0006 — use a fixed large offset
        // for dynamic parameters since we cannot know count at compile time.
        var paramSnapshot = configuration.ComponentParameters.ToList();
        RenderFragment content = builder =>
        {
            builder.OpenComponent(0, configuration.ComponentType);
            for (int i = 0; i < paramSnapshot.Count; i++)
                builder.AddAttribute(100 + i, paramSnapshot[i].Key, paramSnapshot[i].Value);
            builder.AddAttribute(200, "OnResolve", EventCallback.Factory.Create<TResult>(
                this, result => tcs.TrySetResult(OverlayResult<TResult>.Success(result))));
            builder.CloseComponent();
        };

        OpenOverlay(configuration.PanelTitle, SizeToClass(configuration.PanelSize), content);

        return await tcs.Task.ConfigureAwait(false);
    }

    // ── Internal: called by OverlayContainer on backdrop/close click ──────────

    internal void DismissWithCancel()
    {
        _cancelCurrentOverlay?.Invoke();
        CloseOverlay();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void OpenOverlay(string title, string sizeClass, RenderFragment content)
    {
        CurrentTitle     = title;
        CurrentSizeClass = sizeClass;
        CurrentContent   = content;
        IsOverlayActive  = true;
        _cancelCurrentOverlay = null; // Will be set by the calling method

        NotifyStateChanged();
    }

    private void CloseOverlay()
    {
        IsOverlayActive       = false;
        CurrentContent        = null;
        CurrentTitle          = string.Empty;
        _cancelCurrentOverlay = null;

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnOverlayStateChanged?.Invoke();

    private static string SizeToClass(OverlayPanelSize size) => size switch
    {
        OverlayPanelSize.Narrow     => "narrow",
        OverlayPanelSize.Half       => "half",
        OverlayPanelSize.Wide       => "wide",
        OverlayPanelSize.FullScreen => "fullscreen",
        _                           => "half"
    };
}
