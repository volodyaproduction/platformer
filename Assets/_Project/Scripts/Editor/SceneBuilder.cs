using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Unity.Cinemachine;

// Программно создаёт Main.unity: тайлмап-уровень, игрок, монеты, KillZone,
// финиш, UI, Cinemachine-камера, VFX-префаб. Запускается через -executeMethod
// SceneBuilder.Build из CLI или через меню Tools.
public static class SceneBuilder
{
    // 1. Пути к ассетам
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    const string TilesDir = "Assets/_Project/Art/Tilemaps";
    const string PrefabsDir = "Assets/_Project/Prefabs";

    // 2. Расположение тайлов земли (x, y) — единый floor c двумя ямами,
    // три промежуточных платформы для двойного прыжка
    static readonly Vector2Int[] GroundTiles = BuildGroundTiles();
    static readonly Vector2Int[] PlatformTiles = BuildPlatformTiles();

    // 3. Координаты монет, шипов, стартовая точка и финиш вынесены в
    //    runtime-класс LevelLayout — он же является источником MaxScore для
    //    валидации на стороне клиента. Менять геометрию уровня → править
    //    LevelLayout.cs, пересборка через build.sh подхватит изменения.

    [MenuItem("Tools/Build Main Scene")]
    public static void Build()
    {
        EnsureDirectory(TilesDir);
        EnsureDirectory(PrefabsDir);
        EnsureDirectory("Assets/_Project/Scenes");

        // 5. Создаём пустую сцену
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 6. Главная камера (без Cinemachine — следим только за X)
        var mainCamera = CreateMainCamera();

        // 7. Игрок
        var player = CreatePlayer();

        // 8. Тайлмап со слоем Ground + платформы
        CreateTilemap();

        // 9. Привязываем камеру к игроку — только по X, Y фиксированный
        AttachCameraFollow(mainCamera.gameObject, player.transform);

        // 10. VFX-префабы: партикл сбора + всплывающий текст «+1/−2»
        var vfxPrefab = CreateCoinPickupVfxPrefab();
        var floatingTextPrefab = CreateFloatingTextPrefab();

        // 11. Монеты, привязанные к VFX
        SpawnCoins(vfxPrefab);

        // 11a. Шипы-ловушки (sprite сгенерирован AssetForge)
        SpawnTraps();

        // 12. KillZone под уровнем + FinishZone справа + Ceiling сверху
        CreateKillZone();
        CreateFinishZone();
        CreateCeiling();

        // 13. UI: Canvas со счётчиком, таймером, GameOverPanel и диалогом
        // имени для лидерборда + экранные кнопки тач-управления.
        var playerCtrl = player.GetComponent<PlayerController>();
        var ui = CreateUI(playerCtrl);

        // 14. GameManager + HUD: один объект, HUD подписан на события.
        CreateGameManager(ui.scoreText, ui.timerText, floatingTextPrefab);

        // Пауза по ESC: оверлей с кнопками «Продолжить» / «В меню»
        var canvasTr = GameObject.Find("Canvas").transform;
        CreatePausePanel(canvasTr);

        // 15. Связываем поля PlayerController после создания зависимостей
        ConfigurePlayerComponents(player);

        // 15a. LeaderboardClient — singleton с DontDestroyOnLoad. Обычно
        // создаётся в MainMenu (build index 0), но дублируем здесь на случай
        // прямого захода на сцену Main: дубликаты сами уничтожатся в Awake.
        var lbGO = new GameObject("LeaderboardClient");
        lbGO.AddComponent<LeaderboardClient>();

        // 16. Сохраняем сцену и добавляем её в Build Settings
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SceneBuilder] Сцена создана: " + ScenePath);
    }

    // ===== Уровень =====

    static Vector2Int[] BuildGroundTiles()
    {
        // Уровень длиной x≈100 с восемью ямами/провалами.
        var list = new List<Vector2Int>();
        AddRow(list, 0, 7, 0);     // старт
        AddRow(list, 11, 14, 0);   // после ямы 8..10
        AddRow(list, 18, 24, 0);   // после провала 15..17 (накрыт платформой)
        AddRow(list, 28, 34, 0);   // после ямы 25..27
        AddRow(list, 39, 44, 0);   // после ямы 35..38
        AddRow(list, 47, 52, 0);   // после провала 45..46
        AddRow(list, 56, 68, 0);   // длинный отрезок
        AddRow(list, 71, 75, 0);   // после ямы 69..70
        AddRow(list, 80, 85, 0);   // после провала 76..79 (накрыт платформой)
        AddRow(list, 89, 100, 0);  // финишный отрезок (после ямы 86..88)
        return list.ToArray();
    }

    static Vector2Int[] BuildPlatformTiles()
    {
        var list = new List<Vector2Int>();
        AddRow(list, 4, 6, 3);     // между стартом и первой ямой
        AddRow(list, 15, 17, 4);   // над провалом 15..17
        AddRow(list, 21, 22, 5);   // высокая — двойной прыжок
        AddRow(list, 30, 32, 4);
        AddRow(list, 36, 38, 4);   // над ямой 35..38
        AddRow(list, 41, 42, 5);   // высокая
        AddRow(list, 49, 50, 3);
        AddRow(list, 53, 55, 4);   // над провалом 53..55
        AddRow(list, 62, 64, 5);   // высокая
        AddRow(list, 76, 79, 4);   // над ямой 76..79
        AddRow(list, 82, 83, 5);   // высокая
        AddRow(list, 86, 88, 4);   // над ямой 86..88
        AddRow(list, 93, 94, 5);   // финальная высокая
        return list.ToArray();
    }

    static void AddRow(List<Vector2Int> list, int xStart, int xEnd, int y)
    {
        for (int x = xStart; x <= xEnd; x++) list.Add(new Vector2Int(x, y));
    }

    // ===== Камера и Cinemachine =====

    static Camera CreateMainCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.5f, 0.7f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = -10f;
        cam.farClipPlane = 100f;
        camGO.transform.position = new Vector3(0, 3f, -10);
        camGO.AddComponent<AudioListener>();
        return cam;
    }

    static void AttachCameraFollow(GameObject mainCamGO, Transform target)
    {
        // 24. Камера следит только по X с зафиксированным Y. Cinemachine
        // даже с большим Y-damping всё равно «всплывал» вверх при долгом
        // нахождении игрока в воздухе. Жёсткая фиксация Y — самый надёжный
        // способ всегда видеть землю.
        var follow = mainCamGO.AddComponent<CameraFollowX>();
        follow.target = target;
        follow.fixedY = 3f;
        follow.fixedZ = -10f;
    }

    // ===== Игрок =====

    static GameObject CreatePlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = new Vector3(LevelLayout.PlayerStart.x, LevelLayout.PlayerStart.y, 0);
        go.tag = "Player";

        // Спрайт
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/_Project/Kenney/Player/alienPink_stand.png");
        sr.sortingOrder = 10;

        // 25. Физика: Rigidbody2D + CapsuleCollider2D
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Спрайт игрока 133x184 при PPU=128 → 1.04x1.44 unit, центр в (0,0).
        // Коллайдер чуть уже и немного короче спрайта, со сдвигом offset.y=-0.07,
        // чтобы нижний край коллайдера совпал с нижним краем спрайта (ноги).
        // При стоянии на земле игрок будет ровно ступать на верх тайла.
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.55f, 1.3f);
        col.offset = new Vector2(0, -0.07f);

        // 26. Контроллер игрока (поля назначим в ConfigurePlayerComponents)
        go.AddComponent<PlayerController>();

        // 27. AudioSource для звука прыжка
        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.volume = 0.1f;

        // 28. GroundCheck чуть ниже нижнего края коллайдера (нижний край
        // спрайта на localY=-0.72; ставим круг на -0.7, чтобы радиус 0.15
        // перекрывал верх земли при стоянии).
        var gc = new GameObject("GroundCheck");
        gc.transform.parent = go.transform;
        gc.transform.localPosition = new Vector3(0, -0.7f, 0);

        return go;
    }

    static void ConfigurePlayerComponents(GameObject player)
    {
        var pc = player.GetComponent<PlayerController>();
        pc.groundCheck = player.transform.Find("GroundCheck");
        // Маска для ground-check: либо по имени, либо по индексу 6 как фолбэк
        var idx = LayerMask.NameToLayer("Ground");
        pc.groundLayer = idx >= 0 ? (1 << idx) : (1 << 6);
        pc.standSprite = LoadSprite("Assets/_Project/Kenney/Player/alienPink_stand.png");
        pc.walk1Sprite = LoadSprite("Assets/_Project/Kenney/Player/alienPink_walk1.png");
        pc.walk2Sprite = LoadSprite("Assets/_Project/Kenney/Player/alienPink_walk2.png");
        pc.jumpSprite = LoadSprite("Assets/_Project/Kenney/Player/alienPink_jump.png");
        pc.jumpClip = LoadClip("Assets/_Project/Audio/jump.wav");

        EditorUtility.SetDirty(pc);
    }

    // ===== Тайлмап земли =====

    static void CreateTilemap()
    {
        // Слой Ground определён в TagManager.asset на индексе 6
        var groundLayerIdx = LayerMask.NameToLayer("Ground");
        if (groundLayerIdx < 0)
        {
            Debug.LogWarning("Слой 'Ground' не найден, использую индекс 6");
            groundLayerIdx = 6;
        }

        // 29. Сетка и тайлмап
        var gridGO = new GameObject("Grid");
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        var tilemapGO = new GameObject("Tilemap_Ground");
        tilemapGO.transform.SetParent(gridGO.transform, false);
        tilemapGO.layer = groundLayerIdx;

        var tilemap = tilemapGO.AddComponent<Tilemap>();
        var renderer = tilemapGO.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        // 30. Простой TilemapCollider2D без CompositeCollider2D.
        // CompositeCollider2D в batch-mode не успевает сгенерировать
        // геометрию до сохранения сцены (m_CompositePaths остаётся пустым),
        // из-за чего игрок падает сквозь землю. TilemapCollider2D одиночно
        // даёт отдельный коллайдер на каждый тайл, для простого уровня этого
        // достаточно.
        var tilemapCollider = tilemapGO.AddComponent<TilemapCollider2D>();

        // 31. Создаём Tile-ассеты на лету и красим
        var grassMid = CreateTileAsset("grassMid",
            "Assets/_Project/Kenney/Ground/grassMid.png");
        var grassLeft = CreateTileAsset("grassLeft",
            "Assets/_Project/Kenney/Ground/grassLeft.png");
        var grassRight = CreateTileAsset("grassRight",
            "Assets/_Project/Kenney/Ground/grassRight.png");
        var grassCenter = CreateTileAsset("grassCenter",
            "Assets/_Project/Kenney/Ground/grassCenter.png");
        var halfLeft = CreateTileAsset("grassHalf_left",
            "Assets/_Project/Kenney/Ground/grassHalf_left.png");
        var halfMid = CreateTileAsset("grassHalf_mid",
            "Assets/_Project/Kenney/Ground/grassHalf_mid.png");
        var halfRight = CreateTileAsset("grassHalf_right",
            "Assets/_Project/Kenney/Ground/grassHalf_right.png");

        // 32. Покраска земли: первые ряды — grass-Mid сверху, grassCenter снизу
        PaintGroundStrip(tilemap, GroundTiles, grassMid, grassCenter);

        // 33. Платформы — половинные плитки (тонкие)
        PaintPlatforms(tilemap, PlatformTiles, halfLeft, halfMid, halfRight);

        tilemap.RefreshAllTiles();
    }

    static void PaintGroundStrip(Tilemap tm, Vector2Int[] tiles,
        Tile top, Tile under)
    {
        // 34. Верхний ряд — травяная плитка, ниже — заполнение землёй
        var topSet = new HashSet<Vector2Int>(tiles);
        foreach (var p in tiles)
        {
            tm.SetTile(new Vector3Int(p.x, p.y, 0), top);
            // Заполнение от y-1 до y-3, чтобы был видимый «грунт» под травой
            for (int yy = p.y - 1; yy >= p.y - 3; yy--)
            {
                tm.SetTile(new Vector3Int(p.x, yy, 0), under);
            }
        }
    }

    static void PaintPlatforms(Tilemap tm, Vector2Int[] platformTiles,
        Tile left, Tile mid, Tile right)
    {
        // 35. Группируем тайлы платформы по y, чтобы понять края
        var byRow = new Dictionary<int, List<int>>();
        foreach (var p in platformTiles)
        {
            if (!byRow.ContainsKey(p.y)) byRow[p.y] = new List<int>();
            byRow[p.y].Add(p.x);
        }
        foreach (var kv in byRow)
        {
            kv.Value.Sort();
            int y = kv.Key;
            var xs = kv.Value;
            for (int i = 0; i < xs.Count; i++)
            {
                Tile t = mid;
                if (i == 0) t = left;
                else if (i == xs.Count - 1) t = right;
                tm.SetTile(new Vector3Int(xs[i], y, 0), t);
            }
        }
    }

    static Tile CreateTileAsset(string name, string spritePath)
    {
        var path = $"{TilesDir}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = LoadSprite(spritePath);
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    // ===== Монеты =====

    static void SpawnCoins(GameObject vfxPrefab)
    {
        var coinSprite = LoadSprite("Assets/_Project/Kenney/Items/coinGold.png");
        foreach (var pos in LevelLayout.CoinPositions)
        {
            var go = new GameObject($"Coin_{pos.x:F0}_{pos.y:F0}");
            go.transform.position = pos;
            // Монета в натуральную величину (1 unit = размер тайла) — сразу
            // видно и не проваливается в землю.
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var coin = go.AddComponent<Coin>();
            coin.pickupVfxPrefab = vfxPrefab;
        }
    }

    static void SpawnTraps()
    {
        // Спрайт «шипы» 96×48 px при PPU=128 = 0.75×0.375 unit. Pivot center.
        // Сгенерирован в AssetForge перед сборкой сцены.
        var sprite = LoadSprite("Assets/_Project/Art/Generated/spike.png");
        foreach (var pos in LevelLayout.TrapPositions)
        {
            var go = new GameObject($"Trap_{pos.x:F0}_{pos.y:F0}");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;

            // BoxCollider2D под прямоугольную форму шипов; чуть меньше
            // визуала по высоте, щадим игрока на касании краем.
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.7f, 0.32f);
            col.offset = new Vector2(0f, -0.02f);

            go.AddComponent<Trap>();
        }
    }

    // ===== Невидимый потолок (не даёт выпрыгнуть за камеру) =====

    static void CreateCeiling()
    {
        // Камера фиксирована на Y=3 с orthographicSize=6 → верх кадра y=9.
        // Ставим невидимый коллайдер ровно по верхней границе кадра,
        // чтобы игрок при двойном прыжке упирался и не вылетал «в небо».
        var go = new GameObject("Ceiling");
        go.transform.position = new Vector3(50f, 9.5f, 0);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(200f, 1f);
    }

    // ===== KillZone и Финиш =====

    static void CreateKillZone()
    {
        var go = new GameObject("KillZone");
        go.transform.position = new Vector3(50f, -4f, 0);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(200f, 2f);
        col.isTrigger = true;

        go.AddComponent<KillZone>();
    }

    static void CreateFinishZone()
    {
        var go = new GameObject("FinishZone");
        go.transform.position = new Vector3(LevelLayout.FinishPosition.x, LevelLayout.FinishPosition.y, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Assets/_Project/Kenney/Items/finishFlag.png");
        sr.sortingOrder = 4;
        // Флаг 1x1 unit при scale 1.5 → 1.5x1.5; центр на y=1.75 ставит
        // нижний край на y=1 (поверх тайла земли).
        go.transform.localScale = Vector3.one * 1.5f;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 1.0f);
        col.isTrigger = true;

        go.AddComponent<FinishZone>();
    }

    // ===== VFX-префаб =====

    static GameObject CreateCoinPickupVfxPrefab()
    {
        // Префаб пересобирается на каждом билде — правки параметров (цвет,
        // burst, размер) подхватываются без ручного удаления .prefab.
        var prefabPath = $"{PrefabsDir}/CoinPickupVfx.prefab";

        var go = new GameObject("CoinPickupVfx");
        var ps = go.AddComponent<ParticleSystem>();

        // 36. Main module: короткая жизнь, лёгкая гравитация, авто-уничтожение
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 4f;
        main.startSize = 0.25f;
        main.startColor = new Color(1f, 0.85f, 0.2f);
        main.gravityModifier = 1.5f;
        main.maxParticles = 50;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 37. Emission: одна вспышка 20 партиклов
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 20)
        });

        // 38. Shape: круглая зона эмиссии
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        // 39. Material — Sprites/Default + текстура искры
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var sparkle = LoadSprite("Assets/_Project/Kenney/Items/sparkle.png");
        var mat = new Material(Shader.Find("Sprites/Default"));
        if (sparkle != null) mat.mainTexture = sparkle.texture;
        renderer.material = mat;
        renderer.sortingOrder = 20;

        // 40. Сохранение в префаб и удаление инстанса из сцены
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ===== Floating text =====

    static GameObject CreateFloatingTextPrefab()
    {
        // World-space Canvas с UI Text: рисуется поверх спрайтов в мировом
        // масштабе, поддерживает кириллицу через Roboto. Scale 0.01 →
        // 100 px текста = 1 unit, нормальный размер «+1»/«-2» над объектом.
        // Префаб пересобирается каждый билд — правки подхватываются сразу.
        var prefabPath = $"{PrefabsDir}/FloatingText.prefab";

        var go = new GameObject("FloatingText");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 100);
        go.transform.localScale = Vector3.one * 0.01f;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = LoadFont();
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        var outline = textGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);

        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var ft = go.AddComponent<FloatingText>();
        ft.text = text;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ===== UI =====

    // Вспомогательная структура для возврата ссылок из CreateUI
    public struct UiRefs
    {
        public Text scoreText;
        public Text timerText;
        public GameOverPanel gameOverPanel;
    }

    static UiRefs CreateUI(PlayerController player)
    {
        // 41. EventSystem нужен для кликов
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 42. Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasTr = canvasGO.transform;

        // 43. HUD: ScoreText (верх-лево) и TimerText (верх-центр).
        var scoreText = MakeHudText(canvasTr, "ScoreText", "Монеты: 0",
            anchor: new Vector2(0, 1), pivot: new Vector2(0, 1),
            anchoredPos: new Vector2(40, -30));
        var timerText = MakeHudText(canvasTr, "TimerText", "Время: 30.0",
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0, -30));

        // 44. NameInputDialog: затемнение + поле + кнопка OK. Скрыт по умолчанию.
        var nameDialog = CreateNameDialog(canvasTr);

        // 45. GameOverPanel: затемнение + заголовок + счёт + 3 кнопки.
        var gameOverPanel = CreateGameOverPanel(canvasTr, nameDialog, player);

        // 46. Экранные кнопки тач-управления (200×200 в reference 1920×1080):
        // слева пара < >, справа ^. Полупрозрачные, чтобы не закрывать игру.
        // ASCII вместо ←→↑: в Roboto-Regular.ttf (subset под кириллицу) нет
        // глифов стрелочного блока Unicode, поэтому они рисовались пустыми.
        CreateTouchButton(canvasTr, player, TouchButton.Action.Left,
            "TouchLeft",  "<", new Vector2(0, 0), new Vector2(160, 160));
        CreateTouchButton(canvasTr, player, TouchButton.Action.Right,
            "TouchRight", ">", new Vector2(0, 0), new Vector2(380, 160));
        CreateTouchButton(canvasTr, player, TouchButton.Action.Jump,
            "TouchJump",  "^", new Vector2(1, 0), new Vector2(-160, 160));

        return new UiRefs
        {
            scoreText = scoreText,
            timerText = timerText,
            gameOverPanel = gameOverPanel,
        };
    }

    static Text MakeHudText(Transform parent, string name, string content,
        Vector2 anchor, Vector2 pivot, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = LoadFont();
        t.fontSize = 48;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperCenter;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(600, 80);
        return t;
    }

    static GameOverPanel CreateGameOverPanel(Transform canvas,
        NameInputDialog nameDialog, PlayerController player)
    {
        // Корень — затемнение поверх всего, скрыт по умолчанию
        var root = CreateDimmedRoot(canvas, "GameOverPanel");
        var title = CreatePauseText(root.transform, "Title", "Время вышло",
            fontSize: 96, anchoredPos: new Vector2(0, 200),
            size: new Vector2(900, 160));
        var scoreText = CreatePauseText(root.transform, "FinalScore",
            "Монеты: 0", fontSize: 64, anchoredPos: new Vector2(0, 90),
            size: new Vector2(800, 100));

        // Подпись «Новый рекорд: X» / «Твой рекорд: X» — заполняется после
        // ответа сервера. До ответа показываем многоточие.
        var recordText = CreatePauseText(root.transform, "RecordText",
            "...", fontSize: 44, anchoredPos: new Vector2(0, 10),
            size: new Vector2(800, 70));

        // Порядок кнопок как в shooter: Restart → Menu → Leaderboard
        var restart = CreatePauseButton(root.transform, "RestartButton",
            "Заново", new Color(0.2f, 0.6f, 0.9f),
            anchoredPos: new Vector2(0, -90), size: new Vector2(420, 100));
        var menu = CreatePauseButton(root.transform, "MenuButton",
            "В меню", new Color(0.4f, 0.4f, 0.4f),
            anchoredPos: new Vector2(0, -210), size: new Vector2(420, 90));
        var leaderboard = CreatePauseButton(root.transform, "LeaderboardButton",
            "Лидерборд", new Color(0.9f, 0.6f, 0.2f),
            anchoredPos: new Vector2(0, -320), size: new Vector2(420, 80));

        root.SetActive(false);

        // Контроллер на отдельном объекте — он управляет UI и подпиской
        var ctrlGO = new GameObject("GameOverController");
        ctrlGO.transform.SetParent(canvas, false);
        var panel = ctrlGO.AddComponent<GameOverPanel>();
        panel.root = root;
        panel.titleText = title;
        panel.finalScoreText = scoreText;
        panel.recordText = recordText;
        panel.restartButton = restart;
        panel.menuButton = menu;
        panel.leaderboardButton = leaderboard;
        panel.nameDialog = nameDialog;
        panel.player = player;
        EditorUtility.SetDirty(panel);
        return panel;
    }

    // internal — чтобы SceneBuilderMainMenu мог переиспользовать диалог
    // на сцене главного меню без дублирования кода.
    internal static NameInputDialog CreateNameDialog(Transform canvas)
    {
        // Унифицированная форма для двух режимов (первый ввод и смена имени):
        // заголовок «Никнейм», читаемая подсказка про @Telegram, поле ввода,
        // строка ошибки, кнопка OK. Текст подсказки в NameInputDialog.Hint.
        var root = CreateDimmedRoot(canvas, "NameInputPanel");

        CreatePauseText(root.transform, "Title", "Никнейм",
            fontSize: 80, anchoredPos: new Vector2(0, 230),
            size: new Vector2(900, 120));

        // Подсказка с переносом: fontSize 30, центрирование UpperCenter,
        // достаточная высота под 2-3 строки.
        var hint = CreateWrapText(root.transform, "Hint",
            "Подсказка будет проставлена в NameInputDialog.Open()",
            fontSize: 30, anchoredPos: new Vector2(0, 90),
            size: new Vector2(900, 140));

        // InputField (legacy UGUI)
        var fieldGO = new GameObject("NameField");
        fieldGO.transform.SetParent(root.transform, false);
        var bg = fieldGO.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.95f);
        var frt = fieldGO.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0.5f, 0.5f);
        frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(0, -40);
        frt.sizeDelta = new Vector2(620, 80);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(fieldGO.transform, false);
        var fieldText = textGO.AddComponent<Text>();
        fieldText.font = LoadFont();
        fieldText.fontSize = 40;
        fieldText.color = Color.black;
        fieldText.alignment = TextAnchor.MiddleLeft;
        fieldText.supportRichText = false;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20, 5);
        trt.offsetMax = new Vector2(-20, -5);

        var field = fieldGO.AddComponent<InputField>();
        field.textComponent = fieldText;
        field.characterLimit = 24;

        // Строка ошибки/статуса между полем и кнопкой OK
        var error = CreatePauseText(root.transform, "Error", "",
            fontSize: 28, anchoredPos: new Vector2(0, -120),
            size: new Vector2(700, 50));
        error.color = new Color(1f, 0.6f, 0.4f);

        // Две кнопки в одном ряду: «Отмена» слева (нейтральный), OK справа
        // (шалфей — действие). Игрок может закрыть диалог без сохранения —
        // в GameOverPanel при этом счёт не отправляется в лидерборд.
        var cancel = CreatePauseButton(root.transform, "CancelButton",
            "Отмена", new Color(0.7f, 0.7f, 0.7f),
            anchoredPos: new Vector2(-160, -200), size: new Vector2(280, 90));

        var submit = CreatePauseButton(root.transform, "SubmitButton",
            "OK", new Color(0.659f, 0.835f, 0.729f),
            anchoredPos: new Vector2(160, -200), size: new Vector2(280, 90));

        root.SetActive(false);

        var ctrlGO = new GameObject("NameInputController");
        ctrlGO.transform.SetParent(canvas, false);
        var dialog = ctrlGO.AddComponent<NameInputDialog>();
        dialog.root = root;
        dialog.nameField = field;
        dialog.submitButton = submit;
        dialog.cancelButton = cancel;
        dialog.hintText = hint;
        dialog.errorText = error;
        EditorUtility.SetDirty(dialog);
        return dialog;
    }

    static Text CreateWrapText(Transform parent, string name, string content,
        int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = LoadFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return t;
    }

    static GameObject CreateDimmedRoot(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.75f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static void CreateTouchButton(Transform canvas, PlayerController player,
        TouchButton.Action action, string name, string label,
        Vector2 anchor, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);
        go.AddComponent<Button>();

        var tb = go.AddComponent<TouchButton>();
        tb.player = player;
        tb.action = action;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 200);
        rt.anchoredPosition = anchoredPos;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = LoadFont();
        text.fontSize = 120;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        // Обводка — как у счётчика монет, для читаемости на любом фоне
        var outline = textGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        var textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    // ===== Pause panel =====

    static void CreatePausePanel(Transform canvas)
    {
        var panelGO = new GameObject("PausePanel");
        panelGO.transform.SetParent(canvas, false);
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.75f);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panelGO.SetActive(false);

        var title = CreatePauseText(panelGO.transform, "Title", "Пауза",
            fontSize: 96, anchoredPos: new Vector2(0, 120),
            size: new Vector2(800, 160));

        var resume = CreatePauseButton(panelGO.transform, "ResumeButton",
            "Продолжить", new Color(0.2f, 0.6f, 0.9f),
            anchoredPos: new Vector2(0, -40), size: new Vector2(420, 100));

        var menu = CreatePauseButton(panelGO.transform, "MenuButton",
            "В меню", new Color(0.4f, 0.4f, 0.4f),
            anchoredPos: new Vector2(0, -160), size: new Vector2(420, 90));

        // Бургер-кнопка в правом верхнем углу — открыть паузу мышью/тапом
        var openBtn = CreateBurgerButton(canvas);

        // Контроллер вешаем на отдельный GO в Canvas — он управляет panel/кнопками
        var ctrlGO = new GameObject("PauseController");
        ctrlGO.transform.SetParent(canvas, false);
        var ctrl = ctrlGO.AddComponent<PauseController>();
        ctrl.panel = panelGO;
        ctrl.resumeButton = resume;
        ctrl.menuButton = menu;
        ctrl.openButton = openBtn;
        EditorUtility.SetDirty(ctrl);
    }

    static Button CreateBurgerButton(Transform canvas)
    {
        // Квадратная кнопка с тремя полосками — белые Image-прямоугольники,
        // без шрифта (юникод-глифа ☰ нет в Roboto-subset, рисуем сами).
        var go = new GameObject("PauseOpenButton");
        go.transform.SetParent(canvas, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.2f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-30, -30);
        rt.sizeDelta = new Vector2(90, 90);

        for (int i = 0; i < 3; i++)
        {
            var line = new GameObject($"Line{i + 1}");
            line.transform.SetParent(go.transform, false);
            var lineImg = line.AddComponent<Image>();
            lineImg.color = Color.white;
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0, 16 - i * 16);
            lrt.sizeDelta = new Vector2(50, 6);
        }
        return btn;
    }

    static Text CreatePauseText(Transform parent, string name, string content,
        int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = LoadFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var trt = go.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = anchoredPos;
        trt.sizeDelta = size;
        return t;
    }

    static Button CreatePauseButton(Transform parent, string name, string label,
        Color color, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var brt = go.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = anchoredPos;
        brt.sizeDelta = size;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.font = LoadFont();
        txt.fontSize = 44;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }

    // ===== GameManager + HUD =====

    static GameManager CreateGameManager(Text scoreText, Text timerText,
        GameObject floatingTextPrefab)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();

        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.volume = 0.1f;
        gm.sfxSource = audio;
        gm.coinClip = LoadClip("Assets/_Project/Audio/coin.wav");
        gm.victoryClip = LoadClip("Assets/_Project/Audio/victory.wav");
        gm.floatingTextPrefab = floatingTextPrefab;

        // HUD на том же объекте — подписывается на события GameManager
        var hud = go.AddComponent<HUD>();
        hud.scoreText = scoreText;
        hud.timerText = timerText;

        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(hud);
        return gm;
    }

    // ===== Утилиты =====

    static Font LoadFont()
    {
        // Roboto-Regular поддерживает кириллицу — встроенный LegacyRuntime.ttf
        // не имеет славянских глифов, поэтому русский текст не отображается.
        var f = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/_Project/Fonts/Roboto-Regular.ttf");
        if (f == null) Debug.LogWarning("[SceneBuilder] Roboto не найден");
        return f;
    }

    static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[SceneBuilder] Спрайт не найден: {path}");
        return s;
    }

    static AudioClip LoadClip(string path)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Debug.LogWarning($"[SceneBuilder] Звук не найден: {path}");
        return c;
    }

    static void EnsureDirectory(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>();
        bool found = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == scenePath) found = true;
            scenes.Add(s);
        }
        if (!found)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
