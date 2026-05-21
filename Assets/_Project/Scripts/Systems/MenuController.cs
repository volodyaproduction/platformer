using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: одна кнопка «Старт» — грузит игровую сцену.
public class MenuController : MonoBehaviour
{
    public Button startButton;

    void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
    }

    void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
    }

    void StartGame() => SceneManager.LoadScene("Main");
}
