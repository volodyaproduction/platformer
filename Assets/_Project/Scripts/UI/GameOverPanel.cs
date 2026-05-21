using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Панель конца раунда: появляется по событию GameOver, показывает причину
// окончания и итоговый счёт. Если счёт проходит в топ-5 — открывает диалог
// ввода имени. Кнопки: Заново / В меню / Лидерборд.
public class GameOverPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public Text titleText;
    public Text finalScoreText;
    public Button restartButton;
    public Button menuButton;
    public Button leaderboardButton;

    [Header("Лидерборд")]
    public NameInputDialog nameDialog;

    [Header("Игрок (для заморозки)")]
    public PlayerController player;

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver += OnGameOver;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(ToLeaderboard);
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver -= OnGameOver;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (menuButton != null) menuButton.onClick.RemoveListener(ToMenu);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(ToLeaderboard);
    }

    void OnGameOver(int finalScore, GameManager.EndReason reason)
    {
        if (root != null) root.SetActive(true);
        if (titleText != null) titleText.text = TitleFor(reason);
        if (finalScoreText != null)
            finalScoreText.text = $"Монеты: {finalScore}";

        // 1. Замораживаем игрока — иначе при падении он продолжит «лететь»
        if (player != null) player.Freeze();

        // 2. Лидерборд: если результат проходит в топ-5 — спросить имя
        if (LeaderboardSaveSystem.QualifiesForTop(finalScore))
        {
            if (nameDialog != null)
                nameDialog.Open(n => LeaderboardSaveSystem.Submit(n, finalScore));
            else
                LeaderboardSaveSystem.Submit("Игрок", finalScore);
        }
    }

    static string TitleFor(GameManager.EndReason reason) => reason switch
    {
        GameManager.EndReason.Timeout => "Время вышло",
        GameManager.EndReason.Fell => "Вы упали",
        GameManager.EndReason.Finished => "Финиш!",
        _ => "Раунд окончен",
    };

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ToMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
    }

    void ToLeaderboard()
    {
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }
}
