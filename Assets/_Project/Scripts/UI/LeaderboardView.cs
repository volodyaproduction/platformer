using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Просмотр глобального лидерборда: тянет данные с сервера, рендерит строки в
// ScrollRect, подсвечивает топ-3 цветными «медалями». Если никнейм игрока
// начинается с `@` — строка кликабельна и открывает t.me/<nick>.
public class LeaderboardView : MonoBehaviour
{
    [Header("Скролл-контейнер")]
    public RectTransform content;       // содержимое ScrollRect, у него VLG
    public Text statusText;             // «Загрузка...», «нет связи»
    public Button backButton;

    [Header("Шрифт")]
    public Font font;

    [Header("Цвета медалей (1-3 место)")]
    public Color goldColor = new Color(1.0f, 0.84f, 0.0f);
    public Color silverColor = new Color(0.85f, 0.85f, 0.9f);
    public Color bronzeColor = new Color(0.80f, 0.50f, 0.20f);

    void OnEnable()
    {
        if (backButton != null) backButton.onClick.AddListener(Back);
        Refresh();
    }

    void OnDisable()
    {
        if (backButton != null) backButton.onClick.RemoveListener(Back);
    }

    void Refresh()
    {
        ClearRows();
        if (statusText != null) statusText.text = "Загрузка...";

        if (LeaderboardClient.Instance == null)
        {
            if (statusText != null) statusText.text = "Сервер недоступен";
            return;
        }

        LeaderboardClient.Instance.GetTop((entries, error) =>
        {
            if (error != null)
            {
                if (statusText != null) statusText.text = "Нет связи с сервером";
                return;
            }
            if (entries == null || entries.Count == 0)
            {
                if (statusText != null) statusText.text =
                    "Пока пусто. Сыграй первым!";
                return;
            }
            if (statusText != null) statusText.text = string.Empty;
            RenderEntries(entries);
        });
    }

    void ClearRows()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void RenderEntries(List<LeaderboardClient.Entry> entries)
    {
        // 1. Подсвечиваем свою строку — сравнение case-insensitive, потому
        //    что уникальность имён на сервере проверяется по lowercase.
        var myName = PlayerIdentity.GetName();

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            CreateRow(rank: i + 1, name: e.name, score: e.score,
                isMe: !string.IsNullOrEmpty(myName)
                    && string.Equals(myName, e.name,
                        System.StringComparison.OrdinalIgnoreCase));
        }
    }

    void CreateRow(int rank, string name, int score, bool isMe)
    {
        // 2. Каждая строка — горизонтальный layout: [rank] [name] [score].
        //    Высота фиксирована через LayoutElement, ширина растягивается.
        var row = new GameObject($"Row_{rank}", typeof(RectTransform));
        row.transform.SetParent(content, false);

        var bg = row.AddComponent<Image>();
        bg.color = isMe
            ? new Color(0.30f, 0.45f, 0.30f, 0.5f)
            : new Color(1f, 1f, 1f, 0.05f);

        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 80f;
        layout.preferredHeight = 80f;

        var hg = row.AddComponent<HorizontalLayoutGroup>();
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.padding = new RectOffset(20, 20, 6, 6);
        hg.spacing = 16;
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight = true;

        // 3. Медаль или номер
        var rankColor = rank switch
        {
            1 => goldColor,
            2 => silverColor,
            3 => bronzeColor,
            _ => (Color?)null,
        };
        AddMedal(row.transform, rank, rankColor);

        // 4. Имя — кликабельное, если начинается с @
        AddNameCell(row.transform, name);

        // 5. Очки — справа, отдельная ячейка
        AddScoreCell(row.transform, score);
    }

    void AddMedal(Transform parent, int rank, Color? color)
    {
        // Image и Text оба наследуют Graphic, а Unity допускает только один
        // Graphic на GameObject — поэтому фон-кружок и текст лежат на разных
        // GO: внешний — для layout и фона, внутренний дочерний — для цифры.
        var go = new GameObject($"Rank_{rank}", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 70;
        le.preferredWidth = 70;

        if (color.HasValue)
        {
            var img = go.AddComponent<Image>();
            img.color = color.Value;
        }

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var t = labelGO.AddComponent<Text>();
        t.text = rank.ToString();
        t.font = font;
        t.fontSize = 38;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color.HasValue ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void AddNameCell(Transform parent, string name)
    {
        var go = new GameObject("Name");
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredWidth = 600;

        var t = go.AddComponent<Text>();
        t.text = name ?? string.Empty;
        t.font = font;
        t.fontSize = 40;
        t.alignment = TextAnchor.MiddleLeft;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;

        // Имя с @ → подкрашиваем как ссылку и делаем кликабельной.
        // Text сам по себе raycast target, поэтому IPointerClickHandler
        // получит событие через GraphicRaycaster без дополнительной графики.
        if (!string.IsNullOrEmpty(name) && name.StartsWith("@") && name.Length > 1)
        {
            t.color = new Color(0.5f, 0.8f, 1f);
            t.raycastTarget = true;
            var btn = go.AddComponent<TelegramLinkClick>();
            btn.telegramName = name.Substring(1);
        }
    }

    void AddScoreCell(Transform parent, int score)
    {
        var go = new GameObject("Score");
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 200;
        le.preferredWidth = 200;

        var t = go.AddComponent<Text>();
        t.text = score.ToString();
        t.font = font;
        t.fontSize = 40;
        t.alignment = TextAnchor.MiddleRight;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void Back() => SceneManager.LoadScene("MainMenu");
}

// Клик по нику с @ → открыть t.me/<nick>. Сам по себе Text является
// Graphic с raycastTarget=true, поэтому отдельная raycast-цель не нужна.
public class TelegramLinkClick : MonoBehaviour, IPointerClickHandler
{
    public string telegramName;

    public void OnPointerClick(PointerEventData _)
    {
        if (string.IsNullOrEmpty(telegramName)) return;
        Application.OpenURL("https://t.me/" + telegramName);
    }
}
