using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PolygonCollider2D の外周パスを元に、角を丸めた描画用メッシュを生成して MeshFilter に適用する。
/// 物理（Collider）は変更しない＝見た目だけ丸める。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoundedFromCollider2D : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private PolygonCollider2D sourceCollider; // 未指定なら親から自動取得

    [Header("Rounding")]
    [Tooltip("角とみなす内角の最大値（度）。これ以下の鋭い角のみ丸める。例: 135")]
    [Range(30f, 179.9f)] public float angleThresholdDeg = 135f;
    [Tooltip("フィレット半径（ローカル単位）。各角では辺長・角度に合わせて自動クランプされる。")]
    public float baseCornerRadius = 0.2f;
    [Tooltip("各角を構成する円弧の分割数。大きいほど滑らか（コスト↑）。")]
    [Range(1, 16)] public int arcSegments = 5;

    [Header("Update")]
    [Tooltip("毎フレーム再生成（形状が頻繁に変わる場合）。オフなら手動で Rebuild() を呼ぶ。")]
    public bool liveUpdate = true;

    private MeshFilter _mf;
    private Mesh _mesh;

    void Reset()
    {
        _mf = GetComponent<MeshFilter>();
        if (sourceCollider == null) sourceCollider = GetComponentInParent<PolygonCollider2D>();
    }

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        if (sourceCollider == null) sourceCollider = GetComponentInParent<PolygonCollider2D>();
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "RoundedRenderMesh" };
            _mesh.MarkDynamic();
            _mf.sharedMesh = _mesh;
        }
        Rebuild();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "RoundedRenderMesh" };
                _mf.sharedMesh = _mesh;
            }
            Rebuild();
        }
    }

    void Update()
    {
        if (liveUpdate) Rebuild();
    }

    /// <summary>外周の丸め → 三角形分割 → メッシュ反映</summary>
    public void Rebuild()
    {
        if (sourceCollider == null || sourceCollider.pathCount == 0) return;

        // ここでは最初のパスのみ（穴がある場合は拡張して複数パス対応してください）
        var path = new List<Vector2>();
        sourceCollider.GetPath(0, path);

        if (path.Count < 3) return;

        // 反時計回り（CCW）に正規化
        if (SignedArea(path) < 0f) path.Reverse();

        // 丸めた外周を生成
        var rounded = BuildRoundedOutline(path, angleThresholdDeg * Mathf.Deg2Rad, baseCornerRadius, arcSegments);

        // 三角形分割（Ear-Clipping）
        var indices = TriangulateEarClipping(rounded);

        // UV は AABB で簡易展開（ゼリー用なら十分）
        var uvs = BuildBoxUV(rounded);

        // メッシュ更新（2Dなので z=0）
        var verts3 = new Vector3[rounded.Count];
        for (int i = 0; i < rounded.Count; i++) verts3[i] = new Vector3(rounded[i].x, rounded[i].y, 0f);

        _mesh.Clear();
        _mesh.SetVertices(verts3);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(indices, 0, true);
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals(); // 2Dでもリフラクション用に法線が欲しければ擬似的に
    }

    // --- 幾何ユーティリティ ---

    static float SignedArea(List<Vector2> poly)
    {
        float a = 0f;
        for (int i = 0, n = poly.Count; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            a += (p.x * q.y - q.x * p.y);
        }
        return a * 0.5f;
    }

    static List<Vector2> BuildRoundedOutline(List<Vector2> path, float angleThresholdRad, float baseRadius, int segPerArc)
    {
        var outPts = new List<Vector2>(path.Count + path.Count * segPerArc);

        int n = path.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = path[(i - 1 + n) % n];
            Vector2 curr = path[i];
            Vector2 next = path[(i + 1) % n];

            Vector2 e1 = (prev - curr).normalized; // curr→prev
            Vector2 e2 = (next - curr).normalized; // curr→next

            // 内角 α：-e1 と e2 のなす角
            float cosA = Mathf.Clamp(Vector2.Dot(-e1, e2), -1f, 1f);
            float alpha = Mathf.Acos(cosA); // 0..π

            // 凸判定（CCWで左折 = convex）
            float cross = e1.x * e2.y - e1.y * e2.x;
            bool isConvex = cross > 0f;

            // 丸め対象？
            if (isConvex && alpha <= angleThresholdRad && baseRadius > 0f)
            {
                float len1 = (curr - prev).magnitude;
                float len2 = (next - curr).magnitude;

                // d = r / tan(α/2) が各辺長以下であるように半径クランプ
                float tanHalf = Mathf.Tan(alpha * 0.5f);
                float maxR1 = len1 * tanHalf;
                float maxR2 = len2 * tanHalf;
                float r = Mathf.Min(baseRadius, maxR1, maxR2);
                float d = r / tanHalf;

                // 接点
                Vector2 t1 = curr + e1 * d;
                Vector2 t2 = curr + e2 * d;

                // 円心 C = curr + bisector * (r / sin(α/2))
                Vector2 bis = (e1 + e2).normalized; // 凸なら内向き
                float h = r / Mathf.Sin(alpha * 0.5f);
                Vector2 C = curr + bis * h;

                // 円弧（t1→t2 を CCW に辿る）
                Vector2 v1 = t1 - C;
                Vector2 v2 = t2 - C;

                float start = Mathf.Atan2(v1.y, v1.x);
                float end = Mathf.Atan2(v2.y, v2.x);

                float delta = Mathf.Atan2(v1.x * v2.y - v1.y * v2.x, Vector2.Dot(v1, v2)); // signed angle from v1 to v2
                if (delta <= 0f) delta += Mathf.PI * 2f; // CCW に

                // まず t1 を入れる（辺の連続性）
                outPts.Add(t1);
                for (int s = 1; s < segPerArc; s++)
                {
                    float t = s / (float)segPerArc;
                    float ang = start + delta * t;
                    outPts.Add(C + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
                }
                outPts.Add(t2);
            }
            else
            {
                // 角をそのまま通す
                outPts.Add(curr);
            }
        }

        return outPts;
    }

    static List<int> TriangulateEarClipping(List<Vector2> poly)
    {
        // 簡易 Ear-Clipping（凸耳を探して切り取る）
        int n = poly.Count;
        var indicesList = new List<int>();
        var V = new List<int>(n);
        for (int i = 0; i < n; i++) V.Add(i);

        int guard = 0;
        while (V.Count > 3 && guard++ < 10000)
        {
            bool earFound = false;
            for (int i = 0; i < V.Count; i++)
            {
                int i0 = V[(i - 1 + V.Count) % V.Count];
                int i1 = V[i];
                int i2 = V[(i + 1) % V.Count];

                Vector2 a = poly[i0];
                Vector2 b = poly[i1];
                Vector2 c = poly[i2];

                // 凸？
                Vector2 ab = b - a;
                Vector2 bc = c - b;
                float cross = ab.x * bc.y - ab.y * bc.x;
                if (cross <= 0f) continue; // CCW想定。凹は不可

                // 他点が三角形内に無いか
                bool contains = false;
                for (int j = 0; j < V.Count; j++)
                {
                    int vi = V[j];
                    if (vi == i0 || vi == i1 || vi == i2) continue;
                    if (PointInTri(poly[vi], a, b, c)) { contains = true; break; }
                }
                if (contains) continue;

                // 耳として採用
                indicesList.Add(i0);
                indicesList.Add(i1);
                indicesList.Add(i2);
                V.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound) break; // 失敗（自己交差など）
        }

        if (V.Count == 3)
        {
            indicesList.Add(V[0]); indicesList.Add(V[1]); indicesList.Add(V[2]);
        }
        return indicesList;
    }

    static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // バarycentricによる CCW 三角形内判定
        float s1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        float s2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
        float s3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
        bool hasNeg = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
        bool hasPos = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);
        return !(hasNeg && hasPos);
    }

    static List<Vector2> BuildBoxUV(List<Vector2> poly)
    {
        var uvs = new List<Vector2>(poly.Count);
        Vector2 min = poly[0], max = poly[0];
        for (int i = 1; i < poly.Count; i++)
        {
            min = Vector2.Min(min, poly[i]);
            max = Vector2.Max(max, poly[i]);
        }
        Vector2 size = max - min;
        if (size.x < 1e-6f) size.x = 1e-6f;
        if (size.y < 1e-6f) size.y = 1e-6f;

        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 uv = new Vector2((poly[i].x - min.x) / size.x, (poly[i].y - min.y) / size.y);
            uvs.Add(uv);
        }
        return uvs;
    }
}
