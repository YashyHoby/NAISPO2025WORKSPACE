// Scripts/Core/SoundObstacle.cs
using UnityEngine;
public class SoundObstacle : EventZone
{
    public string soundKey = "metal";
    public override void Apply(HeartAgent a)
    {
        AudioHub.PlayMaterial(soundKey);
        var n = ((Vector2)a.transform.position - (Vector2)transform.position).normalized;
        a.vel = Vector2.Reflect(a.vel, n) * 0.9f;
    }
}