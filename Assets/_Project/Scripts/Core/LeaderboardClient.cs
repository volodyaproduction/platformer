using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

// Сетевой клиент глобального лидерборда. Singleton + DontDestroyOnLoad —
// чтобы переживал смены сцен MainMenu → Main → Leaderboard.
//
// Все запросы идут на относительные пути `/api/...`: Vercel раздаёт и WebGL-
// сборку, и serverless-функции с одного домена, поэтому абсолютный URL не
// нужен (и его нельзя зашивать — он отличается между preview/production).
public class LeaderboardClient : MonoBehaviour
{
    public static LeaderboardClient Instance { get; private set; }

    // ===== DTO для JsonUtility =====
    // [Preserve] критично для WebGL/IL2CPP: эти классы создаются только через
    // JsonUtility.FromJson по рефлексии, и без атрибута linker их выкидывает —
    // получаем RuntimeError "null function" при попытке распарсить ответ.

    [Preserve, Serializable]
    public class Entry
    {
        public string name;
        public int score;
    }

    [Preserve, Serializable]
    public class TopResponse
    {
        public List<Entry> entries;
        public string error;
    }

    [Preserve, Serializable]
    class SaveScoreRequest
    {
        public string player_id;
        public int score;
    }

    [Preserve, Serializable]
    public class SaveScoreResponse
    {
        public int personalBest;
        public bool isNewRecord;
        public string error;
    }

    [Preserve, Serializable]
    class ChangeNameRequest
    {
        public string player_id;
        public string name;
    }

    [Preserve, Serializable]
    public class ChangeNameResponse
    {
        public bool ok;
        public string error;
    }

    // ===== Lifecycle =====

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===== Публичный API =====

    public void GetTop(Action<List<Entry>, string> callback)
    {
        StartCoroutine(GetTopCo(callback));
    }

    public void SaveScore(int score, Action<SaveScoreResponse> callback)
    {
        StartCoroutine(SaveScoreCo(score, callback));
    }

    public void ChangeName(string name, Action<ChangeNameResponse> callback)
    {
        StartCoroutine(ChangeNameCo(name, callback));
    }

    // ===== Корутины =====

    IEnumerator GetTopCo(Action<List<Entry>, string> cb)
    {
        using var req = UnityWebRequest.Get("/api/leaderboard-top");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            cb?.Invoke(null, req.error ?? "network_error");
            yield break;
        }
        TopResponse resp = null;
        try { resp = JsonUtility.FromJson<TopResponse>(req.downloadHandler.text); }
        catch (Exception e) { cb?.Invoke(null, "parse_error: " + e.Message); yield break; }
        if (resp == null) { cb?.Invoke(null, "empty_response"); yield break; }
        cb?.Invoke(resp.entries ?? new List<Entry>(), resp.error);
    }

    IEnumerator SaveScoreCo(int score, Action<SaveScoreResponse> cb)
    {
        var body = new SaveScoreRequest
        {
            player_id = PlayerIdentity.GetOrCreateId(),
            score = score,
        };
        using var req = PostJson("/api/leaderboard-save-score", JsonUtility.ToJson(body));
        yield return req.SendWebRequest();

        var resp = ParseSaveScoreResponse(req);
        cb?.Invoke(resp);
    }

    IEnumerator ChangeNameCo(string name, Action<ChangeNameResponse> cb)
    {
        var body = new ChangeNameRequest
        {
            player_id = PlayerIdentity.GetOrCreateId(),
            name = name,
        };
        using var req = PostJson("/api/leaderboard-change-name", JsonUtility.ToJson(body));
        yield return req.SendWebRequest();

        var resp = ParseChangeNameResponse(req);
        cb?.Invoke(resp);
    }

    // ===== Утилиты =====

    static UnityWebRequest PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    static SaveScoreResponse ParseSaveScoreResponse(UnityWebRequest req)
    {
        // Сервер шлёт JSON и при 200, и при 400/429 (поле error). Парсим
        // то, что есть; если совсем не JSON — кладём сетевую ошибку.
        var text = req.downloadHandler != null ? req.downloadHandler.text : null;
        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                var r = JsonUtility.FromJson<SaveScoreResponse>(text);
                if (r != null) return r;
            }
            catch { /* fallthrough */ }
        }
        return new SaveScoreResponse { error = req.error ?? "network_error" };
    }

    static ChangeNameResponse ParseChangeNameResponse(UnityWebRequest req)
    {
        var text = req.downloadHandler != null ? req.downloadHandler.text : null;
        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                var r = JsonUtility.FromJson<ChangeNameResponse>(text);
                if (r != null) return r;
            }
            catch { /* fallthrough */ }
        }
        return new ChangeNameResponse { ok = false, error = req.error ?? "network_error" };
    }
}
