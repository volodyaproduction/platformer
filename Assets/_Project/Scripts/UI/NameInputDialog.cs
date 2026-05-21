using System;
using UnityEngine;
using UnityEngine.UI;

// Диалог ввода/смены никнейма для лидерборда. Одна и та же форма в двух
// сценариях:
//   OpenForFirstTime — открывается после первого раунда, если PlayerIdentity
//     ещё не имеет имени. После успешной отправки на сервер вызывает callback
//     с выбранным никнеймом; вызывающий код сразу шлёт счёт.
//   OpenForChange — открывается из главного меню для смены уже выбранного
//     никнейма. Поле предзаполнено текущим именем.
//
// Уникальность никнейма проверяется на сервере (HTTP 409 → ошибка «занято»).
// Подсказка одинаковая в обоих режимах: пригласить указать Telegram-ник с `@`,
// чтобы рядом с записью в лидерборде появилась кликабельная ссылка t.me/<nick>.
public class NameInputDialog : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public InputField nameField;
    public Button submitButton;
    public Text hintText;
    public Text errorText;

    Action<string> onSuccess;

    const string Hint =
        "Поставь @ впереди — твой ник станет ссылкой на Telegram, " +
        "и из лидерборда к тебе смогут постучаться.";

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(HandleSubmit);
        if (errorText != null) errorText.text = string.Empty;
    }

    void OnDisable()
    {
        if (submitButton != null) submitButton.onClick.RemoveListener(HandleSubmit);
    }

    public void OpenForFirstTime(Action<string> callback)
    {
        Open(initialName: string.Empty, callback);
    }

    public void OpenForChange(Action<string> callback)
    {
        Open(initialName: PlayerIdentity.GetName(), callback);
    }

    void Open(string initialName, Action<string> callback)
    {
        onSuccess = callback;
        if (root != null) root.SetActive(true);
        if (nameField != null) nameField.text = initialName ?? string.Empty;
        if (hintText != null) hintText.text = Hint;
        if (errorText != null) errorText.text = string.Empty;
        SetInteractable(true);
    }

    void HandleSubmit()
    {
        var raw = nameField != null ? nameField.text : string.Empty;
        var name = (raw ?? string.Empty).Trim();

        // 1. Локальная валидация — серверный ответ для тривиальных проблем
        //    тратить впустую не будем
        if (name.Length < 2)
        {
            ShowError("Никнейм слишком короткий");
            return;
        }
        if (name.Length > 24)
        {
            ShowError("Никнейм слишком длинный (макс 24)");
            return;
        }
        if (name.IndexOfAny(new[] { ' ', '\t', '\n', '\r' }) >= 0)
        {
            ShowError("Без пробелов");
            return;
        }

        // 2. Имя не изменилось — сервер дёргать не нужно, просто закрываем
        if (name == PlayerIdentity.GetName())
        {
            if (root != null) root.SetActive(false);
            var cb = onSuccess;
            onSuccess = null;
            cb?.Invoke(name);
            return;
        }

        // 3. Запрос на сервер. Кнопка блокируется на время запроса.
        SetInteractable(false);
        if (errorText != null) errorText.text = "Проверяем...";

        if (LeaderboardClient.Instance == null)
        {
            ShowError("Нет связи с сервером");
            SetInteractable(true);
            return;
        }

        LeaderboardClient.Instance.ChangeName(name, OnServerResponse);

        void OnServerResponse(LeaderboardClient.ChangeNameResponse resp)
        {
            if (resp != null && resp.ok)
            {
                PlayerIdentity.SetName(name);
                if (root != null) root.SetActive(false);
                var cb = onSuccess;
                onSuccess = null;
                cb?.Invoke(name);
                return;
            }

            // 4. Локализация серверных ошибок
            var msg = resp != null ? resp.error : "network_error";
            ShowError(msg switch
            {
                "name_taken" => "Этот ник уже занят, выбери другой",
                "invalid_name" => "Никнейм некорректный",
                "rate_limited" => "Слишком часто, подожди",
                _ => "Нет связи с сервером",
            });
            SetInteractable(true);
        }
    }

    void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }

    void SetInteractable(bool on)
    {
        if (submitButton != null) submitButton.interactable = on;
        if (nameField != null) nameField.interactable = on;
    }
}
