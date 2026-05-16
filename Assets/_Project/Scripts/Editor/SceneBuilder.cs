using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Events;
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

    // 3. Координаты монеток в мире. Монета — спрайт 128x128 при PPU=128,
    // т.е. 1x1 unit, центр на transform. Чтобы нижний край касался верха
    // тайла (y=1 для тайла земли на y=0), центр должен быть на y=1.5.
    static readonly Vector2[] CoinPositions = new Vector2[]
    {
        new Vector2(2.5f, 1.5f),    // на стартовой земле
        new Vector2(5.5f, 4.5f),    // на платформе y=3
        new Vector2(12.5f, 1.5f),   // между ямами
        new Vector2(16f, 5.5f),     // на платформе y=4
        new Vector2(21.5f, 6.5f),   // на самой высокой платформе y=5
        new Vector2(24.5f, 1.5f),   // перед второй ямой
        new Vector2(29.5f, 1.5f),   // на финишной земле
        new Vector2(32.5f, 1.5f),   // перед флагом
    };

    // 4. Стартовая точка и финиш. Игрок при стоянии на земле должен иметь
    // transform.y ≈ 1.72 (центр спрайта 184px при PPU=128 = 1.44 unit).
    // Стартуем чуть выше, чтобы было видно падение.
    static readonly Vector2 PlayerStart = new Vector2(1f, 4f);
    static readonly Vector2 FinishPosition = new Vector2(34f, 1.75f);

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

        // 10. VFX-префаб для эффекта сбора монеты
        var vfxPrefab = CreateCoinPickupVfxPrefab();

        // 11. Монеты, привязанные к VFX
        SpawnCoins(vfxPrefab);

        // 12. KillZone под уровнем + FinishZone справа + Ceiling сверху
        CreateKillZone();
        CreateFinishZone();
        CreateCeiling();

        // 13. UI: Canvas со счётом и панелью победы + кнопка рестарта
        var (scoreText, winPanel) = CreateUI(out var restartButton);

        // 14. GameManager собирает все ссылки
        var gameManager = CreateGameManager(scoreText, winPanel);
        WireRestartButton(restartButton, gameManager);

        // 15. Связываем поля PlayerController после создания зависимостей
        ConfigurePlayerComponents(player);

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
        var list = new List<Vector2Int>();
        // 17. Стартовый отрезок: x = 0..7, y = 0
        for (int x = 0; x <= 7; x++) list.Add(new Vector2Int(x, 0));
        // 18. Средний отрезок после первой ямы: x = 11..14
        for (int x = 11; x <= 14; x++) list.Add(new Vector2Int(x, 0));
        // 19. Длинный пол перед второй ямой: x = 18..24
        for (int x = 18; x <= 24; x++) list.Add(new Vector2Int(x, 0));
        // 20. Финишный отрезок: x = 28..35
        for (int x = 28; x <= 35; x++) list.Add(new Vector2Int(x, 0));
        return list.ToArray();
    }

    static Vector2Int[] BuildPlatformTiles()
    {
        var list = new List<Vector2Int>();
        // 21. Платформа на средней высоте между стартом и первой ямой
        for (int x = 4; x <= 6; x++) list.Add(new Vector2Int(x, 3));
        // 22. Платформа после первой ямы — повыше
        for (int x = 15; x <= 17; x++) list.Add(new Vector2Int(x, 4));
        // 23. Самая высокая платформа — требует двойного прыжка
        for (int x = 21; x <= 22; x++) list.Add(new Vector2Int(x, 5));
        return list.ToArray();
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
        go.transform.position = new Vector3(PlayerStart.x, PlayerStart.y, 0);
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
        foreach (var pos in CoinPositions)
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

    // ===== Невидимый потолок (не даёт выпрыгнуть за камеру) =====

    static void CreateCeiling()
    {
        // Камера фиксирована на Y=3 с orthographicSize=6 → верх кадра y=9.
        // Ставим невидимый коллайдер ровно по верхней границе кадра,
        // чтобы игрок при двойном прыжке упирался и не вылетал «в небо».
        var go = new GameObject("Ceiling");
        go.transform.position = new Vector3(17.5f, 9.5f, 0);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(60f, 1f);
    }

    // ===== KillZone и Финиш =====

    static void CreateKillZone()
    {
        var go = new GameObject("KillZone");
        go.transform.position = new Vector3(17.5f, -4f, 0);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(60f, 2f);
        col.isTrigger = true;

        go.AddComponent<KillZone>();
    }

    static void CreateFinishZone()
    {
        var go = new GameObject("FinishZone");
        go.transform.position = new Vector3(FinishPosition.x, FinishPosition.y, 0);

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
        var prefabPath = $"{PrefabsDir}/CoinPickupVfx.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

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

    // ===== UI =====

    static (Text scoreText, GameObject winPanel) CreateUI(
        out Button restartButton)
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

        // 43. Score Text (legacy UI Text, Arial по умолчанию)
        var scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(canvasGO.transform, false);
        var scoreText = scoreGO.AddComponent<Text>();
        scoreText.text = "Монеты: 0 / 0";
        scoreText.font = LoadFont();
        scoreText.fontSize = 48;
        scoreText.color = Color.white;
        scoreText.alignment = TextAnchor.UpperLeft;

        var outline = scoreGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 1);
        scoreRT.anchoredPosition = new Vector2(40, -30);
        scoreRT.sizeDelta = new Vector2(600, 80);

        // 44. Win-panel — затемнение + текст + кнопка
        var winPanel = new GameObject("WinPanel");
        winPanel.transform.SetParent(canvasGO.transform, false);
        var panelImg = winPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.75f);
        var winRT = winPanel.GetComponent<RectTransform>();
        winRT.anchorMin = Vector2.zero;
        winRT.anchorMax = Vector2.one;
        winRT.offsetMin = Vector2.zero;
        winRT.offsetMax = Vector2.zero;

        // 45. Текст «Вы победили!»
        var winTextGO = new GameObject("WinText");
        winTextGO.transform.SetParent(winPanel.transform, false);
        var winText = winTextGO.AddComponent<Text>();
        winText.text = "Вы победили!";
        winText.font = LoadFont();
        winText.fontSize = 96;
        winText.color = Color.white;
        winText.alignment = TextAnchor.MiddleCenter;
        var winTextRT = winText.GetComponent<RectTransform>();
        winTextRT.anchorMin = new Vector2(0.5f, 0.5f);
        winTextRT.anchorMax = new Vector2(0.5f, 0.5f);
        winTextRT.pivot = new Vector2(0.5f, 0.5f);
        winTextRT.anchoredPosition = new Vector2(0, 80);
        winTextRT.sizeDelta = new Vector2(800, 160);

        // 46. Кнопка Restart
        var btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(winPanel.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.9f);
        restartButton = btnGO.AddComponent<Button>();
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0, -80);
        btnRT.sizeDelta = new Vector2(320, 100);

        var btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnText = btnTextGO.AddComponent<Text>();
        btnText.text = "Заново";
        btnText.font = LoadFont();
        btnText.fontSize = 44;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        var btnTextRT = btnText.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;

        return (scoreText, winPanel);
    }

    static void WireRestartButton(Button btn, GameManager gm)
    {
        // 47. Привязываем onClick.Restart через SerializedObject — UnityEventTools
        // в Unity 6 стал internal, поэтому редактируем persistentCalls напрямую.
        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.arraySize = 1;
        var call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = gm;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
            typeof(GameManager).AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = "Restart";
        call.FindPropertyRelative("m_Mode").intValue = 1; // Void
        call.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    // ===== GameManager =====

    static GameManager CreateGameManager(Text scoreText, GameObject winPanel)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        gm.scoreText = scoreText;
        gm.winPanel = winPanel;

        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        gm.sfxSource = audio;
        gm.coinClip = LoadClip("Assets/_Project/Audio/coin.wav");
        gm.victoryClip = LoadClip("Assets/_Project/Audio/victory.wav");

        EditorUtility.SetDirty(gm);
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
