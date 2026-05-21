using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: «Старт» грузит игровую сцену, «Лидерборд» — таблицу топ-5.
public class MenuController : MonoBehaviour
{
    public Button startButton;
    public Button leaderboardButton;

    void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OpenLeaderboard);
    }

    void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
    }

    void StartGame() => SceneManager.LoadScene("Main");

    void OpenLeaderboard()
    {
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }
}
