# 2D-платформер на Unity 6

Игра-платформер для веба, собранная на **Unity 6.3 LTS (6000.3.15f1)**. Цель — пробежать уровень, собрать монеты и дойти до флага. Поддерживаются десктоп-браузеры и Windows.

## Как играть

- **WebGL (браузер):** ссылка добавится сюда после деплоя на Vercel
- **Windows .exe:** см. ниже инструкцию по сборке локально

**Управление:**
- Движение — `A` / `D` или стрелки `←` / `→`
- Прыжок — `Space` (доступен двойной прыжок: ещё один раз в воздухе)
- При падении в яму уровень рестартуется автоматически

## Функционал по ТЗ

- [x] Управление персонажем с клавиатуры
- [x] Прыжок с проверкой касания земли (`Physics2D.OverlapCircle` + `LayerMask Ground`)
- [x] Сбор монет, счётчик в UI («Монеты: N / total»)
- [x] Финиш-зона с надписью «Вы победили!» и кнопкой «Заново»
- [x] WebGL-билд для браузера
- [x] Windows-билд `.exe`

**Бонусы:**
- Двойной прыжок (`jumpsLeft = 2`, сбрасывается при касании земли)
- Эффект частиц при сборе монеты (жёлтые искры, авто-уничтожение)
- Звуковые эффекты: прыжок, сбор монеты, победа
- KillZone под уровнем — падение в яму = автоматический рестарт

## Архитектура

### Скрипты и их связи

```
PlayerController  ──▶ Rigidbody2D, ground-check, анимация спрайтов, звук прыжка
       │
       │ Player-тег
       ▼
  Coin (trigger)   ──▶ GameManager.AddCoin()  ──▶ +счётчик, звук, обновление UI
  KillZone         ──▶ GameManager.Restart()  ──▶ SceneManager.LoadScene
  FinishZone       ──▶ GameManager.Win()      ──▶ Win-panel, звук победы

GameManager (singleton):
  - scoreText, winPanel — обновляются напрямую
  - sfxSource — один AudioSource, играет PlayOneShot
  - coinClip, victoryClip — клипы
```

### Ключевые тонкости

- **Ввод в `Update`, физика в `FixedUpdate`** — на разных FPS поведение остаётся одинаковым.
- **`CompositeCollider2D` на тайлмапе** — один сплошной коллайдер на всю землю, нет «застреваний» в швах между плитками.
- **Тэг `Player` + слой `Ground`** настроены в `ProjectSettings/TagManager.asset`. Триггеры проверяют тег, ground-check — слой через `LayerMask`.
- **WebGL: `Decompression Fallback = ON`** — Unity сам распаковывает Brotli в JS, не зависит от заголовков хостинга (Vercel работает без `vercel.json`).

### Структура папок

```
Assets/_Project/
  Scenes/Main.unity         ← собирается из кода SceneBuilder.cs
  Scripts/
    Gameplay/               ← PlayerController, Coin, KillZone, FinishZone
    Systems/                ← GameManager
    Editor/                 ← SceneBuilder, BuildScript, KenneyTexturePostprocessor
  Art/Tilemaps/             ← Tile-ассеты, генерируются SceneBuilder
  Prefabs/CoinPickupVfx.prefab  ← VFX, генерируется SceneBuilder
  Kenney/                   ← спрайты (Player, Ground, Items, Backgrounds)
  Audio/                    ← jump.wav, coin.wav, victory.wav
web/                        ← WebGL-билд (для Vercel)
build/Windows/              ← Windows-билд (локально, не в git)
```

## Запуск и сборка локально

### Требования

- Unity Hub
- **Unity 6.3 LTS (6000.3.15f1)** с модулями **Web Build Support** и **Windows Build Support (Mono)**

### Открыть проект в Unity Editor

1. Клонировать репозиторий
2. В Unity Hub: `Open` → выбрать папку `platformer/`
3. Unity сам импортирует ассеты и откроет сцену `Assets/_Project/Scenes/Main.unity`
4. Нажать `Play`

### Сборка из CLI (как делается в этом репо)

Создание / пересборка сцены:
```bash
"/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . \
  -executeMethod SceneBuilder.Build \
  -quit -nographics -logFile /tmp/unity.log
```

Windows-билд (`.exe`):
```bash
"/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . \
  -executeMethod BuildScript.BuildWindows \
  -quit -nographics -logFile /tmp/unity-build-win.log
```

WebGL-билд:
```bash
"/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . \
  -executeMethod BuildScript.BuildWebGL \
  -quit -nographics -logFile /tmp/unity-build-web.log
```

Результаты: `build/Windows/Platformer.exe` и `web/index.html`.

## Деплой на Vercel

`web/` содержит готовый WebGL-билд, `index.html` в корне папки. Деплой:

```bash
cd web
vercel --prod
```

Либо через dashboard: подключить репо, root = `web/`. `vercel.json` не нужен — `Decompression Fallback = ON` делает Unity-лоадер независимым от заголовков сервера.

## Поддерживаемые платформы

- **Десктоп:** Chrome, Firefox, Safari, Edge — последние версии
- **Мобильный браузер:** Unity 6 официально поддерживает iOS Safari 15+ / Android Chrome 58+ ([Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-browsercompatibility.html)), но производительность зависит от устройства, отдельной оптимизации под мобайл нет.
- **Первая загрузка WebGL:** 5–15 секунд при размере билда ~9 МБ после Brotli.

## Источники ассетов и лицензии

- **Спрайты:** [Kenney Platformer Pack Redux](https://kenney.nl/assets/platformer-pack-redux), лицензия **CC0** (см. `Assets/_Project/Kenney/License.txt`)
- **Звуки:** [8-bit platformer SFX by tcpixel](https://opengameart.org/content/8-bit-platformer-sfx), лицензия **CC-BY 3.0**
