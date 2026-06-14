# sites

## Repository layout

```
hephaestus_sites/
  src/           — solution and C# projects (Sites.sln)
  deploy/        — remote install scripts + creds
  output/        — shared build output
  release/       — linux publish output (systemd WorkingDirectory)
  sites.json     — site registry (targetHost keys)
  wwwroot/       — static /x/ additions per domain
  cert/          — TLS material
  deploy.bat     — one-click remote deploy
```

## Remote deploy (local machine → VPS)

```bat
deploy.bat
```

What happens on the VPS (over SSH):

1. Install `git` + .NET 10 SDK/runtime (apt)
2. `git clone` / `git pull` from GitHub
3. `dotnet publish` → `~/hephaestus_sites/release/`
4. Restart `sites-host` systemd service

Credentials: `deploy/install-remote-creds.txt` (3 lines: host, login, password). Defaults in `src/Sites.Deploy/appsettings.json`.

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

`Sites.Host` auto-renews Let's Encrypt certificates (no cron).

## Optional manual cert tool

```bash
dotnet run --project src/Sites.CertTool -- check
dotnet run --project src/Sites.CertTool -- publish --staging
```

## Site configuration (`sites.json`)

Root `sites.json` — keys are **targetHost** (our publish domain):

```json
{
  "tube-18.xyz": { "sourceHost": "tube18.sex" },
  "veryoldgames.xyz": { "sourceHost": "bestoldgames.net" }
}
```

Optional coded `SiteModuleBase` in `src/Sites.Modules` overrides JSON when `sourceHost` matches.

## Control panel (`/cp`)

`https://tube-18.xyz/cp/` — CRUD over root `sites.json`, live registry reload.

Optional password: `Cp:AdminPassword` in `src/Sites.Host/appsettings.json`.
