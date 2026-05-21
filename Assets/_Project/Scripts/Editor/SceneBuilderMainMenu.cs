using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Программно собирает MainMenu.unity: заголовок «2D-платформер» и
// кнопка «Старт», грузящая Main.unity. Цвет фона — голубое небо как в игре.
public static class SceneBuilderMainMenu
{
    public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    public static void Build()
    {
        // 1. Пустая сцена
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Камера — голубой фон в тон игре (см. SceneBuilder.CreateMainCamera)
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.5f, 0.7f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        // 3. EventSystem — нужен для кликов
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // 4. Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = LoadFont();

        // 5. Корень меню с компонентом MenuController
        var menuRoot = new GameObject("Menu", typeof(RectTransform));
        menuRoot.transform.SetParent(canvasGO.transform, false);
        var menuRT = menuRoot.GetComponent<RectTransform>();
        menuRT.anchorMin = Vector2.zero;
        menuRT.anchorMax = Vector2.one;
        menuRT.offsetMin = Vector2.zero;
        menuRT.offsetMax = Vector2.zero;
        var menu = menuRoot.AddComponent<MenuController>();

        // 6. Заголовок
        CreateText(canvasGO.transform, "Title", font,
            fontSize: 110, text: "2D-платформер",
            anchoredPos: new Vector2(0, -160),
            size: new Vector2(1400, 200),
            anchor: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f));

        // 7. Кнопка «Старт» — синяя, в тон restart-кнопке в игре
        menu.startButton = CreateButton(menuRoot.transform, "StartButton", font,
            label: "Старт",
            color: new Color(0.2f, 0.6f, 0.9f),
            anchoredPos: new Vector2(0, 60),
            size: new Vector2(440, 110));

        // 7a. Кнопка «Лидерборд» — оранжевая, ниже «Старт»
        menu.leaderboardButton = CreateButton(menuRoot.transform,
            "LeaderboardButton", font,
            label: "Лидерборд",
            color: new Color(0.9f, 0.6f, 0.2f),
            anchoredPos: new Vector2(0, -70),
            size: new Vector2(440, 100));

        EditorUtility.SetDirty(menu);

        // 8. Сохраняем сцену и регистрируем как первую в BuildSettings
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneAsFirst(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilderMainMenu] Сцена сохранена: " + ScenePath);
    }

    // ===== UI-фабрики =====

    static Text CreateText(Transform parent, string name, Font font,
        int fontSize, string text, Vector2 anchoredPos, Vector2 size,
        Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;

        // Тёмная обводка — заголовок на голубом фоне должен читаться
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return t;
    }

    static Button CreateButton(Transform parent, string name, Font font,
        string label, Color color, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = 44;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    static Font LoadFont()
    {
        var f = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/_Project/Fonts/Roboto-Regular.ttf");
        if (f == null) Debug.LogWarning("[SceneBuilderMainMenu] Roboto не найден");
        return f;
    }

    static void EnsureDir(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddSceneAsFirst(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == scenePath);
        list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
