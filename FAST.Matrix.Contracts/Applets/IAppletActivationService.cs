namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// Provides access to registered applet instances by type.
/// Implemented by AppletActivationService (WASM singleton).
/// Server-side stub returns null for all types.
/// </summary>
public interface IAppletActivationService
{
    TApplet? GetApplet<TApplet>() where TApplet : class;
}
