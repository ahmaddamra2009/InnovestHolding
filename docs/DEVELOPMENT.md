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
