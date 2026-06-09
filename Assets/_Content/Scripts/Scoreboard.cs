using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Per-level top-10 best run times, persisted across sessions in PlayerPrefs and
/// keyed by scene name. Times are run durations in seconds, sorted ascending
/// (lower is better). Shown on the finish screen by <see cref="RunManager"/>.
/// </summary>
public static class Scoreboard
{
    public const int MaxEntries = 10;
    private const string KeyPrefix = "scoreboard_";

    /// <summary>The stored times for a level, best first (never more than ten).</summary>
    public static List<float> GetTimes(string level)
    {
        return Load(level);
    }

    /// <summary>
    /// Records a finishing time and trims the board to the top ten. Returns the
    /// 1-based rank the time earned, or -1 if it didn't make the board.
    /// </summary>
    public static int Submit(string level, float time)
    {
        List<float> times = Load(level);

        // Insert keeping the list sorted; the new time ranks behind any equal or
        // faster existing time (ties keep the incumbent ahead).
        int rank = times.Count;
        for (int i = 0; i < times.Count; i++)
        {
            if (time < times[i])
            {
                rank = i;
                break;
            }
        }

        times.Insert(rank, time);

        if (times.Count > MaxEntries)
            times.RemoveRange(MaxEntries, times.Count - MaxEntries);

        Save(level, times);

        return rank < MaxEntries ? rank + 1 : -1;
    }

    private static string KeyFor(string level) => KeyPrefix + level;

    private static List<float> Load(string level)
    {
        List<float> list = new List<float>();
        string raw = PlayerPrefs.GetString(KeyFor(level), "");

        if (string.IsNullOrEmpty(raw))
            return list;

        foreach (string token in raw.Split(';'))
        {
            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                list.Add(value);
        }

        list.Sort();
        return list;
    }

    private static void Save(string level, List<float> times)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < times.Count; i++)
        {
            if (i > 0)
                sb.Append(';');
            sb.Append(times[i].ToString("R", CultureInfo.InvariantCulture));
        }

        PlayerPrefs.SetString(KeyFor(level), sb.ToString());
        PlayerPrefs.Save();
    }
}
