# Development guide

> **Warning - read first.** `appsettings.json` (and `appsettings.Production.json`) point at the
> **production** database, and the app runs `MigrateAsync()` + seeding on every start. Never run the app
> or `dotnet ef` on a developer machine in any environment other than `Development` (see
> [Never run as Production locally](#never-run-as-production-locally)).

## Prerequisites
.NET SDK 9, SQL Server LocalDB (instance `InnovestLocal`), PowerShell.

## 1. Clone
```powershell
git clone <repo-url> InnovestHolding
cd InnovestHolding
```

## 2. Create / start the LocalDB instance
```powershell
sqllocaldb create InnovestLocal   # once; skip if it already exists
sqllocaldb start InnovestLocal
sqllocaldb info InnovestLocal     # State: Running
```
Verified on 2026-10-22 by running `sqllocaldb info InnovestLocal` (instance existed, `State: Running`).
`create` was not re-run (the instance already existed).

## 3. Configuration (nothing to override)
`appsettings.Development.json` (not published) already sets `DefaultConnection` to
`Server=(localdb)\InnovestLocal;Database=InnovestHolding_Local;Trusted_Connection=True;TrustServerCertificate=True`.
In `Development` and `Testing` the app **refuses to start** with any connection that is not
`(localdb)\...` (`Infrastructure/LocalDbGuard.cs`); the error never prints the string.
The database is created and seeded on first start.

## 4. Build
```powershell
dotnet build InnovestHolding.sln --nologo
```
Verified on 2026-10-22 by running it: 0 Warning(s), 0 Error(s).

## 5. Run
```powershell
dotnet run --launch-profile http
```
The `http` profile sets `ASPNETCORE_ENVIRONMENT=Development` and listens on `http://localhost:5154`.
Verified on 2026-10-22 by running `dotnet run --no-build --launch-profile http`: the log printed
`Database: (localdb)\InnovestLocal / InnovestHolding_Local` and `Hosting environment: Development`;
`GET /` returned 200 with the security headers. Admin login credentials: ask the team (not documented here).

## 6. Run the tests
```powershell
dotnet test InnovestHolding.Tests/InnovestHolding.Tests.csproj --nologo
```
The suite uses its own LocalDB database `InnovestHolding_Tests` (same instance), deleted and recreated at
the start of each run; it asserts the server and database name before the host starts and again by effect
afterwards. All tests must pass.

## Never run as Production locally
The LocalDB guard only covers `Development` and `Testing`. Running the app (`dotnet run` or the dll) with
`--environment Production`, with no `ASPNETCORE_ENVIRONMENT` set, with `--no-launch-profile`, or with a
misspelt environment name reads `appsettings.json` - the production connection - and migrates/seeds it on
startup. Always start with `--launch-profile http`.

`dotnet ef` is different: with `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` unset and no connection
override, `dotnet ef dbcontext info` was run on 2026-10-22 and reported `Data source: (localdb)\InnovestLocal`,
`Database name: InnovestHolding_Local` (LocalDB). It reaches the production connection only if you set
`ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` to something other than `Development`, pass
`-- --environment Production`, or override the connection string. Never do any of these. Migrations reach
production only through the deployment.

## Local behaviour worth knowing
- Every POST needs an antiforgery token: scripts/Postman must GET the page first and send the cookie + token.
- Login lockout state is in memory; restart the app to clear it while testing (see `docs/SECURITY-CONTROLS.md`).
- In Development cookies are `SameAsRequest`, so plain-HTTP localhost works; HSTS and the HTTPS redirect are off.

## Localization (Arabic default, English at `/en`)
**The URL is the only thing that decides the language.** `/` and every path outside `/en` is Arabic (RTL);
`/en` and `/en/...` is English (LTR). `Cultures.Configure` replaces the request-culture provider list with
`UrlCultureProvider` alone, so a query string, a cookie or `Accept-Language` never changes a page, and `/` never
redirects. Only `/` and `/en` are indexable (`CultureUrls.Indexable`). Non-canonical spellings get a 301 from
`CultureUrlNormalizer` (`/ar`, `/Home` -> `/`; `/EN`, `/en/`, `/en/Index` -> `/en`; the query string is kept).
The public forms post to `/Home/Register` + `/Home/Contact` (Arabic page) and `/en/Register` + `/en/Contact`
(English page); `CultureUrls.FormAction` builds the right one. The admin panel stays Arabic; its content forms have
an Arabic and an English field.

### Adding or changing a UI string (validation messages, form labels, page chrome)
1. Add a `const` to `Infrastructure/Localization/MessageKeys.cs` (`Msg_...`, `Field_...`, `Admin_...` or `Ui_...`).
2. Add the same key to **both** `Resources/SharedResource.resx` (neutral file = Arabic, also the fallback) and
   `Resources/SharedResource.en.resx`, as `<data name="Key" xml:space="preserve"><value>...</value></data>`.
3. Use it: in a view `@L[K.Ui.ContactSubmit]` (`L` and `K` come from `Views/_ViewImports.cshtml`); in an input
   model `ErrorMessage = K.Required` / `[Display(Name = K.Fields.FullName)]`.
4. A unit test fails if a `MessageKeys` constant is missing from either file or the two files
   have different keys (resx parity). The public Razor views must not contain Arabic letters: every visible string
   is a resx key or database content.

### Content that editors manage (database, not resx)
Each text has an Arabic and an English value:
- **Entities** (nav, platforms, stats, news, events, partners, FAQ, interest options, register benefits): columns
  `...Ar` + `...En` (`TitleAr`, `TitleEn`, ...). `...Ar` is required, `...En` is nullable.
- **`SiteContents`** (one row per key, `ContentAr` + `ContentEn`): the keys are the registry
  `Infrastructure/Content/ContentKeys.cs` (each with its admin group, label, max length, format and rule).
  The admin content editor is generated from the registry, and only registry keys can be saved. A key marked
  `Neutral` has one value for both languages (phone, e-mail, URLs).
- **Fallback (one rule, `Bilingual.Pick`)**: on `/en`, an English value with any non-whitespace character is shown;
  otherwise the Arabic value is shown and the element is marked `lang="ar" dir="rtl"`. `/` always shows Arabic.
  `Bilingual.Optional` has no fallback (the English page shows nothing when English is empty, e.g. the hero kicker).
  Views never compare languages themselves.
- Admin forms use the `<lang-pair ...>` tag helper (Arabic field first, English field with the fallback hint).
- Timestamps are stored in UTC and rendered as `<time class="utc-time" datetime="...Z">` (`TimeDisplay.Utc`);
  `wwwroot/js/local-time.js` shows them in the viewer's own time zone.
- The home page reads one cached snapshot (`HomeContentCache`); an admin save invalidates it
  (`ContentCacheInvalidationInterceptor`), so a change shows in both languages on the next request.

### Migration workflow (SQL Server, LocalDB only)
```powershell
# 1. change the model, then create the migration (uses LocalDB when no environment is set; see the section below)
dotnet ef migrations add <Name>
# 2. read the generated Up/Down. A property rename that EF could not pair becomes DropColumn + AddColumn = DATA LOSS.
#    It must be a RenameColumn (sp_rename) instead. Fix the .cs by hand.
# 3. produce the SQL exactly as production will run it and look for DROP COLUMN / DROP TABLE / DELETE
dotnet ef migrations script <PreviousMigration> <Name> --no-build -o migration-up.sql
Select-String -Path migration-up.sql -Pattern 'DROP COLUMN|DROP TABLE|DELETE FROM|sp_rename'
# 4. test it on LocalDB against a copy of realistic data, never on production
dotnet ef database update
```
For `AddBilingualContent` the check printed: 17 `sp_rename`, 0 `DROP COLUMN`, 0 `DROP TABLE` (the output is identical to
the script kept in the PR-B work notes). Seed data that existing databases need goes in the migration as an insert
guarded by `WHERE NOT EXISTS` (`DbSeeder` runs on every start but does not seed `SiteContents` any more); never overwrite
an existing row. The app applies pending migrations at start (`MigrateAsync()`), so a deploy needs a database backup first.

Verified on 2026-10-22 by running `dotnet build InnovestHolding.sln --nologo` (0 warnings, 0 errors) and
`dotnet ef migrations script 20260601163804_InitialCreate AddBilingualContent --no-build`
(17 `sp_rename`, 0 `DROP COLUMN`). The `migrations add` and `database update` commands were not re-run for this note.

### Static assets: compression and caching
`Infrastructure/StaticAssets.cs`: Brotli/Gzip only for requests under `/css`, `/js`, `/assets`, `/lib` and only for
css, js, svg and json; HTML pages and form JSON are never compressed. `Cache-Control: public, max-age=31536000,
immutable` is set only on URLs with `?v=` (added by `asp-append-version` on the CSS/JS tags), so edit a static file and
the tag's `?v=` changes with it; URLs without `?v=` keep the default caching. The Cairo font is requested with only the
weights in use (public pages 400/600/700/800/900, admin 400/600/700/800); add a weight to the Google Fonts URL in the
layout if a new `font-weight` needs one.
