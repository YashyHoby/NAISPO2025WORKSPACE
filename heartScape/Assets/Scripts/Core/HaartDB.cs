// Scripts/Core/HeartDB.cs
using UnityEngine;
using System.Collections.Generic;

public static class HeartDB
{
    static Dictionary<string, HeartProfile> dict;
    [System.Serializable] class Wrap { public List<HeartProfile> items; }

    public static void Load()
    {
        if (dict != null) return;
        var txt = Resources.Load<TextAsset>("heart_db").text;
        var w = JsonUtility.FromJson<Wrap>(txt);
        dict = new Dictionary<string, HeartProfile>();
        foreach (var it in w.items) dict[it.uid] = it;
    }
    public static HeartProfile Get(string uid)
    {
        Load();
        return dict.TryGetValue(uid, out var hp) ? hp : new HeartProfile
        {
            uid = uid,
            hr = 75,
            cv = 0.2f,
            range = 20,
            mean = 80
        };
    }
}
