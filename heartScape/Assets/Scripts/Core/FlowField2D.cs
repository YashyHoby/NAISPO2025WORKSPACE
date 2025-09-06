using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FlowField2D : MonoBehaviour
{
    public enum Authoring { FromEmitters, ProceduralNoise }
    [Header("Authoring")]
    public Authoring authoring = Authoring.FromEmitters;

    [Header("Field Area")]
    public Vector2 fieldSize = new Vector2(20, 12); // ワールド単位
    public float cellSize = 1.0f;                   // グリッド解像度
    public Vector2 originOffset = Vector2.zero;     // 原点オフセット（中心基準）

    [Header("Bake (FromEmitters)")]
    public float emitterWriteScale = 1f;            // エミッタの力を全体でスケール
    public bool normalizeCells = false;             // 細胞ベクトルを正規化するか

    [Header("Procedural Noise")]
    public float noiseAmp = 1f;
    public float noiseFreq = 0.1f;
    public float timeSpeed = 0.0f;                  // 0で静的、>0で時間で変化

    [Header("Gizmos")]
    public bool drawGizmos = true;
    [Range(1,6)] public int gizmoStride = 2;        // 何セルに1本描くか
    public float gizmoArrowScale = 0.6f;
    public Color gizmoColor = new(0f, .7f, 1f, .85f);

    Vector2[,] field;
    int nx, ny;
    float lastBakeTime = -999f;

    // --- Public API ---
    public Vector2 GetForceAt(Vector2 worldPos)
    {
        if (field == null) Rebuild();
        // 座標→ローカル→セル座標
        Vector2 local = (Vector2)transform.InverseTransformPoint(worldPos);
        Vector2 half = fieldSize * 0.5f;
        Vector2 p = local - originOffset + half;

        if (p.x < 0 || p.y < 0 || p.x > fieldSize.x || p.y > fieldSize.y)
            return Vector2.zero;

        float fx = p.x / cellSize;
        float fy = p.y / cellSize;

        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, nx-1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, ny-1);
        int x1 = Mathf.Min(x0 + 1, nx-1);
        int y1 = Mathf.Min(y0 + 1, ny-1);

        float tx = Mathf.Clamp01(fx - x0);
        float ty = Mathf.Clamp01(fy - y0);

        Vector2 v00 = field[x0,y0];
        Vector2 v10 = field[x1,y0];
        Vector2 v01 = field[x0,y1];
        Vector2 v11 = field[x1,y1];

        // バイリニア補間
        Vector2 vx0 = Vector2.Lerp(v00, v10, tx);
        Vector2 vx1 = Vector2.Lerp(v01, v11, tx);
        return Vector2.Lerp(vx0, vx1, ty);
    }

    // --- Build / Bake ---
    public void Rebuild()
    {
        nx = Mathf.Max(1, Mathf.RoundToInt(fieldSize.x / Mathf.Max(0.0001f, cellSize)) + 1);
        ny = Mathf.Max(1, Mathf.RoundToInt(fieldSize.y / Mathf.Max(0.0001f, cellSize)) + 1);
        field = new Vector2[nx, ny];

        if (authoring == Authoring.FromEmitters) BakeEmitters();
        else BakeNoise();

        lastBakeTime = Application.isPlaying ? Time.time : -1f;
    }

    void BakeEmitters()
    {
        // まずゼロで初期化
        for (int y=0; y<ny; y++)
            for (int x=0; x<nx; x++)
                field[x,y] = Vector2.zero;

        // 子孫にある FlowEmitter2D を収集
        List<FlowEmitter2D> emitters = new();
        GetComponentsInChildren(true, emitters);

        Vector2 half = fieldSize * 0.5f;

        foreach (var e in emitters)
        {
            if (!e.enabled) continue;
            Vector2 ep = e.transform.position;
            float r = e.radius;
            float r2 = r*r;

            // 影響範囲だけ走査（高速化）
            Vector2 minP = ep - new Vector2(r, r);
            Vector2 maxP = ep + new Vector2(r, r);

            int xMin = Mathf.Clamp(WorldToCell(minP).x, 0, nx-1);
            int yMin = Mathf.Clamp(WorldToCell(minP).y, 0, ny-1);
            int xMax = Mathf.Clamp(WorldToCell(maxP).x, 0, nx-1);
            int yMax = Mathf.Clamp(WorldToCell(maxP).y, 0, ny-1);

            for (int y=yMin; y<=yMax; y++)
            for (int x=xMin; x<=xMax; x++)
            {
                Vector2 wp = CellToWorld(x,y);
                float d2 = (wp - ep).sqrMagnitude;
                if (d2 > r2) continue;

                float d = Mathf.Max(Mathf.Sqrt(d2), 0.0001f);
                float t = Mathf.Clamp01(d / r);
                float w = e.falloff.Evaluate(t);     // 近い＝1, 遠い＝0

                Vector2 dir;
                if (e.mode == FlowEmitter2D.Mode.Radial)
                    dir = (wp - ep).normalized * e.sign;   // sign=+1:押し出し, -1:吸い込み
                else
                    dir = (e.DirectionWorld).normalized;

                field[x,y] += dir * (e.strength * w * emitterWriteScale);
            }
        }

        if (normalizeCells)
        {
            for (int y=0; y<ny; y++)
            for (int x=0; x<nx; x++)
            {
                float m = field[x,y].magnitude;
                if (m > 1e-5f) field[x,y] /= m;
            }
        }
    }

    void BakeNoise()
    {
        float t = (timeSpeed > 0f) ? Time.time * timeSpeed : 0f;
        for (int y=0; y<ny; y++)
        for (int x=0; x<nx; x++)
        {
            Vector2 p = CellToWorld(x,y) * noiseFreq;
            float a = Mathf.PerlinNoise(p.x + 123.4f, p.y + t) * Mathf.PI * 2f;
            field[x,y] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * noiseAmp;
        }
    }

    // Helper: セル<->ワールド変換
    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        Vector2 local = (Vector2)transform.InverseTransformPoint(worldPos);
        Vector2 half = fieldSize * 0.5f;
        Vector2 p = local - originOffset + half;
        return new Vector2Int(
            Mathf.FloorToInt(p.x / Mathf.Max(0.0001f, cellSize)),
            Mathf.FloorToInt(p.y / Mathf.Max(0.0001f, cellSize))
        );
    }

    public Vector2 CellToWorld(int x, int y)
    {
        Vector2 half = fieldSize * 0.5f;
        Vector2 local = new Vector2(x * cellSize, y * cellSize) - half + originOffset;
        return transform.TransformPoint(local);
    }

    void OnValidate() { Rebuild(); }
    void Update()
    {
        if (authoring == Authoring.ProceduralNoise && timeSpeed > 0f) Rebuild();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || field == null) return;
        Gizmos.color = gizmoColor;

        for (int y=0; y<ny; y+=gizmoStride)
        for (int x=0; x<nx; x+=gizmoStride)
        {
            Vector2 p = CellToWorld(x,y);
            Vector2 v = field[x,y];
            Vector2 q = p + v * gizmoArrowScale;

            Gizmos.DrawLine(p, q);
            // 矢じり
            Vector2 t = (q - p);
            if (t.sqrMagnitude > 1e-6f)
            {
                t.Normalize();
                Vector2 left = new(-t.y, t.x);

                // ← ここで型をそろえる：q も Vector3 にキャストして演算
                Vector3 q3    = (Vector3)q;
                Vector3 tip1  = q3 - new Vector3(t.x * 0.15f + left.x * 0.10f, t.y * 0.15f + left.y * 0.10f, 0f);
                Vector3 tip2  = q3 - new Vector3(t.x * 0.15f - left.x * 0.10f, t.y * 0.15f - left.y * 0.10f, 0f);

                Gizmos.DrawLine(q3, tip1);
                Gizmos.DrawLine(q3, tip2);
            }

        }

        // 枠
        Gizmos.color = new Color(1,1,1,0.2f);
        Vector3 c = transform.TransformPoint(originOffset);
        Vector3 sx = transform.TransformVector(new Vector3(fieldSize.x,0,0));
        Vector3 sy = transform.TransformVector(new Vector3(0,fieldSize.y,0));
        Vector3 p0 = c - sx*0.5f - sy*0.5f;
        Vector3 p1 = c + sx*0.5f - sy*0.5f;
        Vector3 p2 = c + sx*0.5f + sy*0.5f;
        Vector3 p3 = c - sx*0.5f + sy*0.5f;
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
    }
}
