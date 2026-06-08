using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FAST.Matrix.Contracts.UI;
using FAST.Matrix.Contracts.Overlay;
using FAST.Matrix.Host.Server.Client.Services;
using FAST.Matrix.Host.Server.Client.Activation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── FAST.Matrix UI Context (Singleton on WASM) ────────────────────────────────
builder.Services.AddSingleton<WasmShellUiContext>();
builder.Services.AddSingleton<IShellUiContext>(sp => sp.GetRequiredService<WasmShellUiContext>());

builder.Services.AddSingleton<WasmShellAppletContext>();
builder.Services.AddSingleton<IShellAppletContext>(sp => sp.GetRequiredService<WasmShellAppletContext>());

builder.Services.AddSingleton<WasmOverlayOrchestrator>();
builder.Services.AddSingleton<IFastOverlayOrchestrator>(
    sp => sp.GetRequiredService<WasmOverlayOrchestrator>());

// ── Applet services ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<FAST.SampleApplet.Services.InMemoryOrganisationService>();

builder.Services.AddSingleton<FAST.SampleApplet.Applet.SampleApplet>(sp =>
{
    var uiContext = sp.GetRequiredService<IShellUiContext>();
    var orgSvc    = sp.GetRequiredService<FAST.SampleApplet.Services.InMemoryOrganisationService>();
    // Direct constructor — synchronous, no async, no blocking
    return new FAST.SampleApplet.Applet.SampleApplet(uiContext, orgSvc);
});

// ── Applet Activation Service ─────────────────────────────────────────────────
builder.Services.AddSingleton<AppletActivationService>(sp =>
{
    var svc     = new AppletActivationService(
        sp.GetRequiredService<IShellUiContext>(),
        sp.GetRequiredService<IShellAppletContext>());

    var applet  = sp.GetRequiredService<FAST.SampleApplet.Applet.SampleApplet>();

    svc.RegisterApplet(
        routePrefix: "/sample",
        activate:    () =>
        {
            // Applet was already constructed with tree/toolbar set up.
            // Re-apply in case of deactivation/reactivation.
            applet.Activate();
            sp.GetRequiredService<IShellAppletContext>().SetActiveApplet(applet);
        },
        deactivate:  () => applet.Deactivate()
    );

    return svc;
});

var host = builder.Build();

// Eagerly resolve AppletActivationService so it's ready before first render
host.Services.GetRequiredService<AppletActivationService>();

await host.RunAsync();
