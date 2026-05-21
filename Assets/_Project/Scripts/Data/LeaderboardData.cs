using System;
using System.Collections.Generic;

// DTO лидерборда: одна запись + контейнер для JsonUtility.
// Сериализуется в PlayerPrefs["platformer_leaderboard_v1"].
[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
    public string date;   // ISO-8601 UTC, опционально
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new();
}
