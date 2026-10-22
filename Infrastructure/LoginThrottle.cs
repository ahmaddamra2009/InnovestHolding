using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;

namespace InnovestHolding.Infrastructure;

/// <summary>
/// The identity a login attempt is counted against. An existing account is keyed on its database id, so
/// every spelling the database resolves to that account (case, full-width/half-width, …) shares ONE
/// counter (Stage 4 H2). A name that matches no account is keyed on its NFKC-normalised, upper-cased form.
/// </summary>
public readonly record struct LoginKey(string Value, bool IsAccount)
{
    public static LoginKey ForAccount(int userId) => new("id:" + userId, true);

    public static LoginKey ForUnknownName(string? submitted) =>
        new("n:" + NormalizeName(submitted).ToUpperInvariant(), false);

    /// <summary>Trim + Unicode NFKC (folds full-width/compatibility forms). Invalid UTF-16 is kept as is.</summary>
    public static string NormalizeName(string? submitted)
    {
        var s = (submitted ?? "").Trim();
        try { return s.Normalize(NormalizationForm.FormKC); }
        catch (ArgumentException) { return s; } // lone surrogate etc. — never a 500
    }
}

/// <summary>Brute-force throttle for password checks, keyed by account (or unknown name) and by client IP.</summary>
public interface ILoginThrottle
{
    bool IsLocked(LoginKey user, string ip);
    void RecordFailure(LoginKey user, string ip);

    /// <summary>After a successful check: clears the account's counter only. The IP counter is NOT reset (L2).</summary>
    void ResetUser(LoginKey user);
}

/// <summary>
/// In-memory implementation (no schema change). Trade-off, stated: the state is lost when the app restarts
/// or the IIS app pool recycles, and it is not shared between instances.
/// <para>Three stores. Account keys are bounded by the number of rows in <c>Users</c> and are ALWAYS tracked.
/// Unknown-name keys and IP keys are each bounded (<see cref="LoginLockoutOptions.MaxTrackedKeys"/>,
/// <see cref="LoginLockoutOptions.MaxTrackedIpKeys"/>); expired entries are purged. When a bounded store is
/// full and purging frees nothing, a key it does not already hold is treated as LOCKED — the check fails
/// closed instead of switching that dimension off (Stage 4 M2).</para>
/// </summary>
public sealed class InMemoryLoginThrottle : ILoginThrottle
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FullPurgeInterval = TimeSpan.FromSeconds(1);

    private sealed class Entry
    {
        public int Failures;
        public DateTimeOffset WindowStart;
        public DateTimeOffset? LockedUntil;
    }

    private sealed class Store(string name, Func<LoginLockoutOptions, int?> cap)
    {
        public readonly string Name = name;
        public readonly Func<LoginLockoutOptions, int?> Cap = cap;
        public readonly ConcurrentDictionary<string, Entry> Map = new(StringComparer.Ordinal);
        public int Count;
        public long NextPurgeTicks;
    }

    private readonly Store _accounts = new("account", _ => null);
    private readonly Store _names = new("unknown-name", o => o.MaxTrackedKeys);
    private readonly Store _ips = new("ip", o => o.MaxTrackedIpKeys);

    private readonly TimeProvider _time;
    private readonly IOptionsMonitor<SecurityOptions> _options;
    private readonly ILogger<InMemoryLoginThrottle>? _logger;

    public InMemoryLoginThrottle(TimeProvider time, IOptionsMonitor<SecurityOptions> options,
        ILogger<InMemoryLoginThrottle>? logger = null)
    {
        _time = time;
        _options = options;
        _logger = logger;
    }

    private LoginLockoutOptions Opt => _options.CurrentValue.Login;

    private Store StoreFor(LoginKey user) => user.IsAccount ? _accounts : _names;
    private static string IpKey(string ip) => string.IsNullOrEmpty(ip) ? "unknown" : ip;

    public bool IsLocked(LoginKey user, string ip)
    {
        var o = Opt;
        var now = _time.GetUtcNow();
        return IsKeyLocked(StoreFor(user), user.Value, now, o) || IsKeyLocked(_ips, IpKey(ip), now, o);
    }

    public void RecordFailure(LoginKey user, string ip)
    {
        var o = Opt;
        var now = _time.GetUtcNow();
        Record(StoreFor(user), user.Value, o.UserFailureLimit, now, o);
        Record(_ips, IpKey(ip), o.IpFailureLimit, now, o);
    }

    public void ResetUser(LoginKey user)
    {
        var store = StoreFor(user);
        if (store.Map.TryRemove(user.Value, out _)) Interlocked.Decrement(ref store.Count);
    }

    private bool IsKeyLocked(Store store, string key, DateTimeOffset now, LoginLockoutOptions o)
    {
        if (store.Map.TryGetValue(key, out var e))
            lock (e) return e.LockedUntil is { } until && until > now;

        // Not tracked. If the store has no room for it, refuse (fail closed) rather than let it through untracked.
        return IsFull(store, now, o);
    }

    private bool IsFull(Store store, DateTimeOffset now, LoginLockoutOptions o)
    {
        if (store.Cap(o) is not { } cap || Volatile.Read(ref store.Count) < cap) return false;
        TryPurge(store, now, o, FullPurgeInterval);
        if (Volatile.Read(ref store.Count) < cap) return false;
        _logger?.LogWarning("Login throttle {Store} store is full ({Count} keys): new keys are refused (fail closed).",
            store.Name, Volatile.Read(ref store.Count));
        return true;
    }

    private void Record(Store store, string key, int limit, DateTimeOffset now, LoginLockoutOptions o)
    {
        TryPurge(store, now, o, PurgeInterval); // keeps expired entries from piling up

        if (!store.Map.TryGetValue(key, out var entry))
        {
            if (IsFull(store, now, o)) return; // already refused by IsLocked; nothing new is stored

            var fresh = new Entry { WindowStart = now };
            entry = store.Map.GetOrAdd(key, fresh);
            if (ReferenceEquals(entry, fresh)) Interlocked.Increment(ref store.Count);
        }

        lock (entry)
        {
            if (entry.LockedUntil is { } until && until <= now)
            {
                entry.LockedUntil = null;
                entry.Failures = 0;
                entry.WindowStart = now;
            }
            if (now - entry.WindowStart > o.Window)
            {
                entry.Failures = 0;
                entry.WindowStart = now;
            }

            entry.Failures++;
            if (entry.Failures >= limit && entry.LockedUntil is null)
                entry.LockedUntil = now + o.LockoutDuration;
        }
    }

    /// <summary>Removes entries that are neither locked nor inside their window. At most once per interval per store.</summary>
    private static void TryPurge(Store store, DateTimeOffset now, LoginLockoutOptions o, TimeSpan interval)
    {
        var next = Interlocked.Read(ref store.NextPurgeTicks);
        if (now.UtcTicks < next) return;
        if (Interlocked.CompareExchange(ref store.NextPurgeTicks, (now + interval).UtcTicks, next) != next) return;

        foreach (var (key, e) in store.Map)
        {
            bool expired;
            lock (e)
                expired = (e.LockedUntil is null || e.LockedUntil <= now) && now - e.WindowStart > o.Window;
            if (expired && store.Map.TryRemove(key, out _))
                Interlocked.Decrement(ref store.Count);
        }
    }
}
