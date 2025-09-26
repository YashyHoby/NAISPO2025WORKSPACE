using UnityEngine;

public class SoundObstacle : EventZone
{
    public string soundKey = "metal";

    public override void Apply(HeartAgent a)
    {
        HeartSoundManager.Instance?.PlayMaterialSound(soundKey);

        var rb = a.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // Calculate normal from obstacle center to HeartAgent.
        var n = ((Vector2)a.transform.position - (Vector2)transform.position).normalized;

        // 騾溷ｺｦ繝吶け繝医Ν繧貞渚蟆・＆縺帙∝ｰ代＠貂幄｡ｰ
        rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, n) * 0.9f;
    }
}
