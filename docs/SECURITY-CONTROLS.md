# Security controls (PR-A)

Every statement below was checked against the code on 2026-10-22. All numbers are defaults in
`Infrastructure/SecurityOptions.cs`; they can be overridden by configuration, e.g. the environment variable
`Security__Login__UserFailureLimit=5`. No appsettings entry is required.

| Control | What it does | Where |
|---|---|---|
| Antiforgery | `AutoValidateAntiforgeryTokenAttribute` is a global filter: every POST/PUT/PATCH/DELETE needs a valid token. Antiforgery cookie: HttpOnly, SameSite=Strict, Secure (always, except Development = same as request). Only exception: `HomeController.Error` (`[IgnoreAntiforgeryToken]`, because the exception handler re-executes a failed POST there). | `Program.cs` (`AddControllersWithViews`, `AddAntiforgery`); `Controllers/HomeController.cs` |
| Rate limit - public forms | `Register` and `Contact`: **5 requests / 10 min** per IP **per action**. The partition is `ip\|Controller/Action` taken from the matched endpoint, not the URL, so path variants share one bucket; routes are pinned (`[HttpPost("/Home/Contact")]` etc.) so `/Home/Contact/1` and `/Admin/Home/Contact` return 404. Over the limit: 429 + `Retry-After`; JSON body for `Home`, plain text otherwise. | `Infrastructure/RateLimitPolicies.cs`; `Controllers/HomeController.cs` |
| Rate limit - login | `POST /Account/Login`: **30 requests / 5 min** per IP (fixed window, no queue). | `RateLimitPolicies.cs`; `Controllers/AccountController.cs` |
| Login lockout | Failed password checks are counted per **account** (keyed on user id, so every spelling the DB collation resolves to shares one counter; unknown names are keyed on their NFKC-normalised upper-case form) and per IP: **5 per account / 20 per IP within 15 min, then locked 15 min**. A locked key skips BCrypt. A successful login clears the account counter only, not the IP counter. Settings > ChangePassword uses the same account key (a wrong current password counts). Memory guard: the unknown-name and IP stores each cap at 10 000 keys; when full, new keys are refused (fail closed). State is in memory: lost on restart/app-pool recycle, not shared across instances. | `Infrastructure/LoginThrottle.cs`, `SecurityOptions.cs`; `Controllers/AccountController.cs`; `Controllers/Admin/SettingsController.cs` |
| Request body size | Register and Contact: `[RequestSizeLimit]` of **32 KB** (`InputLimits.PublicBodyBytes`), enforced by the framework when the endpoint is selected (before antiforgery reads the form); there is no custom middleware. | `Controllers/HomeController.cs` (the `[RequestSizeLimit(InputLimits.PublicBodyBytes)]` attributes on the Register and Contact POST actions) |
| Security headers | On every response, static files included: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy: frame-ancestors 'none'`, `Referrer-Policy: strict-origin-when-cross-origin`. No script/style CSP yet (inline script in `Index.cshtml`). Outside Development: HTTPS redirect + HSTS (ASP.NET default, 30 days). | `Infrastructure/SecurityMiddleware.cs`; `Program.cs` |
| Auth cookie | `Innovest.Auth`, HttpOnly, SameSite=Lax, Secure (always, except Development), 8 h sliding. | `Program.cs` |
| Data-protection keys | Persisted to `App_Data/DataProtection-Keys` (content root) so antiforgery tokens and sessions survive restarts; on Windows encrypted with DPAPI, machine scope; elsewhere unencrypted (protect by ACL). Not persisted in the `Testing` environment. `App_Data/` is git-ignored. | `Program.cs` |
| Password-change session invalidation | The principal carries `innovest:pwd` = SHA-256 of the current password hash; on every authenticated request the hash is re-read by user id and the cookie is rejected on mismatch, missing claim or deleted user. Changing the password ends all other sessions; the current session is re-issued. No schema change. | `Infrastructure/AdminSession.cs`; `Program.cs` (`OnValidatePrincipal`); `Controllers/Admin/SettingsController.cs` |
| Admin route scoping | The `admin` route only resolves controllers deriving `AdminControllerBase`; `/Admin/Home/*` and `/Admin/Account/*` return 404. | `Infrastructure/AdminControllerConstraint.cs`; `Program.cs` |
| Forwarded headers | Off unless `ForwardedHeaders__KnownProxies__0=<ip>` is set (one entry per proxy); never trusts all proxies. Needed only when TLS is terminated in front of the app. | `Program.cs` |
| LocalDB guard | Development/Testing refuse any non-`(localdb)\` connection, at configuration time and on the effective DbContext connection. | `Infrastructure/LocalDbGuard.cs`; `Program.cs` |

## Known limits (accepted)
- Per-IP controls key on the full address; with IPv6 one host may rotate addresses (follow-up: key on /64).
- 5 bad attempts on the admin account lock it for 15 min (recovery: wait, or recycle the app pool).
- Lockout and rate-limit counters reset on restart.

## Tests
`InnovestHolding.Tests/` (the suite, all of which must pass, on LocalDB `InnovestHolding_Tests`): `CsrfAndAuthTests`, `LockoutTests`, `BypassTests`, `ReRun1Tests`, `PublicFormTests`, `AdminInputTests`, `HeadersAndErrorTests`, `ReRun2Tests`, `RenderedFormTests`.
