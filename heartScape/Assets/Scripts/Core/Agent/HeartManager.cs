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
    
    [Header("軌跡システム")]
    [Tooltip("軌跡システムへの参照")]
    public HeartTrailSystem trailSystem;
    
    [Header("心オブジェクト色調整")]
    [Tooltip("全心オブジェクトの明度調整")]
    [Range(0.0f, 2.0f)]
    public float globalBrightness = 1.0f;
    
    [Tooltip("全心オブジェクトの彩度調整")]
    [Range(0.0f, 2.0f)]
    public float globalSaturation = 1.0f;
    
    [Tooltip("全心オブジェクトの色相調整")]
    [Range(-1.0f, 1.0f)]
    public float globalHueShift = 0.0f;
    
    [Tooltip("全心オブジェクトの透明度調整")]
    [Range(0.0f, 1.0f)]
    public float globalAlpha = 1.0f;
    
    [Tooltip("色調整をリアルタイムで適用")]
    public bool applyColorAdjustments = true;

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
        
        // 色調整をリアルタイムで適用
        if (applyColorAdjustments)
        {
            ApplyGlobalColorAdjustments();
        }
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
        
        // 軌跡システムにエージェントを登録
        if (trailSystem != null)
        {
            trailSystem.OnAgentSpawned(a);
        }
        
        HeartSoundManager.Instance?.PlaySpawn(pos);

        return a;
    }
    
    /// <summary>
    /// 全心オブジェクトに色調整を適用
    /// </summary>
    void ApplyGlobalColorAdjustments()
    {
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            
            HeartVisual visual = agent.GetComponent<HeartVisual>();
            if (visual != null)
            {
                // グローバル色調整を適用
                visual.brightnessMultiplier = globalBrightness;
                visual.saturationMultiplier = globalSaturation;
                visual.hueShift = globalHueShift;
                visual.alphaMultiplier = globalAlpha;
            }
        }
    }
    
    /// <summary>
    /// 外部から色調整を設定
    /// </summary>
    public void SetGlobalColorAdjustments(float brightness, float saturation, float hueShift, float alpha = 1.0f)
    {
        globalBrightness = brightness;
        globalSaturation = saturation;
        globalHueShift = hueShift;
        globalAlpha = alpha;
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
                // 軌跡システムからエージェントを登録解除
                if (trailSystem != null)
                {
                    trailSystem.OnAgentDestroyed(a);
                }
                
                Destroy(a.gameObject);
                agents.RemoveAt(i);
            }
        }
    }

    public void ClearAll()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            if (agents[i] != null)
            {
                // 軌跡システムからエージェントを登録解除
                if (trailSystem != null)
                {
                    trailSystem.OnAgentDestroyed(agents[i]);
                }
                Destroy(agents[i].gameObject);
            }
        }
        agents.Clear();
    }
}
