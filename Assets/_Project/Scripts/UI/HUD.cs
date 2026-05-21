using UnityEngine;
using UnityEngine.UI;

// HUD: текст счёта монет + текст таймера. Подписан на события GameSession.
public class HUD : MonoBehaviour
{
    [Header("UI-элементы")]
    public Text scoreText;
    public Text timerText;

    void OnEnable()
    {
        var session = GameSession.Instance;
        if (session == null) return;
        session.ScoreChanged += OnScoreChanged;
        session.TimeChanged += OnTimeChanged;
        OnScoreChanged(session.Score);
        OnTimeChanged(session.TimeLeft);
    }

    void OnDisable()
    {
        var session = GameSession.Instance;
        if (session == null) return;
        session.ScoreChanged -= OnScoreChanged;
        session.TimeChanged -= OnTimeChanged;
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
