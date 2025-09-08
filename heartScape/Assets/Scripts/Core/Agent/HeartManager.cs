using UnityEngine;
using System.Collections.Generic;

public class HeartManager : MonoBehaviour
{
    public HeartAppearanceMapper appearance;
    public List<HeartAgent> agents = new();
    public GameObject agentPrefab;
    public int   maxAgents = 140;

    public bool despawnOffscreen = false;
    public Vector2 despawnExtents = new Vector2(10f, 6f); // ±X, ±Y

    void Awake()
    {
        if (appearance == null)
        {
            appearance = FindFirstObjectByType<HeartAppearanceMapper>();
            if (appearance == null)
                Debug.LogWarning("[HeartManager] HeartAppearanceMapper not set (fallback visuals).");
        }
    }

    void FixedUpdate()
    {
        if (despawnOffscreen) DespawnOffscreen();
    }

    public HeartAgent Spawn(HeartProfile hp, Vector2 pos, Vector2 vel)
    {
        if (agents.Count >= maxAgents || agentPrefab == null) return null;

        var go = Instantiate(agentPrefab, pos, Quaternion.identity);
        var a  = go.GetComponent<HeartAgent>();

        a.profile = hp;
        a.id      = Random.Range(int.MinValue, int.MaxValue);

        var vis = go.GetComponent<HeartVisual>();
        if (vis)
        {
            if (appearance != null)
            {
                appearance.Apply(hp, vis); // 形/色/半径（半径は appearance.setRadiusOnSpawn 次第）
            }
            else
            {
                vis.color = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.75f, 1f);
            }
        }

        var gr = go.GetComponent<HeartGrowth>();
        if (gr)
        {
            // Mapperで半径を決める運用なら Growth による初期上書きをスキップ
            bool mapperSetsRadius = (appearance != null && appearance.setRadiusOnSpawn);
            if (!mapperSetsRadius)
            {
                gr.baseRadius  = Mathf.Lerp(0.45f, 0.7f, Mathf.Clamp01(hp.mean / 120f));
                gr.growthStage = 0;
                gr.ApplyStageScale(); // visual.radius を更新
            }
        }

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

    public void ClearAll()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
            if (agents[i] != null) Destroy(agents[i].gameObject);
        agents.Clear();
    }
}
