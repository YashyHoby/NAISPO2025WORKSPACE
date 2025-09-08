using UnityEngine;
using System.Collections.Generic;

public class BubbleZone : EventZone
{
    [Tooltip("捕獲数がこの値に達すると破裂演出")]
    public int threshold = 10;

    [Tooltip("中心へ引き寄せる強さ（Force）")]
    public float pullStrength = 3.5f;

    [Tooltip("内部ダンピング（追加減速の強さ）")]
    public float damping = 6.0f;

    // 吸い込み時に少し減速させる係数（旧: a.vel *= 0.2f）
    [Range(0f, 1f)] public float initialSlowdown = 0.2f;

    private readonly HashSet<int> captured = new();

    // EventZone 側が FixedUpdate 相当で Apply を呼んでいる想定
    public override void Apply(HeartAgent a)
    {
        if (!a) return;
        var rb = a.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // --- 初回侵入で捕獲状態へ
        if (!captured.Contains(a.id))
        {
            captured.Add(a.id);
            a.isCaptured = true;
            a.capturedBy = this;

            // 旧 a.vel *= 0.2f -> 物理速度を直接スケール
            rb.linearVelocity *= initialSlowdown;

            if (captured.Count >= threshold)
                Burst();
        }

        // --- 内部に留める：中心へ吸引 + 追加ダンピング
        Vector2 center = transform.position;
        Vector2 toC = center - rb.position;

        // 吸引（加算力）
        if (toC.sqrMagnitude > 1e-6f)
        {
            Vector2 pull = toC.normalized * pullStrength;
            rb.AddForce(pull, ForceMode2D.Force);
        }

        // 追加ダンピング（速度を力で減衰させる： -c*v ）
        if (damping > 0f)
        {
            Vector2 dampingForce = -rb.linearVelocity * damping;
            rb.AddForce(dampingForce, ForceMode2D.Force);
        }

        // 中心付近は位置を少しだけ中心へ寄せる（物理に優しい MovePosition）
        if (toC.magnitude < radius * 0.3f)
        {
            Vector2 p = Vector2.Lerp(rb.position, center, 0.15f);
            rb.MovePosition(p);
            // ほぼ静止に寄せる
            rb.linearVelocity *= 0.8f;
        }
    }

    void Burst()
    {
        AudioHub.PlayBubbleBurst();

        // 捕獲解放（軽く弾き出す：Impulse）
        var all = FindObjectsByType<HeartAgent>(FindObjectsSortMode.None);
        foreach (var agent in all)
        {
            if (!agent) continue;
            if (agent.capturedBy != this) continue;

            agent.isCaptured = false;
            agent.capturedBy = null;

            var rb = agent.GetComponent<Rigidbody2D>();
            if (rb)
            {
                Vector2 dir = (rb.position - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 1e-6f) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * 2.0f, ForceMode2D.Impulse);
            }
        }
        captured.Clear();
    }

    public void Release(HeartAgent a)
    {
        if (!a) return;
        if (captured.Remove(a.id))
        {
            a.isCaptured = false;
            a.capturedBy = null;
        }
    }
}
