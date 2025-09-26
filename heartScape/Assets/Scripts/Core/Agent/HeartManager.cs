using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    [Header("Appearance")]
    public HeartAppearanceMapper appearance;
    public GameObject agentPrefab;

    [Header("Limits")]
    [Min(1)] public int maxAgents = 140; // hard cap to avoid runaway instantiation
    [Tooltip("Maximum active agents kept in the scene. Oldest ones shrink away when exceeded.")]
    public int maxActiveAgents = 30;
    [Tooltip("Maximum simultaneous agents that share the same logical id.")]
    public int maxAgentsPerId = 5;
    [Tooltip("Duration used when shrinking and removing excess agents.")]
    public float removalShrinkDuration = 0.35f;

    [Header("Offscreen Despawn")]
    public bool despawnOffscreen = false;
    public Vector2 despawnExtents = new Vector2(10f, 6f); // ±X, ±Y

    [Header("Trail System")]
    [Tooltip("Reference to the trail system controller.")]
    public HeartTrailSystem trailSystem;

    [Header("Global Color Adjustments")]
    [Range(0.0f, 2.0f)] public float globalBrightness = 1.0f;
    [Range(0.0f, 2.0f)] public float globalSaturation = 1.0f;
    [Range(-1.0f, 1.0f)] public float globalHueShift = 0.0f;
    [Range(0.0f, 1.0f)] public float globalAlpha = 1.0f;
    public bool applyColorAdjustments = true;

    public List<HeartAgent> agents = new();

    readonly Dictionary<int, Queue<HeartAgent>> agentsById = new();
    readonly LinkedList<HeartAgent> spawnOrder = new();
    readonly Dictionary<HeartAgent, Coroutine> removalRoutines = new();

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
        CleanupSpawnOrder();

        if (despawnOffscreen)
        {
            DespawnOffscreen();
        }

        if (applyColorAdjustments)
        {
            ApplyGlobalColorAdjustments();
        }
    }

    public HeartAgent Spawn(HeartProfile hp, Vector2 pos, Vector2 vel)
    {
        if (agents.Count >= maxAgents || agentPrefab == null) return null;

        var go = Instantiate(agentPrefab, pos, Quaternion.identity);
        var agent = go.GetComponent<HeartAgent>();
        if (agent == null)
        {
            Debug.LogWarning("[HeartManager] Agent prefab is missing HeartAgent component.");
            Destroy(go);
            return null;
        }

        agent.owner = this;
        agent.profile = hp;

        if (hp != null && !string.IsNullOrEmpty(hp.uid))
        {
            agent.id = hp.uid.GetHashCode();
        }
        else if (agent.id == 0)
        {
            agent.id = Random.Range(int.MinValue, int.MaxValue);
        }

        var visual = go.GetComponent<HeartVisual>();
        if (visual != null)
        {
            if (appearance != null)
            {
                appearance.Apply(hp, visual);
            }
            else
            {
                visual.color = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.75f, 1f);
            }
        }

        var growth = go.GetComponent<HeartGrowth>();
        if (growth != null)
        {
            bool mapperSetsRadius = (appearance != null && appearance.setRadiusOnSpawn);
            if (!mapperSetsRadius)
            {
                growth.baseRadius  = Mathf.Lerp(0.45f, 0.7f, Mathf.Clamp01(hp.mean / 120f));
                growth.growthStage = 0;
                growth.ApplyStageScale();
            }
        }

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = vel;
        }

        agents.Add(agent);
        RegisterAgent(agent);

        if (trailSystem != null)
        {
            trailSystem.OnAgentSpawned(agent);
        }

        HeartSoundManager.Instance?.PlaySpawn(pos);

        return agent;
    }

    internal void NotifyAgentDestroyed(HeartAgent agent)
    {
        if (agent == null) return;

        agents.Remove(agent);
        spawnOrder.Remove(agent);
        removalRoutines.Remove(agent);
        RemoveFromIdQueue(agent);

        if (trailSystem != null)
        {
            trailSystem.OnAgentDestroyed(agent);
        }
    }

    void RegisterAgent(HeartAgent agent)
    {
        if (agent == null) return;

        spawnOrder.AddLast(agent);

        if (!agentsById.TryGetValue(agent.id, out var queue))
        {
            queue = new Queue<HeartAgent>();
            agentsById[agent.id] = queue;
        }
        queue.Enqueue(agent);

        EnforcePerIdLimit(agent.id);
        EnforceGlobalLimit();
    }

    void EnforcePerIdLimit(int id)
    {
        if (maxAgentsPerId <= 0) return;
        if (!agentsById.TryGetValue(id, out var queue)) return;

        CleanupQueue(queue);
        while (queue.Count > maxAgentsPerId)
        {
            var oldest = queue.Dequeue();
            ScheduleRemoval(oldest);
        }
    }

    void EnforceGlobalLimit()
    {
        if (maxActiveAgents <= 0) return;

        CleanupSpawnOrder();
        while (spawnOrder.Count > maxActiveAgents)
        {
            var oldest = spawnOrder.First?.Value;
            spawnOrder.RemoveFirst();
            ScheduleRemoval(oldest);
        }
    }

    void ScheduleRemoval(HeartAgent agent)
    {
        if (agent == null) return;
        if (removalRoutines.ContainsKey(agent)) return;

        RemoveFromCollections(agent);

        var routine = StartCoroutine(ShrinkAndDestroy(agent, removalShrinkDuration));
        removalRoutines[agent] = routine;
    }

    IEnumerator ShrinkAndDestroy(HeartAgent agent, float duration)
    {
        if (agent == null) yield break;

        var visual = agent.GetComponent<HeartVisual>();
        var physics = agent.GetComponent<HeartPhysics>();
        float initialRadius = visual != null ? visual.radius : 0f;
        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        if (physics != null)
        {
            physics.enabled = false;
        }

        while (agent != null && timer < safeDuration)
        {
            float t = timer / safeDuration;
            if (visual != null)
            {
                float newRadius = Mathf.Lerp(initialRadius, 0f, t);
                visual.SetRadius(Mathf.Max(0.001f, newRadius));
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (agent != null)
        {
            Destroy(agent.gameObject);
        }

        removalRoutines.Remove(agent);
    }

    void RemoveFromCollections(HeartAgent agent)
    {
        agents.Remove(agent);
        spawnOrder.Remove(agent);
        RemoveFromIdQueue(agent);
    }

    void RemoveFromIdQueue(HeartAgent agent)
    {
        if (agent == null) return;
        if (!agentsById.TryGetValue(agent.id, out var queue)) return;
        if (queue.Count == 0) return;

        var temp = new Queue<HeartAgent>(queue.Count);
        while (queue.Count > 0)
        {
            var existing = queue.Dequeue();
            if (existing != null && existing != agent)
            {
                temp.Enqueue(existing);
            }
        }

        if (temp.Count > 0)
        {
            agentsById[agent.id] = temp;
        }
        else
        {
            agentsById.Remove(agent.id);
        }
    }

    void CleanupQueue(Queue<HeartAgent> queue)
    {
        if (queue == null || queue.Count == 0) return;

        int count = queue.Count;
        for (int i = 0; i < count; i++)
        {
            var agent = queue.Dequeue();
            if (agent != null)
            {
                queue.Enqueue(agent);
            }
        }
    }

    void CleanupSpawnOrder()
    {
        var node = spawnOrder.First;
        while (node != null)
        {
            var next = node.Next;
            if (node.Value == null)
            {
                spawnOrder.Remove(node);
            }
            node = next;
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

    void ApplyGlobalColorAdjustments()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var agent = agents[i];
            if (agent == null)
            {
                agents.RemoveAt(i);
                continue;
            }

            HeartVisual visual = agent.GetComponent<HeartVisual>();
            if (visual != null)
            {
                visual.brightnessMultiplier = globalBrightness;
                visual.saturationMultiplier = globalSaturation;
                visual.hueShift = globalHueShift;
                visual.alphaMultiplier = globalAlpha;
            }
        }
    }

    void DespawnOffscreen()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var agent = agents[i];
            if (agent == null)
            {
                agents.RemoveAt(i);
                continue;
            }

            Vector3 p = agent.transform.position;
            if (Mathf.Abs(p.x) > despawnExtents.x || Mathf.Abs(p.y) > despawnExtents.y)
            {
                DestroyAgentImmediate(agent);
            }
        }
    }

    void DestroyAgentImmediate(HeartAgent agent)
    {
        if (agent == null) return;

        if (removalRoutines.TryGetValue(agent, out var routine))
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
            removalRoutines.Remove(agent);
        }

        RemoveFromCollections(agent);

        Destroy(agent.gameObject);
    }

    public void ClearAll()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var agent = agents[i];
            if (agent != null)
            {
                DestroyAgentImmediate(agent);
            }
        }

        agents.Clear();
        spawnOrder.Clear();
        agentsById.Clear();
        removalRoutines.Clear();
    }
}
