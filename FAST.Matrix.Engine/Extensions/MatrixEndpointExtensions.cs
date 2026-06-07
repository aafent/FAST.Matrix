using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using FAST.Matrix.Engine.Manifest;

namespace FAST.Matrix.Engine.Extensions;

/// <summary>
/// Registers FAST.Matrix ASP.NET Core minimal API endpoints.
/// Call <c>app.MapMatrixEndpoints()</c> in Program.cs after <c>app.MapRazorComponents()</c>.
/// </summary>
public static class MatrixEndpointExtensions
{
    /// <summary>
    /// Maps all FAST.Matrix server-side HTTP endpoints:
    /// <list type="bullet">
    ///   <item><c>GET /_matrix/manifest.json</c> — WASM lazy-load manifest</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapMatrixEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // WASM manifest — consumed by the client-side MatrixRouterGuard
        app.MapGet("/_matrix/manifest.json", (ManifestService manifest, Microsoft.AspNetCore.Http.HttpContext ctx)
            => manifest.HandleManifestRequest(ctx));

        return app;
    }
}
