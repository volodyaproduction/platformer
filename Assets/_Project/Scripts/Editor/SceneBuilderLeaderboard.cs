using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Сборка сцены Leaderboard.unity: скролл-список глобального лидерборда.
// Строки рендерит LeaderboardView в рантайме из ответа сервера — здесь
// собираем только каркас: камера, заголовок, шапку, ScrollRect (Viewport +
// Content с VerticalLayoutGroup + ContentSizeFitter), статус-текст и кнопку
// «В меню».
public static class SceneBuilderLeaderboard
{
    public const string ScenePath = "Assets/_Project/Scenes/Leaderboard.unity";

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Камера — голубое небо в тон игре
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.5f, 0.7f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        // 2. EventSystem + Canvas
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = LoadFont();
        var ct = canvasGO.transform;

        // 3. Заголовок
        MkText(ct, "Title", font, 110, "Лидерборд",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -50), new Vector2(900, 160),
            TextAnchor.MiddleCenter);

        // 4. ScrollRect-рамка. Внешний контейнер с полупрозрачным тёмным
        //    фоном — даёт ту же визуальную «коробку» что и в шутере на
        //    голубом фоне платформера.
        var scrollGO = new GameObject("Scroll", typeof(RectTransform));
        scrollGO.transform.SetParent(ct, false);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Center();
        scrollRT.anchorMax = Center();
        scrollRT.pivot = Center();
        scrollRT.anchoredPosition = new Vector2(0, 30);
        scrollRT.sizeDelta = new Vector2(1200, 700);
        scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        // RectMask2D (а не Mask + Image): у Mask требуется Image на том же
        // объекте, что превращает Viewport в Graphic и конфликтует с другим
        // Image-фоном (в шутере это уже наступали).
        viewportGO.AddComponent<RectMask2D>();
        scroll.viewport = viewportRT;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = new Vector2(0, 0);
        contentRT.offsetMax = new Vector2(0, 0);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        scroll.content = contentRT;

        // 6. Статус-текст («Загрузка...», «нет связи», «пусто»). Лежит
        //    поверх viewport, но скрывается когда строки приходят.
        var status = MkText(ct, "StatusText", font, 40,
            "Загрузка...", Center(), Center(),
            new Vector2(0, -20), new Vector2(1200, 80),
            TextAnchor.MiddleCenter);

        // 7. Кнопка «В меню» внизу
        var back = MkButton(ct, "BackButton", font, "В меню",
            new Color(0.2f, 0.6f, 0.9f),
            new Vector2(0, -440), new Vector2(360, 100));

        // 8. Контроллер вьюхи
        var viewGO = new GameObject("LeaderboardView", typeof(RectTransform));
        viewGO.transform.SetParent(ct, false);
        var viewRT = viewGO.GetComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = Vector2.zero;
        viewRT.offsetMax = Vector2.zero;
        var view = viewGO.AddComponent<LeaderboardView>();
        view.content = contentRT;
        view.statusText = status;
        view.backButton = back;
        view.font = font;

        EditorUtility.SetDirty(view);

        // 9. LeaderboardClient (на случай прямого захода на эту сцену —
        //    в обычном флоу клиент уже жив, ему дубликат не страшен).
        var lbGO = new GameObject("LeaderboardClient");
        lbGO.AddComponent<LeaderboardClient>();

        // 10. Сохраняем сцену и добавляем в BuildSettings
        EnsureDir(System.IO.Path.GetDirectoryName(ScenePath));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScene(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilderLeaderboard] Сцена сохранена: " + ScenePath);
    }

    // ===== Хелперы UI (локальные — не плодим зависимостей) =====

    static Vector2 Center() => new Vector2(0.5f, 0.5f);

    static Text MkText(Transform parent, string name, Font font, int fontSize,
        string text, Vector2 anchorMin, Vector2 pivot, Vector2 anchoredPos,
        Vector2 size, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = alignment;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMin;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return t;
    }

    static Button MkButton(Transform parent, string name, Font font,
        string label, Color color, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Center();
        rt.anchorMax = Center();
        rt.pivot = Center();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var textGO = new GameObject("Label");
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
        if (f == null) Debug.LogWarning("[SceneBuilderLeaderboard] Roboto не найден");
        return f;
    }

    static void EnsureDir(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    static void AddScene(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == scenePath);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
