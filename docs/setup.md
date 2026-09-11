# Установка и деплой

← [README](../README.md)

## Раскладка

```
parent/
  profile.txt                 активный профиль
  hephaestus_sites/           код
  hephaestus_sites_data/      сайты, wwwroot, settings.json
```

Ключ в `sites.json` — наш домен (`targetHost`). `sourceHost` — откуда проксируем. Код-модуль в `src/Sites.Modules` перекрывает JSON, если `sourceHost` совпал.

DNS доменов должен смотреть на VPS. Let's Encrypt — при старте `sites-host`.

## VPS

`deploy.bat` — все хосты из `deploy/install-remote-creds.txt` **параллельно** (host / login / password / profile). Пароли в git не класть.

На сервере: git + .NET (пропуск, если уже стоят) → clone кода и данных → PostgreSQL (**SQL каждый раз, база `sites` пересоздаётся**) → publish → systemd `sites-host`.

| Скрипт | Что | Аргументы |
|--------|-----|-----------|
| `deploy.bat` | SSH-деплой всех хостов | `[профиль]` |
| `deploy_profile.bat` | Спросит профиль, затем deploy | как deploy |
| `deploy/install-remote.txt` | Скрипт на VPS по SSH | `SITES_PROFILE`, git URL |
| `deploy/install-local.sh` | Данные + postgres + publish + systemd | нужен `SITES_PROFILE` |
| `deploy/install-data.sh` | Клон/reset `hephaestus_sites_data` | — |
| `deploy/install-postgres.sh` | Пакет postgres (skip если есть) + SQL | — |
| `deploy/update.sh` | На сервере: git reset + publish + restart | `[профиль]` (по умолчанию `default`) |

Полный `deploy` / `install-local` **сначала стопит** `sites-host`, потом DROP базы — иначе старые визиты запишутся обратно.

## Локально

| Скрипт | Что | Аргументы |
|--------|-----|-----------|
| `run.bat` | `Sites.Host` (Development, без HTTPS) | пусто — все сайты; `[хост]` или `--sitename хост` — один на `:5000` |
| `certs.bat` | Let's Encrypt → `cert/sites.pfx` | пусто = `publish`; иначе `publish`, `check`, `publish --staging` |
| `clearcache.bat` / `.sh` | Диск-кэш (`C:\_cache` / `/_cache`) | — |
| `push.bat` | commit + push | `[сообщение]` (по умолчанию `Update`) |
| `pull.bat` | `git pull` | — |

```bash
dotnet build src/Sites.sln
dotnet run --project src/Sites.Host
```
