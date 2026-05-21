using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: Старт / Никнейм / Лидерборд.
// Кнопка никнейма показывает текущее имя и открывает диалог смены
// (NameInputDialog лежит на этой же сцене и шлёт rename на сервер).
public class MenuController : MonoBehaviour
{
    [Header("Кнопки меню")]
    public Button startButton;
    public Button nameButton;
    public Text nameButtonLabel;
    public Button leaderboardButton;

    [Header("Диалог имени")]
    public NameInputDialog nameDialog;

    void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (nameButton != null) nameButton.onClick.AddListener(OpenNameDialog);
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OpenLeaderboard);

        RefreshNameLabel();
    }

    void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (nameButton != null) nameButton.onClick.RemoveListener(OpenNameDialog);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
    }

    void RefreshNameLabel()
    {
        if (nameButtonLabel == null) return;
        var n = PlayerIdentity.GetName();
        nameButtonLabel.text = string.IsNullOrEmpty(n)
            ? "Указать никнейм"
            : $"Мой никнейм {n}";
    }

    void OpenNameDialog()
    {
        if (nameDialog == null) return;
        // В меню — режим смены: диалог проверит уникальность на сервере и
        // закроется только при успешном rename
        nameDialog.OpenForChange(_ => RefreshNameLabel());
    }

    void StartGame() => SceneManager.LoadScene("Main");

    void OpenLeaderboard()
    {
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }
}
