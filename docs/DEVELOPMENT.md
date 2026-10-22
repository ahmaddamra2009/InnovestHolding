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
misspelt environment name reads `appsettings.json` - the production connection. Always start with
`--launch-profile http`.

Since the production-database guard (`Infrastructure/ProductionDbGuard.cs`), such a run **stops before it connects**,
in every environment: a connection that is not `(localdb)\...` is accepted only when the process is hosted by IIS (any of
`APP_POOL_ID`, `ASPNETCORE_IIS_PHYSICAL_PATH`, `ASPNETCORE_TOKEN` is set; none is set by `dotnet run` or `dotnet app.dll`)
or when `INNOVEST_ALLOW_REMOTE_DB=1` is set. The variable that allowed a remote database is logged as a Warning at start. On a developer
machine neither is set, so the app (and `dotnet ef`, which runs `Program` up to `Build()`) throws
`ProductionDbGuardException` at configuration time, before any connection, migration or seeding. The same rule is applied
again to the effective DbContext connection before migrating, and on **every connection open** by
`ProductionDbGuardInterceptor` (registered in `AddAppDbContext`). That last check is what stops
`dotnet ef … --connection <remote>`, which replaces the string after start-up (a migrations bundle was not tested). It
covers only contexts built through `AddAppDbContext`: a hand-built `DbContextOptionsBuilder` (the tests have some), a
future design-time factory, or opening `GetDbConnection()` directly is not checked by it. It runs before SqlClient, so
before any name resolution or TCP connect. "LocalDB" means a `(localdb)\...` data source with **no** `Failover Partner` and no
`MultiSubnetFailover` (a failover partner is a second server SqlClient may connect to). The message never contains the
connection string. Whether IIS Express sets the IIS markers has not been checked (there is no IIS Express launch
profile); if it does, a run under IIS Express is treated as an IIS host, so do not point IIS Express at a remote
database. **Never set `INNOVEST_ALLOW_REMOTE_DB` on a developer machine**; it exists for a production host that is not
IIS. Checked on 2026-10-22: the app built from this tree, started with
`ASPNETCORE_ENVIRONMENT=Production` and the fake host `Server=example.invalid`, exited with that exception from
`Program.cs` (configuration) and printed neither the host nor the password. On 2026-10-23,
`ef database update --connection "Server=tcp:example.invalid,…"` against a build of this tree exited 1 with the
guard's `(connection)` refusal, without a connection attempt and without printing the host. The guard is tested in
`InnovestHolding.Tests/ProductionDbGuardTests.cs` and `ProductionDbGuardConnectionTests.cs`.

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

## Public site structure (PR-C1)

### Route table
`Infrastructure/Site/SiteRoutes.cs` is the only list of public pages: 19 entries (`Key`, Arabic `Path`, `Parent`,
`Controller`, `Action`, `Kind` = Static | List | Form). `Program.cs` maps each entry twice (`<key>.ar` at the path,
`<key>.en` at `/en` + path; the home page is `/` and `/en`), GET/HEAD only. Menus, breadcrumbs, the unit tab bar, the
sitemap, the URL normaliser, the language switch and the admin Pages tree all read the table, and `CultureUrls.Page`,
`Alternate` and `Form` are the only URL builders (never write a path by hand in a view). Mapping order in `Program.cs`:
`Admin/{controller}/{action}` (admin controllers only) -> the 38 page routes -> the default route
`{controller}/{action}/{id?}` constrained to `Home|Account` -> `MapFallbackToController("{*path:nonfile}")`, the
branded 404 in the URL's language (`noindex, nofollow`). Because of the constraint `/Dashboard`, `/Events`, `/Faq`... are
the 404 page, and every admin URL starts with `/Admin/`.

| Key | Arabic path | Controller.Action | Kind |
|---|---|---|---|
| `home` | `/` | Home.Index | Static |
| `about` | `/about` | SitePages.About | Static |
| `ecosystem` | `/ecosystem` | SitePages.Ecosystem | Static |
| `forum` | `/forum` | Forum.Index | Static |
| `forum.events` | `/forum/events` | Forum.Events | List |
| `forum.speakers` | `/forum/speakers` | Forum.Speakers | List |
| `forum.partners` | `/forum/partners` | Forum.Partners | List |
| `forum.registration` | `/forum/registration` | Forum.Registration | Form |
| `network` | `/angel-network` | Network.Index | Static |
| `network.investors` | `/angel-network/investors` | Network.Investors | Static |
| `network.startups` | `/angel-network/startups` | Network.Startups | Static |
| `network.opportunities` | `/angel-network/opportunities` | Network.Opportunities | List |
| `network.join` | `/angel-network/join` | Network.Join | Form |
| `tv` | `/tv` | Tv.Index | Static |
| `tv.episodes` | `/tv/episodes` | Tv.Episodes | List |
| `tv.guests` | `/tv/guests` | Tv.Guests | List |
| `tv.sponsorship` | `/tv/sponsorship` | Tv.Sponsorship | Form |
| `news` | `/news` | Newsroom.Index | List |
| `contact` | `/contact` | SitePages.Contact | Form |

