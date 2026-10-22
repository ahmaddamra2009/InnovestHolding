using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure.Forms;

/// <summary>
/// The global cap of the lead forms: at most <see cref="RateLimitOptions.LeadFormGlobalPermitLimit"/> stored submissions
/// per form within <see cref="RateLimitOptions.LeadFormGlobalWindow"/>, whatever the visitors' addresses (the per-IP rate
/// limit does not bound a botnet). A fixed window per form, in memory (one app instance, as hosted); a restart resets it.
/// Only submissions that would be written count: refused and honeypot posts do not use the budget.
/// </summary>
public sealed class LeadVolumeGuard(TimeProvider time, IOptionsMonitor<SecurityOptions> options)
{
    private readonly Dictionary<string, (DateTimeOffset Start, int Count)> _windows = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <summary>Takes one place in the form's window; false when the window is full.</summary>
    public bool TryAcquire(string form)
    {
        var s = options.CurrentValue.RateLimit;
        var now = time.GetUtcNow();
        lock (_gate)
        {
            if (!_windows.TryGetValue(form, out var w) || now - w.Start >= s.LeadFormGlobalWindow) w = (now, 0);
            if (w.Count >= s.LeadFormGlobalPermitLimit)
            {
                _windows[form] = w;
                return false;
            }
            _windows[form] = (w.Start, w.Count + 1);
            return true;
        }
    }
}
