using Microsoft.AspNetCore.Components;
using FAST.Matrix.Contracts.Overlay;

namespace FAST.Matrix.Host.Server.Client.Services;

/// <summary>
/// WASM-side implementation of <see cref="IFastOverlayOrchestrator"/>.
/// </summary>
internal sealed class WasmOverlayOrchestrator : IFastOverlayOrchestrator
{
    public bool IsOverlayActive { get; private set; }
    public event Action? OnOverlayStateChanged;

    internal string CurrentTitle     { get; private set; } = string.Empty;
    internal string CurrentSizeClass { get; private set; } = "half";
    internal RenderFragment? CurrentContent { get; private set; }

    private Action? _cancelCurrent;

    public async Task<OverlayResult<FileSelectionPayload>> InvokeFileManagerAsync(
        FileManagerConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<OverlayResult<FileSelectionPayload>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var reg = cancellationToken.Register(
            () => tcs.TrySetResult(OverlayResult<FileSelectionPayload>.TimedOut()));

        _cancelCurrent = () => tcs.TrySetResult(OverlayResult<FileSelectionPayload>.Cancelled());

        RenderFragment content = b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "fast-loading");
            b.OpenElement(2, "p");
            b.AddContent(3, "FAST.FileManager will mount here.");
            b.CloseElement();
            b.CloseElement();
        };

        Open(configuration.PanelTitle, "half", content);
        return await tcs.Task;
    }

    public async Task<OverlayResult<TResult>> InvokeCustomOverlayAsync<TResult>(
        CustomOverlayConfiguration<TResult> configuration,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<OverlayResult<TResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var reg = cancellationToken.Register(
            () => tcs.TrySetResult(OverlayResult<TResult>.TimedOut()));

        _cancelCurrent = () => tcs.TrySetResult(OverlayResult<TResult>.Cancelled());

        RenderFragment content = b =>
        {
            var paramList = configuration.ComponentParameters.ToList();
            b.OpenComponent(0, configuration.ComponentType);
            for (int i = 0; i < paramList.Count; i++)
                b.AddAttribute(100 + i, paramList[i].Key, paramList[i].Value);
            b.CloseComponent();
        };

        Open(configuration.PanelTitle, SizeClass(configuration.PanelSize), content);
        return await tcs.Task;
    }

    internal void DismissWithCancel() { _cancelCurrent?.Invoke(); Close(); }

    private void Open(string title, string sizeClass, RenderFragment content)
    {
        CurrentTitle     = title;
        CurrentSizeClass = sizeClass;
        CurrentContent   = content;
        IsOverlayActive  = true;
        OnOverlayStateChanged?.Invoke();
    }

    private void Close()
    {
        IsOverlayActive = false;
        CurrentContent  = null;
        CurrentTitle    = string.Empty;
        _cancelCurrent  = null;
        OnOverlayStateChanged?.Invoke();
    }

    private static string SizeClass(OverlayPanelSize s) => s switch
    {
        OverlayPanelSize.Narrow     => "narrow",
        OverlayPanelSize.Wide       => "wide",
        OverlayPanelSize.FullScreen => "fullscreen",
        _                           => "half"
    };
}
