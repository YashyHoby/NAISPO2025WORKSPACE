using UnityEngine;

public class SoundObstacle : EventZone
{
    public string soundKey = "metal";

    public override void Apply(HeartAgent a)
    {
        AudioHub.PlayMaterial(soundKey);

        var rb = a.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // 法線方向を計算（障害物中心 → HeartAgent 方向）
        var n = ((Vector2)a.transform.position - (Vector2)transform.position).normalized;

        // 速度ベクトルを反射させ、少し減衰
        rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, n) * 0.9f;
    }
}
