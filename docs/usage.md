# Работа

← [README](../README.md)

Панель: `https://<наш-домен>/cp/` — сайты, стата, настройки. Save в редакторе сразу перечитывает реестр и чистит текстовый кэш (рестарт не нужен).

## Трафик с `flow=`

Кампания задаётся **query** на любой HTML-странице (обычно главная):

```
https://4tube.xyz/?flow=camp1
https://4tube.xyz/video/123/?flow=camp1
```

| Что | Как |
|-----|-----|
| Параметр | `flow` — имя кампании (до 100 символов) |
| Cookie | `sf`, **30 дней**, Path `/` |
| Пустой / `undefined` / `null` / `none` | пишется **`_default`** (органика тоже считается) |
| Повторный заход | cookie уже есть — `flow=` не обязателен |
| Новый `?flow=` | cookie перезаписывается |

Опционально: `t1` / `target1`, `t2` / `target2` → cookies `st1` / `st2`.

В стате (`/cp/stats`) колонка **Flow** — это это имя. Фильтр по flow / domain.

Уникальность Hit / Video / Play / Goal: **UTC-день + IP + flow + домен** (не «сколько раз открыл»). NAT = один IP.

## Метрики

| Колонка | Что |
|---------|-----|
| **Hit** | любой HTML (включая `/video`) |
| **Video** | заход на `/video` или `/video/…` |
| **Play** | клик в плеере (`userPlay`) — тот же момент, что старт таймера 30с; не автоплей, не превью |
| **Goal** | Hephaestus `POST /internal/track/goal` `{ "ip": "…" }`; повтор с того же IP не раньше **24ч** |

Домен в строке — последние две метки Host (`www.4tube.xyz` → `4tube.xyz`) **только при записи**.

## Редактор сайта (`/cp` → Add / Edit)

Ключ сайта — **наш домен**. Save → `hephaestus_sites_data/{профиль}/sites.json`.

### Identity

| Поле | Смысл |
|------|--------|
| **Target host** | Наш хост, ключ JSON. После создания не меняется |
| **Source host** | Откуда проксируем (upstream) |
| **Display name** | Подпись в списке, на работу не влияет |
| **Source upstream host** | Host-заголовок к источнику; пусто = source host |
| **Extra target hosts** | Доп. имена (www, алиасы) на тот же сайт |

Если для source есть **coded module** — JSON сохраняется, часть поведения может перекрыть код.

### Proxy

| Поле | Смысл |
|------|--------|
| **Pass cookies** | Куки браузер ↔ upstream (антибот). По умолчанию вкл |
| **Disable disk caching** | Не писать/читать `/_cache` для этого сайта |

### Routing

| Поле | Смысл |
|------|--------|
| **External redirect URL** | Куда уводят `/go`, `/out`… Пусто = главная нашего сайта |
| **Enable outbound redirect paths** | Вкл. увод по префиксам |
| **Outbound redirect paths** | Префиксы (`/go`, `/out`, `/click`) |
| **Blocked path prefixes** | Эти пути на upstream не проксируются |
| **Redirect foreign requests** | Чужие ссылки/редиректы переписываем на наш сайт |
| **Foreign redirect URL** | Куда при foreign. Пусто = главная |

### Контент

| Поле | Смысл |
|------|--------|
| **Content replacements** | From → To поверх автозамены хостов. Word boundary — только целое слово |
| **HTML injections** | Вставка HTML. Пути: `*` все, `/` главная, `/video` и всё под ним. Позиция: начало `<head>`, перед `</head>`, перед `</body>` |
| **Local asset aliases** | Файлы из `wwwroot/{targetHost}/` и так отдаются по тому же пути. Таблица — только лишние URL-алиасы |
| **Settings** | Ключ → значение в JSON; в локальных `.js` подставляется `$Key$`. Типично: `VideoInterval` (сек до попапа), `DownloadUrl`, `DisablePlayerEvents` (`true` = не вешать события плеера) |
