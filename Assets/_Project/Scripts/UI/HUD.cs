using UnityEngine;
using UnityEngine.UI;

// HUD: текст счёта монет + текст таймера. Подписан на события GameManager.
public class HUD : MonoBehaviour
{
    [Header("UI-элементы")]
    public Text scoreText;
    public Text timerText;

    void OnEnable()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.ScoreChanged += OnScoreChanged;
        gm.TimeChanged += OnTimeChanged;
        OnScoreChanged(gm.Score);
        OnTimeChanged(gm.TimeLeft);
    }

    void OnDisable()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.ScoreChanged -= OnScoreChanged;
        gm.TimeChanged -= OnTimeChanged;
    }

    void OnScoreChanged(int score)
    {
        if (scoreText != null) scoreText.text = $"Монеты: {score}";
    }

    void OnTimeChanged(float time)
    {
        // Один знак после запятой — достаточно, не дёргается
        if (timerText != null) timerText.text = $"Время: {time:F1}";
    }
}
