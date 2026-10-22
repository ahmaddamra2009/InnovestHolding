using System.Threading.RateLimiting;
using InnovestHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure;

public static class RateLimitPolicies
{
    /// <summary>
    /// Public Register / Contact: per IP and per ACTION. The partition is the action the request was routed
    /// to (from endpoint metadata), never the raw path, so <c>/Home/Contact/1</c>, a trailing slash, another
    /// casing or another route template all share one bucket (Stage 4 H1).
    /// </summary>
    public const string PublicForm = "public-form";

    /// <summary>POST /Account/Login: per IP.</summary>
    public const string Login = "login";

    public static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

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

                // The public forms are called by fetch() and expect JSON; the login form is a full page, so it
                // gets the login page back with the locked message (status stays 429 — Stage 8 N5); anything
                // else gets plain text.
                var action = Action(ctx.HttpContext);
                if (action?.ControllerName == "Home")
                {
                    await response.WriteAsJsonAsync(
                        new { success = false, message = UserMessages.TooManyRequests }, cancellationToken);
                }
                else if (action is { ControllerName: "Account", ActionName: "Login" })
                {
                    await LockedLoginPage(ctx.HttpContext, action);
                }
                else
                {
                    response.ContentType = "text/plain; charset=utf-8";
                    await response.WriteAsync(UserMessages.TooManyRequests, cancellationToken);
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
                Model = new LoginViewModel { ErrorMessage = UserMessages.LoginLocked }
            }
        };
        return result.ExecuteResultAsync(new ActionContext(http, http.GetRouteData(), action));
    }
}
