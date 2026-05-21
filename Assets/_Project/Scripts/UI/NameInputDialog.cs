using System;
using UnityEngine;
using UnityEngine.UI;

// Диалог ввода имени для лидерборда. Открывается из GameOverPanel при
// попадании в топ-5. Имя сохраняется в PlayerPrefs[last_player_name] для
// подсказки при следующем запуске.
public class NameInputDialog : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public InputField nameField;
    public Button submitButton;

    const string PrefKey = "last_player_name";
    const int MaxLen = 12;

    Action<string> onSubmit;

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(HandleSubmit);
    }

    void OnDisable()
    {
        if (submitButton != null) submitButton.onClick.RemoveListener(HandleSubmit);
    }

    public void Open(Action<string> callback)
    {
        if (root != null) root.SetActive(true);
        if (nameField != null)
            nameField.text = PlayerPrefs.GetString(PrefKey, "Игрок");
        onSubmit = callback;
    }

    void HandleSubmit()
    {
        var raw = nameField != null ? nameField.text : "Игрок";
        var name = string.IsNullOrWhiteSpace(raw) ? "Игрок" : raw.Trim();
        if (name.Length > MaxLen) name = name.Substring(0, MaxLen);

        PlayerPrefs.SetString(PrefKey, name);
        PlayerPrefs.Save();

        if (root != null) root.SetActive(false);
        onSubmit?.Invoke(name);
        onSubmit = null;
    }
}
