using System;
using UnityEngine;
using UnityEngine.UI;

// Диалог ввода/смены никнейма для лидерборда. Одна и та же форма в двух
// сценариях:
//   OpenForFirstTime — открывается после раунда, если PlayerIdentity ещё не
//     имеет имени. Принимает два callback'а: onSuccess — имя успешно записано
//     на сервер (вызывающий шлёт счёт), onCancel — игрок закрыл диалог без
//     ввода (счёт не отправляется).
//   OpenForChange — открывается из главного меню для смены уже выбранного
//     никнейма. Поле предзаполнено текущим именем. «Отмена» просто закрывает
//     диалог.
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
    public Button cancelButton;
    public Text hintText;
    public Text errorText;

    Action<string> onSuccess;
    Action onCancel;

    const string Hint =
        "Поставь @ впереди — твой ник станет ссылкой на Telegram, " +
        "и из лидерборда к тебе смогут постучаться.";

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(HandleSubmit);
        if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);
        if (errorText != null) errorText.text = string.Empty;
    }

    void OnDisable()
    {
        if (submitButton != null) submitButton.onClick.RemoveListener(HandleSubmit);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);
    }

    public void OpenForFirstTime(Action<string> onSuccess, Action onCancel = null)
    {
        Open(initialName: string.Empty, onSuccess, onCancel);
    }

    public void OpenForChange(Action<string> onSuccess)
    {
        Open(initialName: PlayerIdentity.GetName(), onSuccess, cancel: null);
    }

    void Open(string initialName, Action<string> success, Action cancel)
    {
        onSuccess = success;
        onCancel = cancel;
        if (root != null)
        {
            root.SetActive(true);
            // Поверх любых других панелей того же Canvas: GameOverPanel
            // создаётся позже в иерархии, иначе закроет диалог собой.
            root.transform.SetAsLastSibling();
        }
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
            CloseAndInvokeSuccess(name);
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
                CloseAndInvokeSuccess(name);
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

    void HandleCancel()
    {
        if (root != null) root.SetActive(false);
        var cb = onCancel;
        onSuccess = null;
        onCancel = null;
        cb?.Invoke();
    }

    void CloseAndInvokeSuccess(string name)
    {
        if (root != null) root.SetActive(false);
        var cb = onSuccess;
        onSuccess = null;
        onCancel = null;
        cb?.Invoke(name);
    }

    void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }

    void SetInteractable(bool on)
    {
        if (submitButton != null) submitButton.interactable = on;
        if (cancelButton != null) cancelButton.interactable = on;
        if (nameField != null) nameField.interactable = on;
    }
}
