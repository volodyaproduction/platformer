using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Пауза по ESC во время уровня. Замораживает игру через Time.timeScale,
// показывает оверлей с кнопками «Продолжить» / «В меню». После победы ESC
// игнорится — там работает WinPanel.
public class PauseController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Button resumeButton;
    public Button menuButton;

    bool paused;

    void OnEnable()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void OnDisable()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (menuButton != null) menuButton.onClick.RemoveListener(ToMenu);
        if (paused) Time.timeScale = 1f;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.IsWon) return;
        Toggle();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Авто-пауза при уходе со вкладки: иначе Unity замораживает Update и
        // игра «висит». По возвращении игрок видит явный экран паузы.
        if (hasFocus || paused) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.IsWon) return;
        Toggle();
    }

    void Toggle()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (panel != null) panel.SetActive(paused);
    }

    void Resume()
    {
        if (paused) Toggle();
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
    }
}