English path = `/en` + Arabic path. Parents: `about`, `ecosystem`, `news`, `contact` under `home`; `forum`, `network`, `tv`
under `ecosystem`; the rest under their unit. Every page action is `[AcceptVerbs("GET", "HEAD")]` and ends in
`Page(key, views, blocks, emptyAction)` (`PublicControllerBase`), which renders `Views/Site/Page.cshtml` from the block
partials in `Views/Shared/Blocks/`. A page with no content block and no intro/body renders the admin's "coming soon" text
(`EmptyBlock`). List pages paginate with `?page=N` (`list_page_size`, default 12; `?page=1` -> 301 to the bare path, out of
range or garbage -> 404).

### Adding a page (both languages)
1. Add a key to `PageKeys` and an entry to `SiteRoutes.All` (path lower-case, no trailing slash, a parent, a `PageKind`).
   `Program.cs` maps the Arabic and the `/en` route from it; nothing to change there.
2. Add the controller action (or a new `PublicControllerBase` controller) that builds the blocks and returns
   `Page(PageKeys.X, v, blocks)`. A page of `PageKind.List` whose rows are not events, news or forum partners must also be
   counted in `SeoPolicy.ListCountOf`, otherwise its count is 0 and it stays `noindex`.
3. Ship its `Pages` row in a migration: insert only `WHERE NOT EXISTS (SELECT 1 FROM Pages WHERE [Key] = ...)` with frozen
   literals (Arabic and English title, summary, meta title and description). Without the row the page renders but is
   `noindex` and the admin shows "create". `DbSeeder` does not write `Pages` or `ContentItems`.
4. UI strings go to `MessageKeys` + both resx files (below); texts an admin edits live in `Pages`, `SiteContents`
   (`ContentKeys`) or `ContentItems` (`ContentItemGroups`).
5. Tests: `SiteStructureTests` loops over `SiteRoutes.All` in both languages (200, one `<h1>`, language switch, in-language
   links), so a new page is covered; add a test for any rule of its own. The sitemap and the robots decision
   (`SeoPolicy.Decide`) come from the same table.

### Public forms under `/forms/*`
Form posts left the page URLs so `/en/contact` can be a page. `FormsController` has exact attribute routes, POST only:
`/forms/register`, `/forms/contact` (Arabic page) and `/en/forms/register`, `/en/forms/contact` (English page), plus the
aliases `/Home/Register` and `/Home/Contact`, kept for one release for pages cached before the deploy. They carry
`[JsonFormEndpoint]` (the 429 handler answers JSON), `[EnableRateLimiting(PublicForm)]`, `[RequestSizeLimit(32 KB)]`, the
global antiforgery filter and the honeypot; every URL of one action shares one rate-limit bucket. `robots.txt` disallows
`/forms` and `/en/forms`. In views build the URL with `CultureUrls.Form("contact", english)`.

### Brand: tokens and fonts
Tokens are CSS variables on `:root` at the top of `wwwroot/css/style.css`: `--navy-deep #111145`, `--navy-brand #233082`,
`--orange #F78610`, `--orange-ink #AD5600` (the orange for small text on a light background), `--cream #F4F1EA`. The old
`--navy*` names are aliases, and every former `--gold*` is now `--accent*`. Orange buttons carry navy text. Fonts: one
Google Fonts `<link>` in the layouts, Cairo (Arabic) plus Montserrat (English; `html[lang=en]` puts it first); add a
weight to that URL if a rule needs one. No font files are committed. Logos and icons are in `wwwroot/assets/brand/`
(`logo-on-dark` and `logo-on-light` as png/webp/@2x, `favicon.svg`, `favicon.ico`, `apple-touch-icon.png`,
`og-default.jpg` 1200x630); they are raster images keyed from the two supplied JPEGs, and the vector logo has not been
delivered. The admin palette is in `wwwroot/css/admin.css`.

## Localization (Arabic default, English at `/en`)
**The URL is the only thing that decides the language.** `/` and every path outside `/en` is Arabic (RTL);
`/en` and `/en/...` is English (LTR). `Cultures.Configure` replaces the request-culture provider list with
`UrlCultureProvider` alone, so a query string, a cookie or `Accept-Language` never changes a page, and `/` never
redirects. Indexability is decided per page and per request, not by a path list: `SeoPolicy.Decide` returns Index or
`noindex, follow` from the page's data (a list with no active item, a page with neither intro nor body, and an English
page without English meta title and description are `noindex, follow`). The `X-Robots-Tag` header, the page's robots meta,
hreflang (only when both languages index) and the sitemap all read that one decision; a response with no decision is
`noindex, nofollow`. Non-canonical spellings get one 301 from `CultureUrlNormalizer` (lower-case, trailing slash, a leading
`/ar`, `/Home`, `/Home/Index`, `/en/Index`; the query string is kept), and only when the target is a route-table path;
anything else is the 404. The public forms post to `/forms/*` (see above); `CultureUrls.Form` builds the right URL. The
admin panel stays Arabic; its content forms have an Arabic and an English field.

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
- **Entities** (nav, platforms, stats, news, events, partners, FAQ, interest options, register benefits, `Pages`,
  `ContentItems`): columns
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
- Every public page reads one cached snapshot (`ISiteContentCache` / `SiteContentCache`: SiteContents, Pages,
  ContentItems, active platforms and partners, list counts); static pages and the shell cost 0 queries on a hit, list
  pages query their rows paged. An admin save invalidates it (`ContentCacheInvalidationInterceptor`), so a change shows in
  both languages on the next request.

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

