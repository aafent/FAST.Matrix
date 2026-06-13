using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Contracts.Overlay;
using FAST.Matrix.Contracts.UI;
using FAST.Matrix.Host.Server.Client.Services;
using FAST.Matrix.Host.Server.Client.Activation;
using FAST.Matrix.Host.Server.Client.Services;

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
    new FAST.SampleApplet.Applet.SampleApplet(
        sp.GetRequiredService<IShellUiContext>(),
        sp.GetRequiredService<FAST.SampleApplet.Services.InMemoryOrganisationService>()));

// ── Applet Activation Service ─────────────────────────────────────────────────
builder.Services.AddSingleton<AppletActivationService>(sp =>
{
    var svc    = new AppletActivationService(
        sp.GetRequiredService<IShellUiContext>(),
        sp.GetRequiredService<IShellAppletContext>());

    svc.RegisterApplet("/sample",
        sp.GetRequiredService<FAST.SampleApplet.Applet.SampleApplet>());

    return svc;
});

builder.Services.AddSingleton<IAppletActivationService>(
    sp => sp.GetRequiredService<AppletActivationService>());

var host = builder.Build();
host.Services.GetRequiredService<AppletActivationService>();
await host.RunAsync();
