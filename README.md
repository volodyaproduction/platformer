# 2D-платформер на Unity 6

**Играть:** https://lsh-platformer.vercel.app/

2D-платформер: бежишь по уровню, собираешь монеты, прыгаешь через ямы, обходишь шипы. Цель — набрать как можно больше монет за 30-секундный раунд (раунд заканчивается также по падению в яму или касанию финишного флага). Развёрнуто на Vercel через WebGL — играется прямо в браузере. Есть глобальный лидерборд, общий с [шутером](https://lsh-shooter.vercel.app/). Собрано на **Unity 6.3 LTS (6000.3.15f1)** в headless-режиме — Unity Editor вручную не открывался.

---

## Сборка и запуск

### 1. Сборка веб-билда

```bash
cd platformer
./build.sh        # web — дефолт
```

Результат — в `web/`, ~9 МБ после Brotli. Скрипт ожидает Unity по дефолтному пути Unity Hub; иначе `UNITY=/path/to/Unity ./build.sh`.

### 2. Локальный запуск в браузере

WebAssembly не грузится с `file://` — нужен HTTP. Любой статический сервер в `web/`, например:

```bash
cd web && python3 -m http.server 3000
# http://localhost:3000/
```

### 3. Сборка `.exe` для Windows

```bash
cd platformer
./build.sh win
```

Результат — `build/Windows/Platformer.exe`. Кросс-билд с macOS работает.

### 4. Деплой на Vercel

**New Project** → выбрать репозиторий → **Output Directory = `web`** → **Deploy**. Лидерборд требует Upstash Redis: **Storage → Create Database → Upstash for Redis** → подключить к проекту → Redeploy. Vercel сам пропишет `KV_REST_API_URL` и `KV_REST_API_TOKEN`. Одну базу можно подключить к обоим проектам — ключи разведены префиксом (`platformer:*`, `shooter:*`).

---

## Структура проекта

### Игровой код (попадает в билд)

```
Assets/_Project/Scripts/
├── Core/                                ← 347 LOC
│   ├── GameSession.cs                   ← singleton, таймер 30 с, события, аудио
│   ├── PlayerIdentity.cs                ← GUID игрока + текущее имя (PlayerPrefs)
│   └── LeaderboardClient.cs             ← HTTP-клиент сетевого лидерборда
├── Data/                                ← 70 LOC
│   └── LevelLayout.cs                   ← позиции монет/шипов + MaxScore (один источник)
├── Gameplay/                            ← 356 LOC
│   ├── PlayerController.cs              ← движение, прыжок, ground-check, Freeze, инвулн
│   ├── Coin.cs                          ← триггер сбора монеты
│   ├── Trap.cs                          ← шипы: –2 монеты + knockback
│   ├── KillZone.cs                      ← конец раунда при падении в яму
│   ├── FinishZone.cs                    ← конец раунда при касании флага
│   ├── TouchButton.cs                   ← экранные кнопки тач-управления
│   └── CameraFollowX.cs                 ← камера следит по X с фикс. Y
└── UI/                                  ← 742 LOC
    ├── HUD.cs                           ← счёт + таймер
    ├── FloatingText.cs                  ← всплывающий «+1»/«-2» с фейдом
    ├── GameOverPanel.cs                 ← итог раунда + диалог имени + сабмит
    ├── NameInputDialog.cs               ← ввод и смена имени
    ├── MenuController.cs                ← кнопки Старт / Никнейм / Лидерборд
    ├── PauseController.cs               ← пауза по ESC и бургер-кнопке
    └── LeaderboardView.cs               ← список топа, медали, t.me-ссылки
```

`GameSession` — event-driven: `ScoreChanged`, `TimeChanged`, `GameOver(score, reason)`. `HUD` и `GameOverPanel` подписаны на события, прямых ссылок «GameSession → UI» нет.

### Серверный код (Vercel serverless)

```
api/                                     ← 265 LOC
├── _redis.js                            ← общая обёртка над Upstash REST API
├── leaderboard-top.js                   ← GET — топ всех игроков по убыванию
├── leaderboard-save-score.js            ← POST — записать счёт (sanity cap + rate limit)
└── leaderboard-change-name.js           ← POST — задать/сменить имя (с проверкой уникальности)
```

### Editor-инфраструктура (в билд **не** попадает)

```
Assets/_Project/Scripts/Editor/          ← 1689 LOC
├── SceneBuilder.cs                      ← собирает Main.unity (1031 LOC)
├── SceneBuilderLeaderboard.cs           ← собирает Leaderboard.unity (234 LOC)
├── SceneBuilderMainMenu.cs              ← собирает MainMenu.unity (210 LOC)
├── BuildScript.cs                       ← BuildWebGL / BuildWindows (116 LOC)
├── AssetForge.cs                        ← рисует spike.png пиксельно (75 LOC)
└── KenneyTexturePostprocessor.cs        ← PPU=128 для Kenney и Art/Generated
```

**Что конкретно делает `SceneBuilder.cs`:** создаёт пустую сцену `Main.unity`, добавляет в неё камеру, игрока с Rigidbody2D и анимацией, тайлмап (земля и платформы для удлинённого уровня), монеты в конкретных координатах, KillZone и Ceiling под/над уровнем, флаг финиша, Canvas со счётчиком, таймером, GameOverPanel и диалогом ввода имени.

**Если бы открывали Unity Editor вручную, этой папки бы вообще не было:** камера, игрок, тайлмап, UI расставляются мышкой в Hierarchy, ссылки на компоненты протаскиваются в инспекторе. SceneBuilder'ы — это замена тех же действий через `EditorSceneManager.NewScene()`, `gameObject.AddComponent<>()`, `EditorUtility.SetDirty()`. `BuildScript.cs` нужен в любом случае — заменяет команду `File → Build`.

### Ассеты (внешние)

```
Assets/_Project/
├── Kenney/             ← спрайты CC0 (Player, Ground, Items, Backgrounds)
├── Audio/              ← jump.wav, coin.wav, victory.wav
└── Fonts/              ← Roboto-Regular.ttf (кириллица в UI)
```

### Сцена и сгенерированные ассеты

```
Assets/_Project/
├── Scenes/MainMenu.unity          ← пересоздаётся SceneBuilderMainMenu (Build Index 0)
├── Scenes/Main.unity              ← пересоздаётся SceneBuilder при сборке
├── Scenes/Leaderboard.unity       ← пересоздаётся SceneBuilderLeaderboard
├── Art/Tilemaps/                  ← Tile-ассеты, генерируются SceneBuilder
└── Prefabs/CoinPickupVfx.prefab   ← VFX-партикл, генерируется SceneBuilder
```

### Корень проекта

```
platformer/
├── ProjectSettings/    ← Unity-конфиги (тэги, слои, ввод, билд)
├── Packages/           ← манифест зависимостей Unity
├── web/                ← готовый WebGL-билд (для Vercel)
├── build/Windows/      ← локальный .exe-билд
└── build.sh            ← одношаговая сборка (web|win)
```

---

## Глобальный лидерборд

Все игроки соревнуются в одной таблице. Имена уникальны: если ник занят — сервер вернёт ошибку. Ник с `@` впереди (`@vova`) в таблице становится кликабельной ссылкой на `t.me/vova`.

### Как игра узнаёт «своего» игрока

При первом запуске генерируется **`player_id`** — случайный 128-битный GUID в `PlayerPrefs` (в WebGL это IndexedDB браузера). Сервер опознаёт игрока по нему: чужой счёт переписать нельзя без знания чужого `player_id`, а GUID неугадываем. Переживает деплой Vercel и перезагрузку браузера, не переживает чистку данных сайта и не синхронизируется между устройствами.

### Хранение в Redis

```
platformer:scores       ZSET  player_id  → лучший счёт          (топ строится отсюда)
platformer:names        Hash  player_id  → имя
platformer:name-index   Hash  имя_lower  → player_id            (для проверки уникальности)
platformer:rate:<id>    Key   1, EX 10                          (rate limit)
```

### Защита от накрутки

- **Sanity cap:** счёт выше серверного потолка (`200`) отклоняется. Реальный потолок — `LevelLayout.MaxScore = CoinPositions.Length` (`22` сейчас). Серверный cap намеренно крупнее, чтобы при добавлении монет не пришлось его править.
- **Rate limit:** один сабмит на `player_id` раз в 10 секунд. Атомарно через `SET NX EX 10` в Redis.

Отрицательный счёт валиден: после серии штрафов от шипов игрок реально уходит в минус и попадает в лидерборд как есть — sanity-cap проверяет только верхнюю границу.

GUID хранится в IndexedDB без HMAC-подписи — для дружеского платформера хватает. Серьёзный анти-чит (подписанные сессии, серверная игровая логика) — оверкилл для этого проекта.

---

## Спрайты и звуки

### Спрайты — [Kenney Platformer Pack Redux](https://kenney.nl/assets/platformer-pack-redux) (CC0)

- **Игрок** (стойка, ходьба ×2, прыжок) — `alienPink_stand.png`, `alienPink_walk1/walk2.png`, `alienPink_jump.png`
- **Земля и платформы** — `grassMid/Left/Right/Center.png` (полные тайлы), `grassHalf_left/mid/right.png` (тонкие платформы)
- **Монета** — `coinGold.png`
- **Флаг финиша** — `finishFlag.png`
- **Искра партикла** — `sparkle.png`
- **Фон (голубое небо)** — `Camera.backgroundColor`, отдельного спрайта нет
- **Шипы (`spike.png`)** — нарисованы программно в `AssetForge.cs`

Лицензионный текст — `Assets/_Project/Kenney/License.txt`.

### Звуки — [8-bit platformer SFX by tcpixel](https://opengameart.org/content/8-bit-platformer-sfx) (CC-BY 3.0)

- `jump.wav` — звук прыжка
- `coin.wav` — сбор монеты
- `victory.wav` — победа

---

## Unity CLI: как создавался проект

«Unity CLI» — это **не отдельный инструмент**, а тот же бинарник Unity, запущенный из терминала с `-batchmode`. Editor не показывается, а просто исполняется указанный C#-метод из `Assets/_Project/Scripts/Editor/`. Это способ заставить редактор сделать что-то без человека за мышкой.

### Что делает Unity CLI vs обычный C#

| Этап | Что делает агент | Unity CLI? |
|---|---|---|
| Игровая логика (`Core/`, `Gameplay/`, `UI/`) | пишет `.cs`-файлы как обычные исходники | ❌ не нужен |
| Сцена, тайлмап, монеты, UI, привязки | пишет `SceneBuilder.cs` (описание «как мышкой») | ✅ `SceneBuilder.Build` |
| Сборка WebGL / Windows | (готов `BuildScript.cs`) | ✅ `BuildScript.BuildWebGL/Windows` |

Без CLI всё это делалось бы вручную: тайлмап — мышкой в Hierarchy, привязки `onClick` — в инспекторе. 1689 строк Editor-папки заменяют эти клики на C#-код.

### Команда

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod BuildScript.BuildWebGL \
         -quit -logFile -
```

| Флаг | Обязателен | Что без него |
|---|---|---|
| `-batchmode` | да | Unity откроет GUI, команда зависнет |
| `-projectPath` | да | Unity не поймёт какой проект собирать |
| `-executeMethod` | да | нечего вызывать |
| `-quit` | да | Unity отработает метод и продолжит висеть |
| `-nographics` | нет | без него Unity создаёт скрытый GL-контекст. На CI без GPU упадёт. |
| `-logFile -` | нет | без флага логи в `~/Library/Logs/Unity/Editor.log`. С `-` — в stdout. |

`BuildScript.BuildWebGL/BuildWindows` сам вызывает `SceneBuilder.Build()` в начале, поэтому одной команды хватает на всё.

### Когда запускать `./build.sh` при разработке

Открывать Unity Editor не нужно ни на одном шаге — **любое изменение = правка C# → `./build.sh`** (логика, сцена, координаты монет, ассеты, ProjectSettings — всё одной командой).

---

## Что осталось

### Улучшения функциональности

- **Оффлайн-кэш лидерборда.** Сейчас при оффлайне таблица показывает «нет связи». В продакшене на ненадёжных сетях — fallback на `PlayerPrefs`-кэш последнего ответа.
- **Уровень в JSON/CSV** вместо хардкода в `LevelLayout.cs` — редактирование без перекомпиляции.
- **Звуки CC-BY 3.0** — для коммерческого использования нужна атрибуция; для CC0-only заменить из `kenney.nl/assets/sci-fi-sounds`.
- **Спрайт-анимация на Animator** — сейчас 2 кадра ходьбы в `Update()`, аниматор не оправдан, но при росте числа состояний — стоит.

### Рефакторинг архитектуры

- **`Editor/SceneBuilder.cs` — 1031 LOC в одном файле.** Смешивает сборку уровня (тайлмап + ямы + платформы), игрока, камеру, два VFX-префаба, всё UI (HUD/GameOverPanel/NameInputDialog/TouchButton/PausePanel/BurgerButton) и `GameSession`. Логически просится раскол на `SceneBuilderLevel.cs` + `SceneBuilderUI.cs` + общий `UiHelpers.cs` (как в шутере).
- **Дубликаты с шутером (~1300 LOC).** `api/_redis.js`, `leaderboard-*.js`, `Core/LeaderboardClient.cs`, `Core/PlayerIdentity.cs`, `UI/NameInputDialog.cs`, `UI/LeaderboardView.cs`, `UI/HUD.cs`, `UI/PauseController.cs`, `UI/MenuController.cs` отличаются только префиксом ключей Redis/PlayerPrefs и парой строк. Аккуратный путь — UPM-пакет в `Packages/com.silkin.shared/` или симлинки на корневой `_shared/`. Не сделано в этой итерации: миграция двух Unity-проектов на общий пакет требует отдельной проверки, что асемблии и meta-файлы переживут переход.
- **`isNewRecord` ложное срабатывание.** В `api/leaderboard-save-score.js` поле возвращается через `personalBest === score` — повторная отправка того же рекорда после rate-limit вернёт `true`, хотя `ZADD GT` не обновил запись. Лечится сравнением через `ZSCORE` до и после `ZADD`.
- **Нет автотестов на API.** Уникальность ника, rate-limit, повторный submit, граничные score проверялись руками. Минимум — Vitest с моком Upstash REST.
- **`web/Build/*.unityweb` в git** — раздувает репозиторий и историю коммитов. Билды лучше держать вне git и собирать в CI.
- **`TilemapCollider2D` вместо `CompositeCollider2D`** — даёт отдельный коллайдер на тайл (~50 тайлов, без проблем). Для большого уровня имеет смысл вернуть Composite, но решить проблему с batch-mode-генерацией геометрии.