### Content migrations: guard every column on its own
A migration that rewrites text an admin can edit (PR-C1's `ApplyCompanyProfile`) must never overwrite an admin edit.
The pattern, in `Migrations/20261022153329_ApplyCompanyProfile.cs`:
- **Find the row by its identity columns** (the Arabic label/title/description, the figure value, the key) compared with
  the OLD seed values. If an admin edited one of them, the whole row is left untouched, so a new label can never end up
  next to an old value.
- **Guard each other column by itself:** `Same(col, old)` is `col = CAST(old AS nvarchar(max)) COLLATE ...BIN2 OR
  (col IS NULL AND old IS NULL)` (NULL-safe, binary), and `Guarded(col, old, new)` is
  `col = CASE WHEN Same(col, old) THEN new ELSE col END`. One condition per row ("the Arabic is still the seed") is NOT
  enough: it overwrites an English or short-label edit made on a row whose Arabic was never touched.
- **New rows:** insert only `WHERE NOT EXISTS` (a page by `Key`, a list group only while the group is empty); never update.
- **`Down` mirrors Up per column:** a column goes back only while it still equals what Up wrote.
- **Freeze the literals** in the migration (copy them; never read application code or the seeder), and write line breaks
  as `NCHAR(10)`: EF's SQL Server generator re-joins `Sql()` lines with the machine newline, which stored CRLF on Windows.
- **Tests** (`CompanyProfileMigrationTests`): run the migration on an unedited database and on one with an admin edit in
  each guarded column (English, short, suffix, icon, flag, and each identity column), and assert the edited cells keep
  their values while the rest equals the unedited result; then break one guard and see the test go red.
After generating the script (`dotnet ef migrations script`), the up script must show 0 `DROP COLUMN`, 0 `DROP TABLE`,
0 `DELETE FROM`; PR-C1's four migrations have 3 `DROP INDEX` (the News, Events and Platforms single-column sort indexes, replaced by composite ones) and no data deleted.

Verified on 2026-10-22 for `AddBilingualContent` by running `dotnet build InnovestHolding.sln --nologo` (0 warnings, 0 errors) and
`dotnet ef migrations script 20260601163804_InitialCreate AddBilingualContent --no-build`
(17 `sp_rename`, 0 `DROP COLUMN`). The `migrations add` and `database update` commands were not re-run for this note.

### Static assets: compression and caching
`Infrastructure/StaticAssets.cs`: Brotli/Gzip only for requests under `/css`, `/js`, `/assets`, `/lib` and only for
css, js, svg and json; HTML pages and form JSON are never compressed. `Cache-Control: public, max-age=31536000,
immutable` is set only on URLs with `?v=` (added by `asp-append-version` on the CSS/JS tags), so edit a static file and
the tag's `?v=` changes with it; URLs without `?v=` keep the default caching. The Cairo font is requested with only the
weights in use (Cairo 400/600/700/800 and Montserrat 500-800 on the public pages; Cairo 400/600/700/800 and Montserrat 500/600/700 in the admin); add a weight to the Google Fonts URL in the
layout if a new `font-weight` needs one.

Verified on 2026-10-22 (PR-C1 sections: route table, forms, brand, migration guard pattern) by reading the code on branch
`feature/3-new-structure-brand` (uncommitted C1). The warning gate is a clean build:
`dotnet build InnovestHolding.sln --nologo --no-incremental` must report 0 warnings (an incremental build reuses compiled
projects and hides their warnings; an earlier version of this note quoted an incremental build). The C1 migration scripts
are not kept in the repository; generate them to check a release, e.g.
`dotnet ef migrations script 20261022120642_AddBilingualContent 20261022172550_RefineSiteStructure -o migration-C1.sql`
(and the reverse range for the down script). The up script must contain 0 `DROP COLUMN`, 0 `DROP TABLE` and 0 `DELETE`.
The app and the tests were not run for this note.

### Front-end pitfalls (PR-C1)
- A scroll-reveal element uses the class `active` for "shown" (`.reveal.active`). Never reuse `active` as the open state of a component that is also a `.reveal` element: the FAQ accordion uses `is-open` for that reason (using `active` hid every item after one click). Check such interactions with motion ON; under `prefers-reduced-motion` `.reveal` stays opaque and the bug cannot show.
- `ContentText.ListItems` splits a content text on the Arabic comma and the Latin comma into chips (fewer than 2 items: no list); the home values card uses it, vision and mission never do.
