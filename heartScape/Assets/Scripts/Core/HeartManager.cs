using UnityEngine;
using System.Collections.Generic;

public class HeartManager : MonoBehaviour
{
    public List<HeartAgent> agents = new();
    public GameObject agentPrefab;
    public int maxAgents = 140;

    [Header("Spatial Hash")]
    public float cellSize = 1.0f;
    private readonly Dictionary<(int, int), List<HeartAgent>> cells = new();

    // 近傍チェック用オフセット
    private static readonly (int, int)[] neigh = new (int, int)[]
    {
        (0,0),(1,0),(0,1),(-1,0),(0,-1),(1,1),(-1,1),(1,-1),(-1,-1)
    };

    void FixedUpdate()
    {
        BuildSpatialHash();
        HandleCollisions();
        DespawnOffscreen();
    }

    // ====== Public API ======
    public HeartAgent Spawn(HeartProfile hp, Vector2 pos, Vector2 vel)
    {
        if (agents.Count >= maxAgents || agentPrefab == null) return null;

        var go = Instantiate(agentPrefab, pos, Quaternion.identity);
        var a = go.GetComponent<HeartAgent>();

        a.profile = hp;
        a.radius = Mathf.Lerp(0.25f, 0.7f, Mathf.Clamp01(hp.mean / 120f));
        a.color = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.75f, 1f);
        a.shapeType = (ShapeType)Random.Range(0, 3);
        a.vel = vel;
        a.id = Random.Range(int.MinValue, int.MaxValue);

        agents.Add(a);
        return a;
    }

    // デバッグ/掃除用：全消去
    public void ClearAll()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            if (agents[i] != null) Destroy(agents[i].gameObject);
        }
        agents.Clear();
    }

    // ====== Internal ======
    private void BuildSpatialHash()
    {
        cells.Clear();
        foreach (var a in agents)
        {
            var key = (
                (int)Mathf.Floor(a.transform.position.x / cellSize),
                (int)Mathf.Floor(a.transform.position.y / cellSize)
            );
            if (!cells.TryGetValue(key, out var list))
            {
                list = new List<HeartAgent>();
                cells[key] = list;
            }
            list.Add(a);
        }
    }

    private void HandleCollisions()
    {
        foreach (var kv in cells)
        {
            foreach (var d in neigh)
            {
                var k = (kv.Key.Item1 + d.Item1, kv.Key.Item2 + d.Item2);
                if (!cells.TryGetValue(k, out var list)) continue;

                int L = list.Count;
                for (int i = 0; i < L; i++)
                {
                    for (int j = i + 1; j < L; j++)
                    {
                        var a = list[i];
                        var b = list[j];
                        float r = a.radius + b.radius;
                        Vector2 delta = (Vector2)(b.transform.position - a.transform.position);
                        float dist2 = delta.sqrMagnitude;
                        if (dist2 >= r * r) continue;

                        float dist = Mathf.Max(Mathf.Sqrt(dist2), 1e-4f);
                        Vector2 n = delta / dist;
                        float rel = Vector2.Dot(b.vel - a.vel, n);

                        // 反発＆めり込み解消
                        float pen = r - dist;
                        a.transform.position -= (Vector3)(n * pen * 0.5f);
                        b.transform.position += (Vector3)(n * pen * 0.5f);
                        a.vel -= n * rel * 0.5f;
                        b.vel += n * rel * 0.5f;

                        // 合体 or エネルギ蓄積
                        if (Mathf.Abs(rel) < 0.8f)
                        {
                            TryMerge(a, b);
                        }
                        else
                        {
                            a.energy += Mathf.Abs(rel) * 0.1f;
                            b.energy += Mathf.Abs(rel) * 0.1f;
                            if (a.energy > 2.5f) TrySplit(a);
                            if (b.energy > 2.5f) TrySplit(b);
                        }

                        // 衝突音（AudioHub を用意していないならこの行はコメントアウトOK）
                        AudioHub.PlayHit(a, b, rel);
                    }
                }
            }
        }
    }

    private void TryMerge(HeartAgent a, HeartAgent b)
    {
        if (a == null || b == null) return;
        var big = (a.radius >= b.radius) ? a : b;
        var small = (a.radius >= b.radius) ? b : a;

        float r2 = big.radius * big.radius + small.radius * small.radius;
        Vector2 v = (big.vel * big.radius + small.vel * small.radius) / (big.radius + small.radius);
        big.radius = Mathf.Sqrt(r2);
        big.vel = v;
        big.energy *= 0.5f;

        agents.Remove(small);
        if (small != null) Destroy(small.gameObject);
    }

    private void TrySplit(HeartAgent a)
    {
        a.energy = 0f;
        if (a.radius < 0.35f) return;

        float childR = a.radius * 0.7f;
        a.radius = childR;

        var hp = a.profile;
        var pos = (Vector2)a.transform.position;
        var dir = Random.insideUnitCircle.normalized;
        var child = Spawn(hp, pos + dir * childR * 0.6f, a.vel + dir * 1.2f);
        if (child != null) child.radius = childR;
    }

    private void DespawnOffscreen()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var a = agents[i];
            var p = a.transform.position;
            if (Mathf.Abs(p.x) > 10f || Mathf.Abs(p.y) > 6f)
            {
                Destroy(a.gameObject);
                agents.RemoveAt(i);
            }
        }
    }
}
