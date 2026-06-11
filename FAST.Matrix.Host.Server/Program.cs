using FAST.Matrix.Host.Server.Components;
using FAST.Matrix.Engine.Extensions;
using FAST.Matrix.Contracts.Applets;
using FAST.Matrix.Engine.Activation;
using FAST.Matrix.Host.Server.Client.Activation;
using FAST.SampleApplet.Applet;
using FAST.SampleApplet.Pages;
using FAST.SampleApplet.Services;

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

// ── IAppletActivationService — server returns null for all GetApplet<T>() calls
builder.Services.AddScoped<IAppletActivationService, NullAppletActivationService>();

// ── SampleApplet — full init for SSR prerender (tree visible immediately) ─────
builder.Services.AddScoped<InMemoryOrganisationService>();
builder.Services.AddScoped<SampleApplet>(sp =>
{
    var uiContext = sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellUiContext>();
    var orgSvc    = sp.GetRequiredService<InMemoryOrganisationService>();
    return new SampleApplet(uiContext, orgSvc);
});

// ── AppletActivationService — server-side with SampleApplet for SSR routing ───
builder.Services.AddScoped<AppletActivationService>(sp =>
{
    var svc    = new AppletActivationService(
        sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellUiContext>(),
        sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellAppletContext>());
    var applet = sp.GetRequiredService<SampleApplet>();
    svc.RegisterApplet<SampleApplet>(
        routePrefix: "/sample",
        applet:      applet,
        activate:    () =>
        {
            applet.Activate();
            sp.GetRequiredService<FAST.Matrix.Contracts.UI.IShellAppletContext>().SetActiveApplet(applet);
        },
        deactivate:  () => applet.Deactivate());
    return svc;
});

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
