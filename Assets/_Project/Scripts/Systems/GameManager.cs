using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Центральная игровая сессия раунда: счёт, 30-секундный таймер, события.
// Раунд заканчивается по таймауту, падению в яму или достижению финиша.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum EndReason { Timeout, Fell, Finished }

    [Header("Настройки раунда")]
    public float roundDuration = 30f;

    [Header("Аудио")]
    public AudioSource sfxSource;
    public AudioClip coinClip;
    public AudioClip victoryClip;

    public event Action<int> ScoreChanged;
    public event Action<float> TimeChanged;
    public event Action<int, EndReason> GameOver;

    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsPlaying { get; private set; }
    public EndReason LastEndReason { get; private set; }

    void Awake()
    {
        // 1. Singleton (сцена короткоживущая, без DontDestroyOnLoad)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // 2. Инициализация счёта/таймера + первичная отправка событий
        Score = 0;
        TimeLeft = roundDuration;
        IsPlaying = true;
        ScoreChanged?.Invoke(Score);
        TimeChanged?.Invoke(TimeLeft);
        StartCoroutine(TimerLoop());
    }

    public void AddCoin()
    {
        if (!IsPlaying) return;
        Score++;
        if (coinClip != null && sfxSource != null)
            sfxSource.PlayOneShot(coinClip);
        ScoreChanged?.Invoke(Score);
    }

    public void AddPenalty(int amount)
    {
        // Штраф за касание ловушки. Клампим к нулю — отрицательный счёт
        // выглядит как баг и портит лидерборд.
        if (!IsPlaying) return;
        Score = Mathf.Max(0, Score - amount);
        ScoreChanged?.Invoke(Score);
    }

    public void EndRound(EndReason reason)
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        LastEndReason = reason;
        // Согласованность с shooter: «раунд окончен → время 0», вне зависимости
        // от причины. UI не показывает «осталось 8.2 сек» после падения.
        TimeChanged?.Invoke(0f);
        // 3. Звук победы — только при достижении финиша (старая семантика)
        if (reason == EndReason.Finished
            && victoryClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(victoryClip);
        }
        GameOver?.Invoke(Score, reason);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator TimerLoop()
    {
        // 4. Каждый кадр уменьшаем таймер, шлём событие
        while (TimeLeft > 0f && IsPlaying)
        {
            yield return null;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft < 0f) TimeLeft = 0f;
            TimeChanged?.Invoke(TimeLeft);
        }
        if (IsPlaying) EndRound(EndReason.Timeout);
    }
}
