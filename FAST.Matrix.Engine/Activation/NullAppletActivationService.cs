using FAST.Matrix.Contracts.Applets;

namespace FAST.Matrix.Engine.Activation;

/// <summary>
/// Server-side stub — always returns null.
/// The real implementation lives in the WASM AppletActivationService singleton.
/// </summary>
public sealed class NullAppletActivationService : IAppletActivationService
{
    public TApplet? GetApplet<TApplet>() where TApplet : class => null;
}
