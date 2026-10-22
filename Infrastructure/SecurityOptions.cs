namespace InnovestHolding.Infrastructure;

/// <summary>
/// Technical security parameters (not business values — ANTIGRAVITY §2): defaults live here and can be
/// overridden by configuration, e.g. the environment variable <c>Security__Login__UserFailureLimit=5</c>.
/// No appsettings entry is required.
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public LoginLockoutOptions Login { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
}

public sealed class LoginLockoutOptions
{
    /// <summary>Failed attempts for one username within <see cref="Window"/> before it is locked.</summary>
    public int UserFailureLimit { get; set; } = 5;

    /// <summary>Failed attempts from one IP within <see cref="Window"/> before it is locked.</summary>
    public int IpFailureLimit { get; set; } = 20;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Upper bound on tracked UNKNOWN-name keys (memory guard). Existing accounts are keyed by id and always
    /// tracked. When full, a new unknown name is refused (fail closed).
    /// </summary>
    public int MaxTrackedKeys { get; set; } = 10_000;

    /// <summary>Upper bound on tracked IP keys (memory guard). When full, a new IP is refused (fail closed).</summary>
    public int MaxTrackedIpKeys { get; set; } = 10_000;
}

public sealed class RateLimitOptions
{
    public int PublicFormPermitLimit { get; set; } = 5;
    public TimeSpan PublicFormWindow { get; set; } = TimeSpan.FromMinutes(10);
    public int LoginPermitLimit { get; set; } = 30;
    public TimeSpan LoginWindow { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>Column mirrors and product limits for input validation (allowed technical constants).</summary>
public static class InputLimits
{
    public const int PublicBodyBytes = 32 * 1024;
    public const int MessageMax = 4000;
    public const int LongTextMax = 4000;
    public const int NewPasswordMin = 10;
    public const int NewPasswordMax = 64;
    public const int PasswordMax = 128;
}
