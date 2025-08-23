// Scripts/Core/BubbleZone.cs
using UnityEngine;
using System.Collections.Generic;
public class BubbleZone : EventZone
{
    HashSet<int> captured = new();
    public int threshold = 10;
    public override void Apply(HeartAgent a)
    {
        if (!captured.Contains(a.id))
        {
            captured.Add(a.id);
            if (captured.Count >= threshold) { Burst(); }
        }
        var dir = ((Vector2)transform.position - (Vector2)a.transform.position).normalized;
        a.vel += dir * 0.8f * Time.deltaTime;
    }
    void Burst()
    {
        AudioHub.PlayBubbleBurst();
        // ここでパーティクルや波紋演出を発火
        captured.Clear();
    }
}