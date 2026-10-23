using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure;

public static class RateLimitPolicies
{
    /// <summary>
    /// Public forms (Forms/*): per IP and per ACTION. The partition is the action the request was routed
    /// to (from endpoint metadata), never the raw path, so <c>/Home/Contact/1</c>, a trailing slash, another
    /// casing or another route template all share one bucket.
    /// </summary>
    public const string PublicForm = "public-form";

    /// <summary>POST /Account/Login: per IP.</summary>
    public const string Login = "login";

    /// <summary>
    /// The key of every per-IP control (public-form and login rate limits, login IP lockout). IPv4 (and IPv4-mapped
    /// IPv6) is the full address. IPv6 is its /48 prefix: one subscriber normally holds a whole /64 and can rotate the
    /// low 64 bits at will, and a /48 (a site allocation, free from tunnel brokers) holds 65,536 /64s, so keying on the
    /// address or on the /64 would hand out a fresh budget per address or per /64 (C4 review M1: fanning out over a /48
    /// could fill the lead forms' global cap). Trade-off: hosts of one /48 share one budget.
    /// </summary>
    public static string ClientIp(HttpContext context) => ClientKey(context.Connection.RemoteIpAddress);

    public static string ClientKey(IPAddress? address)
    {
        if (address is null) return "unknown";
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 6, 10);
        return new IPAddress(bytes).ToString() + "/48";
    }

    private static ControllerActionDescriptor? Action(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

    /// <summary>"Controller/Action" from the endpoint the router selected; one shared bucket if there is none.</summary>
    public static string ActionKey(HttpContext context) =>
        Action(context) is { } a ? $"{a.ControllerName}/{a.ActionName}" : "?";

    public static IServiceCollection AddInnovestRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PublicForm, context =>
            {
                var s = context.RequestServices.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.RateLimit;
                var key = $"{ClientIp(context)}|{ActionKey(context)}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = s.PublicFormPermitLimit,
                    Window = s.PublicFormWindow,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy(Login, context =>
            {
                var s = context.RequestServices.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.RateLimit;
                return RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = s.LoginPermitLimit,
                    Window = s.LoginWindow,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.OnRejected = async (ctx, cancellationToken) =>
            {
                var response = ctx.HttpContext.Response;
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                // The public forms ([JsonFormEndpoint]) are called by fetch() and expect JSON; the login form is a full page, so it
                // gets the login page back with the locked message (status stays 429, so clients and logs still see the limit); anything
                // else gets plain text.
                var action = Action(ctx.HttpContext);
                var l = ctx.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
                if (ctx.HttpContext.GetEndpoint()?.Metadata.GetMetadata<JsonFormEndpointAttribute>() is not null)
                {
                    // Request localization runs before the limiter, so the message is in the page's language.
                    await response.WriteAsJsonAsync(
                        new { success = false, message = l[MessageKeys.TooManyRequests].Value }, cancellationToken);
                }
                else if (action is { ControllerName: "Account", ActionName: "Login" })
                {
                    await LockedLoginPage(ctx.HttpContext, action);
                }
                else
                {
                    response.ContentType = "text/plain; charset=utf-8";
                    await response.WriteAsync(l[MessageKeys.TooManyRequests].Value, cancellationToken);
                }
            };
        });
        return services;
    }

    /// <summary>Renders the login view (no username echoed, the form is not read) with status 429.</summary>
    private static Task LockedLoginPage(HttpContext http, ControllerActionDescriptor action)
    {
        var metadata = http.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var result = new ViewResult
        {
            ViewName = "~/Views/Account/Login.cshtml",
            StatusCode = StatusCodes.Status429TooManyRequests,
            ViewData = new ViewDataDictionary<LoginViewModel>(metadata, new ModelStateDictionary())
            {
                Model = new LoginViewModel
                {
                    ErrorMessage = http.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>()[MessageKeys.LoginLocked].Value
                }
            }
        };
        return result.ExecuteResultAsync(new ActionContext(http, http.GetRouteData(), action));
    }
}

/// <summary>
/// Marks a public form endpoint that is called by fetch() and answers JSON: a rejected request (rate limit) then gets a
/// JSON body in the page's language instead of plain text, which the page script can show.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class JsonFormEndpointAttribute : Attribute;
