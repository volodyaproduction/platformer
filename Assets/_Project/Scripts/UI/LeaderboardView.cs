using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Отображение топ-5 на отдельной сцене Leaderboard. Если записей меньше —
// рисуем прочерки. Кнопка Назад возвращает в главное меню.
public class LeaderboardView : MonoBehaviour
{
    [Header("Строки (5 шт.)")]
    public Text[] rankTexts;
    public Text[] nameTexts;
    public Text[] scoreTexts;

    [Header("Навигация")]
    public Button backButton;
    public Button clearButton;

    void OnEnable()
    {
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (clearButton != null) clearButton.onClick.AddListener(ClearAndRefresh);
        Refresh();
    }

    void OnDisable()
    {
        if (backButton != null) backButton.onClick.RemoveListener(Back);
        if (clearButton != null) clearButton.onClick.RemoveListener(ClearAndRefresh);
    }

    void Refresh()
    {
        var data = LeaderboardSaveSystem.Load();
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));

        for (int i = 0; i < rankTexts.Length; i++)
        {
            var has = i < data.entries.Count;
            if (rankTexts[i] != null) rankTexts[i].text = (i + 1) + ".";
            if (nameTexts[i] != null)
                nameTexts[i].text = has ? data.entries[i].name : "—";
            if (scoreTexts[i] != null)
                scoreTexts[i].text = has ? data.entries[i].score.ToString() : "—";
        }
    }

    void Back() => SceneManager.LoadScene("MainMenu");

    void ClearAndRefresh()
    {
        LeaderboardSaveSystem.Clear();
        Refresh();
    }
}
