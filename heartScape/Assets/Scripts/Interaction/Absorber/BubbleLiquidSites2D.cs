using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class BubbleLiquidSites2D : MonoBehaviour
{
    [System.Serializable]
    struct Site { public Vector2 uv; public Vector2 vel; public Color col; public float radius; }

    [Header("Material")]
    // M_BubbleLiquidVoronoi2D を割り当てる
    public Material liquidMat;

    [Header("Voronoi / Motion")]
    public int maxSites = 16;   // 同時色エリア数
    public float initRadius = 0.08f;
    public float attractK = 0.9f; // 中心への引力
    public float damp = 0.88f;
    public float jitter = 0.35f;
    public float siteFromEdgePush = 0.12f; // 生成時に外縁から内側へ押す距離

    [Header("Debug")]
    public bool seedOnStart = false;      // 起動テスト用：1点だけ色を出す
    public Color seedColor = Color.red;

    readonly List<Site> sites = new();
    readonly Vector4[] siteBuf = new Vector4[16];
    readonly Vector4[] colBuf = new Vector4[16];

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // 共有ではなくインスタンス化マテリアルを使う
        if (liquidMat == null && sr != null) liquidMat = sr.material;
        SyncToMat();
    }

    void OnValidate()
    {
        // エディタ上での割り当て漏れにも対応
        var r = GetComponent<SpriteRenderer>();
        if (r != null)
        {
            var shared = r.sharedMaterial;
            if (liquidMat == null || ReferenceEquals(liquidMat, shared))
                liquidMat = Application.isPlaying ? r.material : shared;
        }
        SyncToMat();
    }

    void Start()
    {
        if (seedOnStart)
        {
            // 画面中心の少し右に赤いサイトを1つ作る（動作確認用。不要になったらOFF）
            AddFromWorld(transform.position + Vector3.right * 0.1f, seedColor);
        }
    }

    public void ClearAll()
    {
        sites.Clear();
        SyncToMat();
    }

    public void AddFromWorld(Vector2 worldPos, Color col)
    {
        Vector2 uv = WorldToUV(worldPos);
        Vector2 c = new Vector2(0.5f, 0.5f);
        Vector2 toC = (c - uv);
        Vector2 dir = toC.sqrMagnitude > 1e-6f ? toC.normalized : Vector2.up;

        // 外縁から少し内側へ押し込んでから生成
        uv += dir * siteFromEdgePush;

        if (sites.Count >= maxSites) sites.RemoveAt(0);
        sites.Add(new Site { uv = uv, vel = dir * 0.1f, col = col, radius = initRadius });
        SyncToMat();
    }

    Vector2 WorldToUV(Vector2 w)
    {
        Vector2 local = transform.InverseTransformPoint(w);
        return local + new Vector2(0.5f, 0.5f); // [-0.5..0.5] → [0..1]
    }

    void Update()
    {
        if (sites.Count == 0)
        {
            if (liquidMat != null) liquidMat.SetFloat("_SiteCount", 0);
            return;
        }

        Vector2 c = new Vector2(0.5f, 0.5f);
        float dt = Time.deltaTime;

        for (int i = 0; i < sites.Count; i++)
        {
            var s = sites[i];
            Vector2 a = (c - s.uv) * attractK;
            s.vel = (s.vel + a * dt + Random.insideUnitCircle * jitter * dt) * damp;
            s.uv = s.uv + s.vel * dt;
            // 枠内にクランプ
            s.uv = Vector2.Min(Vector2.one * 0.98f, Vector2.Max(Vector2.one * 0.02f, s.uv));
            sites[i] = s;
        }

        SyncToMat();
    }

    void SyncToMat()
    {
        if (liquidMat == null) return;

        int n = Mathf.Min(sites.Count, siteBuf.Length);
        for (int i = 0; i < n; i++)
        {
            var s = sites[i];
            siteBuf[i] = new Vector4(s.uv.x, s.uv.y, s.radius, 0);
            colBuf[i] = new Vector4(s.col.r, s.col.g, s.col.b, 1);
        }

        liquidMat.SetFloat("_SiteCount", n);
        liquidMat.SetVectorArray("_Sites", siteBuf);
        liquidMat.SetVectorArray("_SiteCols", colBuf);
    }
    
    // BubbleLiquidSites2D.cs のクラス内に追加
    public void GetPalette(List<Color> dst)
    {
        if (dst == null) return;
        dst.Clear();

        // サイト（色の種）から色を収集。最大16色まで（重複はゆるく除外）
        const int MAX = 16;
        const float THRESH = 0.06f; // 近い色は同一扱い（0〜√3）

        int count = Mathf.Min(sites.Count, MAX);
        for (int i = 0; i < count; i++)
        {
            var c = sites[i].col;
            c.a = 1f;

            bool dup = false;
            for (int j = 0; j < dst.Count; j++)
            {
                var d = dst[j];
                float dd = Mathf.Abs(c.r - d.r) + Mathf.Abs(c.g - d.g) + Mathf.Abs(c.b - d.b);
                if (dd < THRESH) { dup = true; break; }
            }
            if (!dup) dst.Add(c);
        }
    }

}





