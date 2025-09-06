using System.Collections.Generic;
using UnityEngine;

public class FlowSource2D : MonoBehaviour
{
    public enum Mode { Radial, Directional }
    public Mode mode = Mode.Radial;

    [Header("Strength / Range")]
    [Tooltip("力の大きさ（近いほど強く、遠いほど弱く）")]
    public float strength = 5f;
    [Tooltip("この半径を超えると影響しない")]
    public float radius = 8f;
    [Tooltip("源に極端に近い時の暴れ抑制")]
    public float minDistance = 0.1f;

    [Header("Falloff")]
    [Tooltip("0..1 を入力に 1..0 を出す減衰カーブ。t=距離/半径")]
    public AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Directional (mode=Directional時)")]
    [Tooltip("方向ベクトル（Transformの右向きを使うなら Vector2.right を使い、回転で調整）")]
    public Vector2 direction = Vector2.right;

    // すべてのFlowSourceの簡易レジストリ
    public static readonly HashSet<FlowSource2D> All = new();

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>ワールド座標posに与える力（ベクトル）を返す</summary>
    public Vector2 ForceAt(Vector2 pos)
    {
        Vector2 toTarget = pos - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist > radius) return Vector2.zero;

        float t = Mathf.Clamp01(dist / Mathf.Max(radius, 0.0001f));
        float w = falloff.Evaluate(t); // 近い:~1 / 遠い:~0

        if (mode == Mode.Radial)
        {
            if (dist < Mathf.Max(minDistance, 0.0001f)) dist = minDistance;
            Vector2 dir = toTarget / dist; // 源→対象（放射状に押し出す）
            return dir * strength * w;
        }
        else // Directional
        {
            Vector2 dir = direction.sqrMagnitude < 1e-6f
                ? (Vector2)transform.right
                : direction.normalized;
            return dir * strength * w;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
        if (mode == Mode.Directional)
        {
            Vector3 p = transform.position;
            Vector3 d = (Vector3)(direction.sqrMagnitude < 1e-6f ? (Vector2)transform.right : direction.normalized);
            Gizmos.DrawLine(p, p + d * Mathf.Min(radius, 1.5f));
        }
    }
}
