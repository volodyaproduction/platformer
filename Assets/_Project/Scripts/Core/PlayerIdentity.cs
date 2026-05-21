using System;
using UnityEngine;

// Личность игрока: GUID и текущее отображаемое имя. Всё лежит в PlayerPrefs —
// в WebGL это IndexedDB браузера, поэтому идентификатор переживает деплои,
// рестарты браузера и обновления игры, но не чистку данных сайта.
//
// GUID — единственный «логин» в этой игре: при отправке счёта сервер по нему
// узнаёт, чей именно это рекорд, и не даёт переписать чужие записи.
public static class PlayerIdentity
{
    const string IdKey = "platformer_player_id";
    const string NameKey = "platformer_player_name";

    public static string GetOrCreateId()
    {
        var id = PlayerPrefs.GetString(IdKey, string.Empty);
        if (!string.IsNullOrEmpty(id)) return id;

        // 1. Без дефисов — компактнее в URL и заголовках
        id = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(IdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    public static string GetName() =>
        PlayerPrefs.GetString(NameKey, string.Empty);

    public static void SetName(string name)
    {
        PlayerPrefs.SetString(NameKey, name ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static bool HasName() => !string.IsNullOrEmpty(GetName());
}
