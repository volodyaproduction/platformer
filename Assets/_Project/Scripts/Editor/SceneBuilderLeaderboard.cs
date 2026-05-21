using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Сборка сцены Leaderboard.unity: топ-5 (rank/name/score) + кнопки В меню и
// Очистить. Сцена попадает в BuildSettings последней.
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

        // 4. Контейнер с LeaderboardView
        var viewGO = new GameObject("LeaderboardView", typeof(RectTransform));
        viewGO.transform.SetParent(ct, false);
        var viewRT = viewGO.GetComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = Vector2.zero;
        viewRT.offsetMax = Vector2.zero;
        var view = viewGO.AddComponent<LeaderboardView>();
        view.rankTexts = new Text[LeaderboardSaveSystem.TopSize];
        view.nameTexts = new Text[LeaderboardSaveSystem.TopSize];
        view.scoreTexts = new Text[LeaderboardSaveSystem.TopSize];

        // 5. Шапка таблицы (#, Имя, Очки)
        MkText(ct, "HeaderRank",  font, 36, "#",     Center(), Center(),
            new Vector2(-340, 280), new Vector2(120, 50), TextAnchor.MiddleCenter);
        MkText(ct, "HeaderName",  font, 36, "Имя",   Center(), Center(),
            new Vector2(-50, 280),  new Vector2(420, 50), TextAnchor.MiddleLeft);
        MkText(ct, "HeaderScore", font, 36, "Очки",  Center(), Center(),
            new Vector2(320, 280),  new Vector2(220, 50), TextAnchor.MiddleRight);

        // 6. Строки (rank/name/score)
        float startY = 200f, rowH = 70f;
        for (int i = 0; i < LeaderboardSaveSystem.TopSize; i++)
        {
            float y = startY - i * rowH;
            view.rankTexts[i] = MkText(ct, $"Row{i}_Rank", font, 48, (i + 1) + ".",
                Center(), Center(), new Vector2(-340, y),
                new Vector2(120, 60), TextAnchor.MiddleCenter);
            view.nameTexts[i] = MkText(ct, $"Row{i}_Name", font, 48, "—",
                Center(), Center(), new Vector2(-50, y),
                new Vector2(420, 60), TextAnchor.MiddleLeft);
            view.scoreTexts[i] = MkText(ct, $"Row{i}_Score", font, 48, "—",
                Center(), Center(), new Vector2(320, y),
                new Vector2(220, 60), TextAnchor.MiddleRight);
        }

        // 7. Кнопки внизу: Назад и Очистить
        view.backButton = MkButton(ct, "BackButton", font, "В меню",
            new Color(0.2f, 0.6f, 0.9f),
            new Vector2(-180, -400), new Vector2(320, 100));
        view.clearButton = MkButton(ct, "ClearButton", font, "Очистить",
            new Color(0.55f, 0.35f, 0.35f),
            new Vector2(180, -400), new Vector2(280, 100));

        EditorUtility.SetDirty(view);

        // 8. Сохраняем сцену и добавляем в BuildSettings
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
