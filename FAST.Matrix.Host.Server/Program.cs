using FAST.Matrix.Host.Server.Components;
using FAST.Matrix.Engine.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Render Modes ───────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ── FAST.Matrix Engine ────────────────────────────────────────────────────────
builder.Services.AddFastMatrix(builder.Configuration, options =>
{
    // Override the Applets folder path if needed (defaults to {BaseDirectory}/Applets)
    // options.Discovery.AppletsFolder = Path.Combine(builder.Environment.ContentRootPath, "Applets");
});

var app = builder.Build();

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ── Blazor Components ─────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FAST.Matrix.Host.Server.Client._Imports).Assembly);

// ── FAST.Matrix Endpoints (manifest.json, etc.) ───────────────────────────────
app.MapMatrixEndpoints();

app.Run();
