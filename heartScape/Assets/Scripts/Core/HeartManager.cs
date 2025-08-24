using UnityEngine;
using System.Collections.Generic;

public class HeartManager : MonoBehaviour
{
    public List<HeartAgent> agents = new();
    public GameObject agentPrefab;
    public int   maxAgents = 140;
    public float cellSize  = 1.0f;

    Dictionary<(int,int), List<HeartAgent>> cells = new();

    static readonly (int,int)[] neigh = {
        (0,0),(1,0),(0,1),(-1,0),(0,-1),(1,1),(-1,1),(1,-1),(-1,-1)
    };

    void FixedUpdate()
    {
        BuildSpatialHash();
        HandleCollisions();
        DespawnOffscreen();
    }

    public HeartAgent Spawn(HeartProfile hp, Vector2 pos, Vector2 vel)
    {
        if (agents.Count >= maxAgents || agentPrefab == null) return null;

        var go = Instantiate(agentPrefab, pos, Quaternion.identity);
        var a  = go.GetComponent<HeartAgent>();

        a.profile = hp;
        a.shapeType = (ShapeType)Random.Range(0, 3);
        a.color   = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.75f, 1f);
        a.vel     = vel;
        a.id      = Random.Range(int.MinValue, int.MaxValue);

        // 初期半径（基準×段階スケールを適用）
        a.baseRadius  = Mathf.Lerp(0.45f, 0.7f, Mathf.Clamp01(hp.mean / 120f));
        a.growthStage = 0;
        a.ApplyStageScale(); // public にしたくない場合は Spawn 後に radius を直指定でもOK

        agents.Add(a);
        return a;
    }

    void BuildSpatialHash()
    {
        cells.Clear();
        foreach (var a in agents)
        {
            var c = ((int)Mathf.Floor(a.transform.position.x / cellSize),
                     (int)Mathf.Floor(a.transform.position.y / cellSize));
            if (!cells.TryGetValue(c, out var list)) { list = new(); cells[c] = list; }
            list.Add(a);
        }
    }

    void HandleCollisions()
    {
        foreach (var kv in cells)
        {
            foreach (var d in neigh)
            {
                var key = (kv.Key.Item1 + d.Item1, kv.Key.Item2 + d.Item2);
                if (!cells.TryGetValue(key, out var list)) continue;
                int L = list.Count;

                for (int i = 0; i < L; i++)
                for (int j = i + 1; j < L; j++)
                {
                    var a = list[i]; var b = list[j];

                    // ★ バブル捕獲中は衝突判定スキップ
                    if (a.isCaptured || b.isCaptured) continue;

                    float r = a.radius + b.radius;
                    Vector2 delta = (Vector2)(b.transform.position - a.transform.position);
                    float dist2 = delta.sqrMagnitude;
                    if (dist2 >= r * r) continue;

                    float dist = Mathf.Max(Mathf.Sqrt(dist2), 1e-4f);
                    Vector2 n  = delta / dist;
                    float rel  = Vector2.Dot(b.vel - a.vel, n);

                    // 反発＋食い込み解消
                    float pen = r - dist;
                    a.transform.position -= (Vector3)(n * pen * 0.5f);
                    b.transform.position += (Vector3)(n * pen * 0.5f);
                    a.vel -= n * rel * 0.5f;
                    b.vel += n * rel * 0.5f;

                    // 相対速度が低ければ合体（体積保存）
                    if (Mathf.Abs(rel) < 0.8f)
                        TryMerge(a, b);

                    // 衝突での分裂は行わない（energy も不使用）
                    AudioHub.PlayHit(a, b, rel);
                }
            }
        }
    }

    void TryMerge(HeartAgent a, HeartAgent b)
    {
        if (a == null || b == null) return;
        var big   = (a.radius >= b.radius) ? a : b;
        var small = (a.radius >= b.radius) ? b : a;

        float r2 = big.radius * big.radius + small.radius * small.radius;
        Vector2 v = (big.vel * big.radius + small.vel * small.radius) / (big.radius + small.radius);

        big.radius = Mathf.Sqrt(r2);
        big.vel    = v;

        // 成長段階は維持（見た目上のサイズは radius を優先）
        agents.Remove(small);
        if (small != null) Destroy(small.gameObject);
    }

    void DespawnOffscreen()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var a = agents[i];
            var p = a.transform.position;
            if (Mathf.Abs(p.x) > 10f || Mathf.Abs(p.y) > 6f)
            {
                if (a != null) Destroy(a.gameObject);
                agents.RemoveAt(i);
            }
        }
    }

    // デバッグ用：全消去
    public void ClearAll()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
            if (agents[i] != null) Destroy(agents[i].gameObject);
        agents.Clear();
    }
}
