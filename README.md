# sites

Прокси-сайты + CP `/cp`. Профиль — соседний `profile.txt`.

```
parent/
  profile.txt
  hephaestus_sites/         ← код
  hephaestus_sites_data/    ← сайты, wwwroot, settings
```

`sites.json`: ключ — наш домен (`targetHost`), `sourceHost` — источник. Модуль в `src/Sites.Modules` перекрывает JSON, если `sourceHost` совпал.

## Деплой на VPS

`deploy.bat` — все хосты из `deploy/install-remote-creds.txt` параллельно (host / login / password / profile). На сервере: git + .NET (пропуск, если уже есть) → clone кода и данных → PostgreSQL (**SQL каждый раз, база `sites` пересоздаётся**) → publish → systemd `sites-host`.

| Скрипт | Что делает | Аргументы |
|--------|------------|-----------|
| `deploy.bat` | Удалённый деплой | `[профиль]` (остальное — в CLI) |
| `deploy_profile.bat` | Спросит профиль, затем `deploy.bat` | дальше как deploy |
| `deploy/install-remote.txt` | То, что крутится по SSH | env: `SITES_PROFILE`, git URL |
| `deploy/install-local.sh` | Данные + postgres + publish + systemd | нужен `SITES_PROFILE` |
| `deploy/install-data.sh` | Клон/reset `hephaestus_sites_data` | — |
| `deploy/install-postgres.sh` | Пакет postgres (skip если есть) + SQL | — |
| `deploy/update.sh` | На сервере: git reset + publish + restart | `[профиль]` (по умолчанию `default`) |

DNS доменов должен смотреть на VPS. Сертификаты Let's Encrypt — при старте `sites-host`.

## Локально

| Скрипт | Что делает | Аргументы |
|--------|------------|-----------|
| `run.bat` | `Sites.Host` (Development, без HTTPS) | без аргументов — все сайты; `[хост]` или `--sitename хост` — один сайт на `:5000` |
| `certs.bat` | CertTool (Let's Encrypt → `cert/sites.pfx`) | без аргументов = `publish`; иначе `publish`, `check`, `publish --staging`… |
| `clearcache.bat` / `.sh` | Диск-кэш прокси (`C:\_cache` / `/_cache`) | — |
| `push.bat` | commit + push | `[сообщение]` (по умолчанию `Update`) |
| `pull.bat` | `git pull` | — |

```bash
dotnet build src/Sites.sln
dotnet run --project src/Sites.Host
```

## Трекинг (кратко)

`?flow=` → cookie `sf` 30 дней; пустой/мусорный flow → `_default`. Домен = последние две метки хоста **только при записи**. Hit / Video (`/video`) / Play (`POST /_s/e`, не Video) / Goal (Hephaestus `POST /internal/track/goal` `{ip}`). Hold 24ч (`goal_at`). Стата: `/cp/Stats`.
