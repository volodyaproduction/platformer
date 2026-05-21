using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Панель конца раунда. Логика отправки счёта:
//
//   1. Раунд закончен → подписываемся на GameManager.GameOver.
//   2. Если у игрока нет никнейма — открываем NameInputDialog, ждём успешного
//      ответа от сервера (имя записано в platformer:names), после чего шлём
//      счёт.
//   3. Если никнейм есть — сразу шлём счёт.
//   4. Сервер возвращает {personalBest, isNewRecord}. Показываем «Монеты: X»
//      + либо «Новый рекорд!», либо «Твой рекорд: Y».
public class GameOverPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public Text titleText;
    public Text finalScoreText;
    public Text recordText;
    public Button restartButton;
    public Button menuButton;
    public Button leaderboardButton;

    [Header("Диалог имени")]
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
        if (recordText != null) recordText.text = "...";

        // 1. Замораживаем игрока — иначе при падении он продолжит «лететь»
        if (player != null) player.Freeze();

        // 2. Первая игра — сначала имя, потом сабмит. Иначе сразу сабмит.
        //    Если игрок закрыл диалог «Отмена» — счёт не отправляется.
        if (!PlayerIdentity.HasName())
        {
            if (nameDialog != null)
                nameDialog.OpenForFirstTime(
                    onSuccess: _ => SubmitScore(finalScore),
                    onCancel: () =>
                    {
                        if (recordText != null)
                            recordText.text = "Счёт не сохранён";
                    });
            else
                SubmitScore(finalScore);    // диалога нет — шлём без имени
        }
        else
        {
            SubmitScore(finalScore);
        }
    }

    void SubmitScore(int score)
    {
        if (LeaderboardClient.Instance == null)
        {
            if (recordText != null) recordText.text = "Сервер недоступен";
            return;
        }
        LeaderboardClient.Instance.SaveScore(score, resp =>
        {
            if (recordText == null) return;
            if (resp == null || !string.IsNullOrEmpty(resp.error))
            {
                recordText.text = resp != null && resp.error == "rate_limited"
                    ? "Сабмит уже учтён"
                    : "Не удалось отправить счёт";
                return;
            }
            recordText.text = resp.isNewRecord
                ? $"Новый рекорд: {resp.personalBest}!"
                : $"Твой рекорд: {resp.personalBest}";
        });
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
