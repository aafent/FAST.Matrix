using FAST.Matrix.Host.Server.Components;
using FAST.Matrix.Engine.Extensions;
using FAST.SampleApplet.Applet;
using FAST.SampleApplet.Pages;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ── FAST.Matrix Engine ────────────────────────────────────────────────────────
builder.Services.AddFastMatrix(builder.Configuration, options =>
{
    options.Discovery.AppletsFolder = Path.Combine(AppContext.BaseDirectory, "Applets");
});

// ── SampleApplet — server-side stub for SSR prerender ─────────────────────────
// The real instance (with tree/toolbar) lives in the WASM singleton.
// This stub only satisfies @inject SampleApplet during server-side prerender.
builder.Services.AddScoped<SampleApplet>(_ => new SampleApplet());

// ── AppletActivationService — server-side stub for SSR prerender ──────────────
builder.Services.AddScoped<FAST.Matrix.Host.Server.Client.Activation.AppletActivationService>(sp =>
    new FAST.Matrix.Host.Server.Client.Activation.AppletActivationService(
        sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellUiContext>(),
        sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellAppletContext>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(FAST.Matrix.Host.Server.Client._Imports).Assembly,
        typeof(SampleWorkspace).Assembly);

app.MapMatrixEndpoints();

app.Run();
