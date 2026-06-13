using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FAST.Matrix.Contracts.UI;
using FAST.Matrix.Contracts.Overlay;
using FAST.Matrix.Host.Server.Client.Services;
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
    return new FAST.SampleApplet.Applet.SampleApplet(uiContext, orgSvc);
});

// ── Applet Activation Service ─────────────────────────────────────────────────
builder.Services.AddSingleton<AppletActivationService>(sp =>
{
    var svc    = new AppletActivationService(
        sp.GetRequiredService<IShellUiContext>(),
        sp.GetRequiredService<IShellAppletContext>());

    var applet = sp.GetRequiredService<FAST.SampleApplet.Applet.SampleApplet>();

    svc.RegisterApplet<FAST.SampleApplet.Applet.SampleApplet>(
        routePrefix: "/sample",
        applet:      applet,
        activate:    () =>
        {
            applet.Activate();
            sp.GetRequiredService<IShellAppletContext>().SetActiveApplet(applet);
        },
        deactivate:  () => applet.Deactivate());

    return svc;
});

builder.Services.AddSingleton<FAST.Matrix.Contracts.Applets.IAppletActivationService>(
    sp => sp.GetRequiredService<AppletActivationService>());

var host = builder.Build();

// Eagerly resolve AppletActivationService so it's ready before first render
host.Services.GetRequiredService<AppletActivationService>();

await host.RunAsync();
