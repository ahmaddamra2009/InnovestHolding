using InnovestHolding.Controllers.Admin;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// Route constraint for the <c>Admin/…</c> conventional route: the controller must be one of the admin
/// controllers (types deriving from <see cref="AdminControllerBase"/>), so <c>/Admin/Home/*</c> and
/// <c>/Admin/Account/*</c> no longer resolve. The list is derived from the code, not typed by hand.
/// </summary>
public sealed class AdminControllerConstraint : IRouteConstraint
{
    public static readonly IReadOnlySet<string> Names = typeof(AdminControllerBase).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(AdminControllerBase)))
        .Select(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) ? t.Name[..^"Controller".Length] : t.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && value?.ToString() is { } name && Names.Contains(name);
}
