using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FlowManager2D : MonoBehaviour
{
    [Header("Sampling")]
    public bool autoCollect = true; // 子階層から自動収集
    public List<MonoBehaviour> sourceBehaviours = new(); // IFlowSource2D 実装
    public List<FlowWall2D> walls = new();

    [Header("Saturation (optional)")]
    public bool saturate = false;
    public float satScale = 8f; // 合成が強すぎる場合の和のソフトサチュレーション

    [Header("Debug Gizmo")]
    public bool drawGizmos = true;
    public Vector2 center = Vector2.zero;
    public Vector2 size   = new(20f, 12f);
    public float step = 1.5f;
    public float arrowScale = 0.35f;
    public float minMagnitudeToDraw = 0.05f;

    readonly List<IFlowSource2D> _sources = new();

    void OnEnable()  { RefreshLists(); }
    void OnValidate(){ RefreshLists(); }

    public void RefreshLists()
    {
        _sources.Clear();
        if (autoCollect)
        {
            GetComponentsInChildren(true, sourceBehaviours);
            walls.Clear();
            GetComponentsInChildren(true, walls);
        }
        foreach (var mb in sourceBehaviours)
        {
            if (!mb) continue;
            if (mb is IFlowSource2D s) _sources.Add(s);
        }
    }

    /// <summary>ワールド座標で流速 u(x) を返す（合成＋壁補正）</summary>
    public Vector2 SampleVelocity(Vector2 pos)
    {
        Vector2 u = Vector2.zero;

        // Source 合成
        if (_sources.Count == 1) u = _sources[0].SampleVelocity(pos);
        else
        {
            float denom = 1f;
            foreach (var s in _sources)
            {
                var ui = s.SampleVelocity(pos);
                u += ui;
                if (saturate) denom += ui.magnitude / Mathf.Max(0.0001f, satScale);
            }
            if (saturate) u /= denom;
        }

        // 壁補正（Slide）
        foreach (var w in walls)
            if (w) w.ApplyWallEffect(pos, ref u);

        return u;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(0.8f, 0.95f, 1f, 0.9f);
        Vector2 half = size * 0.5f;
        Vector3 p0 = new(center.x - half.x, center.y - half.y, 0);
        Vector3 p1 = new(center.x + half.x, center.y - half.y, 0);
        Vector3 p2 = new(center.x + half.x, center.y + half.y, 0);
        Vector3 p3 = new(center.x - half.x, center.y + half.y, 0);
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);

        if (step <= 0.001f) return;

        for (float y = center.y - half.y; y <= center.y + half.y; y += step)
        for (float x = center.x - half.x; x <= center.x + half.x; x += step)
        {
            Vector2 pos = new Vector2(x, y);
            Vector2 u = SampleVelocity(pos);
            float m = u.magnitude;
            if (m < minMagnitudeToDraw) continue;

            Vector3 a = new(x, y, 0);
            Vector3 b = a + (Vector3)(u.normalized * Mathf.Min(m * arrowScale, step * 0.8f));

            Gizmos.DrawLine(a, b);
            // 矢じり
            Vector3 t = (b - a).normalized;
            Vector3 l = new(-t.y, t.x, 0);
            Gizmos.DrawLine(b, b - (t * 0.15f + l * 0.10f));
            Gizmos.DrawLine(b, b - (t * 0.15f - l * 0.10f));
        }
    }
}
