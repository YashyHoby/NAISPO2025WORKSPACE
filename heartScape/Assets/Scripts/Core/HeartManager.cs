using UnityEngine;
using System.Collections.Generic;

public class HeartManager : MonoBehaviour
{
    public List<HeartAgent> agents = new();
    public GameObject agentPrefab;
    public int   maxAgents = 140;

    // 画面外消しを使うなら true（ReflectiveBoundary を使う場合は false 推奨）
    public bool despawnOffscreen = false;
    public Vector2 despawnExtents = new Vector2(10f, 6f); // ±X, ±Y

    void FixedUpdate()
    {
        if (despawnOffscreen) DespawnOffscreen();
    }

    public HeartAgent Spawn(HeartProfile hp, Vector2 pos, Vector2 vel)
    {
        if (agents.Count >= maxAgents || agentPrefab == null) return null;

        var go = Instantiate(agentPrefab, pos, Quaternion.identity);
        var a  = go.GetComponent<HeartAgent>();

        // ハブ
        a.profile = hp;
        a.id      = Random.Range(int.MinValue, int.MaxValue);

        // Visual
        var vis = go.GetComponent<HeartVisual>();
        if (vis)
        {
            //vis.shapeType = (ShapeType)Random.Range(0, 3);
            vis.color     = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.75f, 1f);
        }

        // Growth
        var gr = go.GetComponent<HeartGrowth>();
        if (gr)
        {
            gr.baseRadius  = Mathf.Lerp(0.45f, 0.7f, Mathf.Clamp01(hp.mean / 120f));
            gr.growthStage = 0;
            gr.ApplyStageScale();          // これが visual.radius を内部的に更新
        }

        // 物理（初速）
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = vel;   // 旧Unityなら rb.velocity

        agents.Add(a);
        return a;
    }

    void DespawnOffscreen()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var a = agents[i];
            if (!a) { agents.RemoveAt(i); continue; }

            var p = a.transform.position;
            if (Mathf.Abs(p.x) > despawnExtents.x || Mathf.Abs(p.y) > despawnExtents.y)
            {
                Destroy(a.gameObject);
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
