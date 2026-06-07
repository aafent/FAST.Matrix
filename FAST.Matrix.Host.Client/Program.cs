using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FAST.Matrix.Contracts.UI;
using FAST.Matrix.Contracts.Overlay;
using FAST.Matrix.Host.Server.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IShellUiContext, WasmShellUiContext>();
builder.Services.AddSingleton<IShellAppletContext, WasmShellAppletContext>();
builder.Services.AddSingleton<IFastOverlayOrchestrator, WasmOverlayOrchestrator>();

await builder.Build().RunAsync();
