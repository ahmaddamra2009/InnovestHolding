using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure.Forms;

/// <summary>
/// The global cap of the lead forms: at most <see cref="RateLimitOptions.LeadFormGlobalPermitLimit"/> stored submissions
/// per form within <see cref="RateLimitOptions.LeadFormGlobalWindow"/>, whatever the visitors' addresses (the per-IP rate
/// limit does not bound a botnet). A fixed window per form, in memory (one app instance, as hosted); a restart resets it.
/// Only submissions that would be written count: refused and honeypot posts do not use the budget.
/// The first refusal of a window is logged once as a warning (form name, limit, window end: no visitor data), so the
/// operator learns that real leads may be turned away (C4 review M1); the visitor's 429 names another contact channel.
/// </summary>
public sealed class LeadVolumeGuard(TimeProvider time, IOptionsMonitor<SecurityOptions> options, ILogger<LeadVolumeGuard> logger)
{
    private readonly Dictionary<string, (DateTimeOffset Start, int Count, bool Reported)> _windows = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <summary>Takes one place in the form's window; false when the window is full.</summary>
    public bool TryAcquire(string form)
    {
        var s = options.CurrentValue.RateLimit;
        var now = time.GetUtcNow();
        bool report;
        DateTimeOffset end;
        lock (_gate)
        {
            if (!_windows.TryGetValue(form, out var w) || now - w.Start >= s.LeadFormGlobalWindow) w = (now, 0, false);
            if (w.Count < s.LeadFormGlobalPermitLimit)
            {
                _windows[form] = (w.Start, w.Count + 1, w.Reported);
                return true;
            }
            report = !w.Reported;
            end = w.Start + s.LeadFormGlobalWindow;
            _windows[form] = (w.Start, w.Count, true);
        }
        if (report)
            logger.LogWarning(
                "Lead form {Form} reached its global cap of {Limit} submissions per {Window}; further submissions are refused (429) until {WindowEnd:u}.",
                form, s.LeadFormGlobalPermitLimit, s.LeadFormGlobalWindow, end);
        return false;
    }
}
