# sites

## Repository layout

```
parent/
  profile.txt                 — active profile name
  hephaestus_sites/           — this repo (code)
    src/                      — solution and C# projects
    deploy/                   — remote install + code PAT + data PAT
    output/  release/ cert/
  hephaestus_sites_data/      — sibling data repo (dynamic)
    {profile}/sites.json
    {profile}/settings.json
    {profile}/wwwroot/
```

## Remote deploy (local machine → VPS)

```bat
deploy.bat
```

Deploys every host in `deploy/install-remote-creds.txt` in parallel and prints a success/fail report.

What happens on each VPS (over SSH):

1. Validate the creds profile and overwrite `$HOME/profile.txt`
2. Install `git` + .NET 10 SDK/runtime (apt)
3. `git clone`/`reset` **code** (`hephaestus_sites`) and **data** (`hephaestus_sites_data`)
4. Install PostgreSQL and apply `deploy/setup-postgres.sql` (database `sites`, role `tss` / `123`). Each deploy **drops and recreates** `sites` (tracking data is wiped).
5. `dotnet publish` → `~/hephaestus_sites/release/`
6. Restart `sites-host` systemd service — on start it pulls data again and issues/renews Let's Encrypt for every host in that profile's `sites.json`

CP git pull/push syncs **hephaestus_sites_data** only (not the code repo), same pattern as Hephaestus + `hephaestus_data`.

Credentials: `deploy/install-remote-creds.txt` (host / login / password / profile per server). All hosts deploy in parallel; each target’s `$HOME/profile.txt` is overwritten. Defaults in `src/Sites.Deploy.Cli/appsettings.json`.

## Build layout

| Path | Purpose |
|---|---|
| `output/` | All project build output (`src/Directory.Build.props`) |
| `release/` | Linux publish — `sites-host` systemd `WorkingDirectory` |

```bash
dotnet build src/Sites.sln
dotnet msbuild src/Sites.Publish/Sites.Publish.csproj -t:PublishSites -p:PublishRuntimeIdentifier=linux-x64
```

## Development (local)

```bash
dotnet run --project src/Sites.Host
```

`appsettings.Development.json` disables HTTPS and cert maintenance.

## Production (VPS)

1. Point DNS for all site domains to the server.
2. Set `CertMaintenance:AcmeEmail` in `src/Sites.Host/appsettings.json` (real email — Let's Encrypt rejects `example.com`).
3. Deploy via `deploy.bat` (or run `Sites.Host` on the server after publish).

`Sites.Host` pulls `hephaestus_sites_data` and issues/renews Let's Encrypt on every start (deploy or reboot). The background loop checks again after 15s, then every 12 hours.

Tracking: `?flow=campaign` sets a cookie. HTML pages count `hit` / `/video` as `video`. Play beacons `POST /t/e`. Hephaestus later `POST /internal/track/goal` with `{ "ip": "..." }`. Memory flushes to Postgres every 2 minutes. An IP can convert once per 24h.

## Optional manual cert tool

```bash
dotnet run --project src/Sites.CertTool -- check
dotnet run --project src/Sites.CertTool -- publish --staging
```

## Site configuration (`hephaestus_sites_data/{profile}/sites.json`)

Keys are **targetHost** (our publish domain):

```json
{
  "tube-18.xyz": { "sourceHost": "tube18.sex" },
  "veryoldgames.xyz": { "sourceHost": "bestoldgames.net" }
}
```

Optional coded `SiteModuleBase` in `src/Sites.Modules` overrides JSON when `sourceHost` matches.

## Control panel (`/cp`)

`https://tube-18.xyz/cp/` — site list. Add is `/cp/edit`, edit is `/cp/edit?site=tube-18.xyz`. CRUD over `hephaestus_sites_data/{profile}/sites.json`, live registry reload. Git buttons push/pull the data repo.

Optional password: `Cp:AdminPassword` in `src/Sites.Host/appsettings.json`.
