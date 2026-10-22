using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Static-file delivery: compression and long caching, both limited to the static assets.
/// <list type="bullet">
/// <item>Compression (Brotli/Gzip) runs only for requests to <see cref="CompressedPrefixes"/> and only for the
/// <see cref="CompressedTypes"/>. HTML pages are never compressed: they carry the antiforgery token next to reflected
/// input, the precondition of a BREACH-style compression oracle.</item>
/// <item><c>Cache-Control: public, max-age=31536000, immutable</c> only on a content-hashed URL (the <c>?v=</c> that
/// <c>asp-append-version</c> adds): the URL changes whenever the file does, so a year is safe. Unversioned URLs keep
/// the default (revalidate) behaviour.</item>
/// </list>
/// </summary>
public static class StaticAssets
{
    public static readonly string[] CompressedPrefixes = ["/css", "/js", "/assets", "/lib"];

    public static readonly string[] CompressedTypes =
        ["text/css", "text/javascript", "application/javascript", "image/svg+xml", "application/json"];

    public const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    public static IServiceCollection AddStaticAssetCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true; // safe here: only static files, no secrets, no reflected input
            o.MimeTypes = CompressedTypes;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        return services;
    }

    public static bool IsStaticAssetPath(PathString path) =>
        CompressedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>Compression (static paths only) + static files with the immutable rule for versioned URLs.</summary>
    public static IApplicationBuilder UseStaticAssets(this IApplicationBuilder app)
    {
        app.UseWhen(ctx => IsStaticAssetPath(ctx.Request.Path), branch => branch.UseResponseCompression());
        return app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.Context.Request.Query.ContainsKey("v"))
                    ctx.Context.Response.Headers.CacheControl = ImmutableCacheControl;
            }
        });
    }
}
