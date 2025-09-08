// Scripts/Core/HeartDB.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public static class HeartDB
{
    static Dictionary<string, HeartProfile> dict;
    [Serializable] class Wrap { public List<HeartProfile> items = new(); }

    public static bool IsLoaded => dict != null;

    public static void Load()
    {
        if (dict != null) return;

        var ta = Resources.Load<TextAsset>("heart_db");
        if (ta == null)
        {
            Debug.LogWarning("[HeartDB] Resources/heart_db(.json) not found.");
            dict = new Dictionary<string, HeartProfile>(StringComparer.Ordinal);
            return;
        }

        try
        {
            var w = JsonUtility.FromJson<Wrap>(ta.text);
            dict = new Dictionary<string, HeartProfile>(StringComparer.Ordinal);
            if (w?.items != null)
            {
                foreach (var it in w.items)
                {
                    if (string.IsNullOrEmpty(it.uid)) continue;
                    dict[it.uid] = it;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[HeartDB] Parse error: {e.Message}");
            dict = new Dictionary<string, HeartProfile>(StringComparer.Ordinal);
        }
    }

    public static void Reload()
    {
        dict = null;
        Load();
        Debug.Log("[HeartDB] Reloaded");
    }

    public static HeartProfile Get(string uid)
    {
        Load();
        if (string.IsNullOrEmpty(uid)) return Default("DB_DEFAULT");
        return dict.TryGetValue(uid, out var hp) ? hp : Default(uid);
    }

    static HeartProfile Default(string uid) => new HeartProfile
    {
        uid = uid,
        hr = 75f,
        cv = 0.2f,
        range = 20f,
        mean = 80f
    };
}
