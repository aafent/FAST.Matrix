namespace FAST.Matrix.Contracts.Applets;

/// <summary>
/// Holds a static reference to the WASM AppletActivationService singleton.
/// Set by AppletActivationService constructor on WASM.
/// Null on server (NullAppletActivationService doesn't set it).
/// Used by page components to bypass DI injection reuse during SSR→WASM hydration.
/// </summary>
public static class AppletActivationServiceLocator
{
    public static IAppletActivationService? Current { get; set; }
}
