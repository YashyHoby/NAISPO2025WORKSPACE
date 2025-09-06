using UnityEngine;

/// <summary>
/// 壁近傍で流速 u の法線成分を弱め、接線成分を残す（Slide専用）
/// 付与対象：EdgeCollider2D / PolygonCollider2D / BoxCollider2D など
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class FlowWall2D : MonoBehaviour
{
    [Tooltip("この距離以内で効果を出す")]
    public float effectRadius = 1.0f;

    [Tooltip("法線成分の抑制係数（1=完全に消す）")]
    [Range(0f, 1f)] public float noPenetration = 1.0f;

    [Tooltip("接線成分のブースト係数（0で無効）")]
    [Range(0f, 1f)] public float slideBoost = 0.25f;

    Collider2D col;

    void OnEnable() { col = GetComponent<Collider2D>(); }

    /// <summary>posでの流速uを書き換える（Slide効果）</summary>
    public void ApplyWallEffect(Vector2 pos, ref Vector2 u)
    {
        if (!col || effectRadius <= 0f) return;

        Vector2 nearest = col.ClosestPoint(pos);
        Vector2 d = (Vector2)pos - nearest;
        float dist = d.magnitude;
        if (dist <= 1e-6f || dist > effectRadius) return;

        Vector2 n = d / dist; // 壁から外向き法線（pos方向）
        // 壁へ突っ込むときだけ作用（u が -n 方向へ進もうとしている）
        if (Vector2.Dot(u, -n) <= 0f) return;

        float alpha = Mathf.SmoothStep(0f, 1f, (effectRadius - dist) / effectRadius);

        // 法線成分を減衰
        float un = Vector2.Dot(u, n);
        u -= alpha * un * n * noPenetration;

        // 接線（uから法線を引いた成分）を少し保つ／強める
        if (slideBoost > 0f)
        {
            Vector2 tang = u - Vector2.Dot(u, n) * n;
            u += alpha * slideBoost * tang;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!col) col = GetComponent<Collider2D>();
        if (!col) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        // effectRadiusの可視化：Collider の境界近傍を薄く描く（簡易）
        var b = col.bounds;
        Vector3 min = b.min; Vector3 max = b.max;
        Vector3 p0 = new(min.x, min.y, 0), p1 = new(max.x, min.y, 0),
                p2 = new(max.x, max.y, 0), p3 = new(min.x, max.y, 0);
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
    }
}
