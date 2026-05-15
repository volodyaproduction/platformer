using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Менеджер игры: счётчик монет, состояние победы, обновление UI напрямую,
// центральная точка для звуков победы и сбора монет.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 1. UI-ссылки (Score Text + Panel победы)
    [Header("UI")]
    public Text scoreText;
    public GameObject winPanel;

    // 2. Аудио: один источник на менеджере, два клипа
    [Header("Аудио")]
    public AudioSource sfxSource;
    public AudioClip coinClip;
    public AudioClip victoryClip;

    private int coinCount;
    private int totalCoins;
    private bool gameWon;

    void Awake()
    {
        // 3. Singleton-инициализация
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 4. Считаем все монеты в сцене для счётчика «N / total»
        totalCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
        UpdateScoreUI();
        if (winPanel != null) winPanel.SetActive(false);
    }

    public void AddCoin()
    {
        coinCount++;
        if (coinClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(coinClip);
        }
        UpdateScoreUI();
    }

    public void Win()
    {
        // 5. Защита от повторного срабатывания при множественном касании
        if (gameWon) return;
        gameWon = true;

        if (winPanel != null) winPanel.SetActive(true);
        if (victoryClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(victoryClip);
        }
    }

    public void Restart()
    {
        // 6. Перезагрузка текущей сцены
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Монеты: {coinCount} / {totalCoins}";
        }
    }
}
