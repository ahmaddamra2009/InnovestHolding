using System.Text.RegularExpressions;

namespace InnovestHolding.Infrastructure.Media;

/// <summary>
/// Serves the uploaded images at <c>/media/{key}.{jpg|png|webp}</c> — GET/HEAD only, nothing else under /media.
/// The content type comes from the signature of the stored bytes (read again here), and must agree with the
/// extension; anything else is a 404. Headers: long immutable cache (a key is never reused), nosniff (global), and a
/// CSP that sandboxes the response and allows nothing, so even a file opened directly cannot run anything.
/// </summary>
public static partial class MediaEndpoint
{
    public const string Csp = "default-src 'none'; frame-ancestors 'none'; sandbox";
    public const string CacheControl = "public, max-age=31536000, immutable";

    [GeneratedRegex(@"^/media/([0-9a-f]{32})\.(jpg|png|webp)$")]
    private static partial Regex PathPattern();

    public static IApplicationBuilder UseMediaFiles(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/media", StringComparison.OrdinalIgnoreCase)
                || (path.Length > 6 && path[6] != '/'))
            {
                await next();
                return;
            }

            var response = context.Response;
            var method = context.Request.Method;
            var match = PathPattern().Match(path);
            if (!match.Success || !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method)))
            {
                response.StatusCode = match.Success ? StatusCodes.Status405MethodNotAllowed : StatusCodes.Status404NotFound;
                return;
            }

            var store = context.RequestServices.GetRequiredService<MediaStore>();
            var claimed = ImageInspector.FromExtension("x." + match.Groups[2].Value)!.Value;
            var file = store.PathOf(match.Groups[1].Value, claimed);
            if (!File.Exists(file))
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, useAsync: true);
            var head = new byte[12];
            var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, context.RequestAborted);
            if (ImageInspector.Sniff(head.AsSpan(0, read)) != claimed)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = ImageInspector.ContentType(claimed);
            response.ContentLength = stream.Length;
            response.Headers.CacheControl = CacheControl;
            response.Headers.ContentSecurityPolicy = Csp;
            response.Headers.ETag = "\"" + match.Groups[1].Value + "\"";
            if (HttpMethods.IsHead(method)) return;
            stream.Position = 0;
            await stream.CopyToAsync(response.Body, context.RequestAborted);
        });
}
