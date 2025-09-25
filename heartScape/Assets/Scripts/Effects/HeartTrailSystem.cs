using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 心オブジェクトの軌跡システム - 液体が溶けて離散するような軌跡を描画
/// </summary>
public class HeartTrailSystem : MonoBehaviour
{
    [Header("軌跡設定")]
    [Tooltip("軌跡パーティクルのプレハブ")]
    public GameObject trailParticlePrefab;
    
    [Tooltip("軌跡の生成間隔（秒）")]
    public float spawnInterval = 0.1f;
    
    [Tooltip("軌跡パーティクルの最大数")]
    public int maxTrailParticles = 200;
    
    [Header("軌跡の外観")]
    [Tooltip("軌跡の基本色")]
    public Color baseTrailColor = new Color(0.3f, 0.7f, 1f, 0.8f);
    
    [Tooltip("軌跡のサイズ範囲")]
    public Vector2 trailSizeRange = new Vector2(0.1f, 0.3f);
    
    [Tooltip("軌跡の生存時間")]
    public float trailLifetime = 2f;
    
    [Header("軌跡色調整")]
    [Tooltip("軌跡の明度調整")]
    [Range(0.0f, 2.0f)]
    public float trailBrightness = 1.0f;
    
    [Tooltip("軌跡の彩度調整")]
    [Range(0.0f, 2.0f)]
    public float trailSaturation = 1.0f;
    
    [Tooltip("軌跡の色相調整")]
    [Range(-1.0f, 1.0f)]
    public float trailHueShift = 0.0f;
    
    [Tooltip("軌跡の透明度調整")]
    [Range(0.0f, 1.0f)]
    public float trailAlpha = 1.0f;
    
    [Tooltip("軌跡色調整をリアルタイムで適用")]
    public bool applyTrailColorAdjustments = true;
    
    [Header("軌跡の縁取り効果")]
    [Tooltip("縁取りを有効にする")]
    public bool enableOutline = true;
    
    [Tooltip("縁取りの色")]
    public Color outlineColor = Color.white;
    
    [Tooltip("縁取りの幅（0-0.1）")]
    [Range(0f, 0.1f)]
    public float outlineWidth = 0.02f;
    
    [Tooltip("縁取りの明るさ（0-3）")]
    [Range(0f, 3f)]
    public float outlineBrightness = 1.5f;
    
    [Tooltip("縁取りのソフトネス（0-1）")]
    [Range(0f, 1f)]
    public float outlineSoftness = 0.5f;
    
    [Header("液体効果")]
    [Tooltip("拡散の強さ")]
    public float diffusionStrength = 1f;
    
    [Tooltip("離散化の速度")]
    public float discretizationSpeed = 2f;
    
    [Tooltip("重力効果")]
    public float gravityEffect = -0.5f;
    
    [Header("パフォーマンス")]
    [Tooltip("軌跡を生成する最小移動距離")]
    public float minMovementDistance = 0.1f;
    
    [Tooltip("軌跡を生成する最小速度")]
    public float minMovementSpeed = 0.5f;
    
    [Header("レンダリング")]
    [Tooltip("軌跡のレンダリング順序（心オブジェクトより後ろに描画）")]
    public int trailSortingOrder = -10;
    
    private HeartManager heartManager;
    private Dictionary<HeartAgent, TrailData> agentTrails = new Dictionary<HeartAgent, TrailData>();
    private Queue<TrailParticle> particlePool = new Queue<TrailParticle>();
    private List<TrailParticle> activeParticles = new List<TrailParticle>();
    
    /// <summary>
    /// アクティブなパーティクル数（デバッグ用）
    /// </summary>
    public int ActiveParticleCount => activeParticles.Count;
    
    /// <summary>
    /// 登録されているエージェント数（デバッグ用）
    /// </summary>
    public int RegisteredAgentCount => agentTrails.Count;
    
    /// <summary>
    /// 各心エージェントの軌跡データ
    /// </summary>
    private class TrailData
    {
        public Vector3 lastPosition;
        public float lastSpawnTime;
        public Color agentColor;
        public float agentSize;
    }
    
    void Awake()
    {
        // パーティクルプールを初期化
        InitializeParticlePool();
    }
    
    void Start()
    {
        heartManager = FindFirstObjectByType<HeartManager>();
        if (heartManager == null)
        {
            Debug.LogError("[HeartTrailSystem] HeartManagerが見つかりません。");
            enabled = false;
            return;
        }
        
        // 既存の心エージェントを登録
        foreach (var agent in heartManager.agents)
        {
            if (agent != null)
            {
                RegisterAgent(agent);
            }
        }
    }
    
    void Update()
    {
        UpdateAgentTrails();
        UpdateActiveParticles();
        CleanupInactiveAgents();
        
        // メモリリークを防ぐための定期的なクリーンアップ（頻度を大幅に下げる）
        if (Time.frameCount % 600 == 0) // 10秒ごと
        {
            CleanupMemoryLeaks();
        }
        
        // 積極的なメモリクリーンアップは削除（固まりの原因）
    }
    
    // ForceGarbageCollectionとStagedGCメソッドは削除（固まりの原因）
    
    void OnDestroy()
    {
        // パーティクルプールをクリーンアップ
        CleanupExistingPool();
    }
    
    /// <summary>
    /// パーティクルプールを初期化
    /// </summary>
    void InitializeParticlePool()
    {
        if (trailParticlePrefab == null)
        {
            Debug.LogError("[HeartTrailSystem] trailParticlePrefabが設定されていません。");
            return;
        }
        
        // 既存のプールをクリーンアップ
        CleanupExistingPool();
        
        // プールコンテナを作成
        GameObject poolContainer = new GameObject("TrailParticlePool");
        poolContainer.transform.SetParent(transform);
        poolContainer.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
        
        for (int i = 0; i < maxTrailParticles; i++)
        {
            GameObject particleObj = Instantiate(trailParticlePrefab, poolContainer.transform);
            particleObj.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
            
            TrailParticle particle = particleObj.GetComponent<TrailParticle>();
            if (particle == null)
            {
                particle = particleObj.AddComponent<TrailParticle>();
            }
            
            particleObj.SetActive(false);
            particlePool.Enqueue(particle);
        }
    }
    
    /// <summary>
    /// 既存のパーティクルプールをクリーンアップ
    /// </summary>
    void CleanupExistingPool()
    {
        // 既存のプールコンテナを探して削除
        Transform existingPool = transform.Find("TrailParticlePool");
        if (existingPool != null)
        {
            // アクティブなパーティクルをクリア
            activeParticles.Clear();
            particlePool.Clear();
            
            // 既存のプールを削除
            if (Application.isPlaying)
            {
                Destroy(existingPool.gameObject);
            }
            else
            {
                DestroyImmediate(existingPool.gameObject);
            }
        }
    }
    
    /// <summary>
    /// メモリリークを防ぐためのクリーンアップ（軽量化）
    /// </summary>
    void CleanupMemoryLeaks()
    {
        // null参照を削除（軽量化）
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            if (activeParticles[i] == null)
            {
                activeParticles.RemoveAt(i);
            }
        }
        
        // 古いエージェント参照をクリーンアップ（軽量化）
        var agentsToRemove = new List<HeartAgent>();
        foreach (var kvp in agentTrails)
        {
            if (kvp.Key == null)
            {
                agentsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var agent in agentsToRemove)
        {
            agentTrails.Remove(agent);
        }
        
        // 重いパーティクルシステムのクリーンアップは削除（固まりの原因）
    }
    
    /// <summary>
    /// 心エージェントを軌跡システムに登録
    /// </summary>
    public void RegisterAgent(HeartAgent agent)
    {
        if (agent == null || agentTrails.ContainsKey(agent)) return;
        
        TrailData trailData = new TrailData
        {
            lastPosition = agent.transform.position,
            lastSpawnTime = 0f,
            agentColor = GetAgentColor(agent),
            agentSize = GetAgentSize(agent)
        };
        
        agentTrails[agent] = trailData;
    }
    
    /// <summary>
    /// 心エージェントの軌跡システムから登録解除
    /// </summary>
    public void UnregisterAgent(HeartAgent agent)
    {
        agentTrails.Remove(agent);
    }
    
    /// <summary>
    /// 各エージェントの軌跡を更新
    /// </summary>
    void UpdateAgentTrails()
    {
        foreach (var kvp in agentTrails)
        {
            HeartAgent agent = kvp.Key;
            TrailData trailData = kvp.Value;
            
            if (agent == null || !agent.gameObject.activeInHierarchy) continue;
            
            Vector3 currentPos = agent.transform.position;
            float distanceMoved = Vector3.Distance(currentPos, trailData.lastPosition);
            
            // 移動距離と時間をチェック
            if (distanceMoved >= minMovementDistance && 
                Time.time - trailData.lastSpawnTime >= spawnInterval)
            {
                // 速度チェック
                Rigidbody2D rb = agent.GetComponent<Rigidbody2D>();
                if (rb != null && rb.linearVelocity.magnitude >= minMovementSpeed)
                {
                    SpawnTrailParticle(trailData.lastPosition, currentPos, trailData);
                    trailData.lastSpawnTime = Time.time;
                }
                
                trailData.lastPosition = currentPos;
            }
        }
    }
    
    /// <summary>
    /// 軌跡パーティクルを生成
    /// </summary>
    void SpawnTrailParticle(Vector3 startPos, Vector3 endPos, TrailData trailData)
    {
        if (particlePool.Count == 0) return;
        
        TrailParticle particle = particlePool.Dequeue();
        activeParticles.Add(particle);
        
        // パーティクルの初期設定
        Vector3 spawnPos = Vector3.Lerp(startPos, endPos, 0.5f);
        particle.transform.position = spawnPos;
        particle.gameObject.SetActive(true);
        
        // レンダリング順序を設定
        SpriteRenderer particleRenderer = particle.GetComponent<SpriteRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.sortingOrder = trailSortingOrder;
        }
        
        // 軌跡の方向から初期速度を計算
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
        
        // 液体の拡散効果
        Vector3 diffusionVelocity = perpendicular * Random.Range(-diffusionStrength, diffusionStrength);
        diffusionVelocity += Vector3.up * gravityEffect;
        
        // パーティクルを初期化
        float size = Random.Range(trailSizeRange.x, trailSizeRange.y) * trailData.agentSize;
        particle.Initialize(
            trailData.agentColor,
            size,
            trailLifetime,
            diffusionVelocity,
            discretizationSpeed
        );
        
        // 縁取りシェーダーを適用
        if (enableOutline)
        {
            ApplyOutlineToParticle(particle);
        }
    }
    
    /// <summary>
    /// アクティブなパーティクルを更新
    /// </summary>
    void UpdateActiveParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            TrailParticle particle = activeParticles[i];
            
            if (particle == null || !particle.gameObject.activeInHierarchy)
            {
                // パーティクルをプールに戻す
                if (particle != null)
                {
                    // パーティクルをリセットしてからプールに戻す
                    particle.ResetParticle();
                    particle.gameObject.SetActive(false);
                    particlePool.Enqueue(particle);
                }
                activeParticles.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 非アクティブなエージェントをクリーンアップ
    /// </summary>
    void CleanupInactiveAgents()
    {
        var agentsToRemove = new List<HeartAgent>();
        
        foreach (var kvp in agentTrails)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
            {
                agentsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var agent in agentsToRemove)
        {
            UnregisterAgent(agent);
        }
    }
    
    /// <summary>
    /// エージェントの色を取得
    /// </summary>
    Color GetAgentColor(HeartAgent agent)
    {
        HeartVisual visual = agent.GetComponent<HeartVisual>();
        if (visual != null)
        {
            // HeartVisualから直接色を取得
            if (visual.color != Color.clear)
            {
                Color agentColor = visual.color;
                
                // 軌跡色調整を適用
                if (applyTrailColorAdjustments)
                {
                    agentColor = ApplyTrailColorAdjustments(agentColor);
                }
                
                return agentColor;
            }
            
            // マテリアルから色を取得（プロパティが存在する場合のみ）
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color materialColor = Color.white;
                
                // マテリアルに_Colorプロパティがあるかチェック
                if (renderer.material.HasProperty("_Color"))
                {
                    materialColor = renderer.material.color;
                }
                else if (renderer.material.HasProperty("_MainColor"))
                {
                    materialColor = renderer.material.GetColor("_MainColor");
                }
                else if (renderer.material.HasProperty("_BaseColor"))
                {
                    materialColor = renderer.material.GetColor("_BaseColor");
                }
                
                // 軌跡色調整を適用
                if (applyTrailColorAdjustments)
                {
                    materialColor = ApplyTrailColorAdjustments(materialColor);
                }
                
                return materialColor;
            }
        }
        
        // フォールバック色（軌跡色調整を適用）
        if (applyTrailColorAdjustments)
        {
            return ApplyTrailColorAdjustments(baseTrailColor);
        }
        
        return baseTrailColor;
    }
    
    /// <summary>
    /// 軌跡色調整を適用
    /// </summary>
    Color ApplyTrailColorAdjustments(Color originalColor)
    {
        // RGBからHSVに変換
        float h, s, v;
        Color.RGBToHSV(originalColor, out h, out s, out v);
        
        // 色相調整
        h = (h + trailHueShift) % 1.0f;
        if (h < 0) h += 1.0f;
        
        // 彩度調整
        s = Mathf.Clamp01(s * trailSaturation);
        
        // 明度調整
        v = Mathf.Clamp01(v * trailBrightness);
        
        // HSVからRGBに変換
        Color adjustedColor = Color.HSVToRGB(h, s, v);
        
        // 透明度調整
        adjustedColor.a = Mathf.Clamp01(originalColor.a * trailAlpha);
        
        return adjustedColor;
    }
    
    /// <summary>
    /// エージェントのサイズを取得
    /// </summary>
    float GetAgentSize(HeartAgent agent)
    {
        HeartVisual visual = agent.GetComponent<HeartVisual>();
        if (visual != null)
        {
            return visual.transform.localScale.x;
        }
        
        return 1f;
    }
    
    /// <summary>
    /// 新しいエージェントが生成されたときの通知用
    /// </summary>
    public void OnAgentSpawned(HeartAgent agent)
    {
        RegisterAgent(agent);
    }
    
    /// <summary>
    /// 外部から軌跡色調整を設定
    /// </summary>
    public void SetTrailColorAdjustments(float brightness, float saturation, float hueShift, float alpha = 1.0f)
    {
        trailBrightness = brightness;
        trailSaturation = saturation;
        trailHueShift = hueShift;
        trailAlpha = alpha;
    }
    
    /// <summary>
    /// 軌跡色調整をリセット
    /// </summary>
    public void ResetTrailColorAdjustments()
    {
        trailBrightness = 1.0f;
        trailSaturation = 1.0f;
        trailHueShift = 0.0f;
        trailAlpha = 1.0f;
    }
    
    /// <summary>
    /// 軌跡システムをリセット
    /// </summary>
    public void ResetTrailSystem()
    {
        // アクティブなパーティクルを無効化
        foreach (var particle in activeParticles)
        {
            if (particle != null)
            {
                particle.gameObject.SetActive(false);
            }
        }
        activeParticles.Clear();
        
        // エージェント軌跡をクリア
        agentTrails.Clear();
        
        // パーティクルプールを再初期化
        CleanupExistingPool();
        InitializeParticlePool();
    }
    
    /// <summary>
    /// エージェントが破棄されるときの通知用
    /// </summary>
    public void OnAgentDestroyed(HeartAgent agent)
    {
        UnregisterAgent(agent);
    }
    
    /// <summary>
    /// パーティクルに縁取りシェーダーを適用
    /// </summary>
    void ApplyOutlineToParticle(TrailParticle particle)
    {
        if (particle == null) return;
        
        // パーティクルのレンダラーを取得
        var renderer = particle.GetComponent<Renderer>();
        if (renderer == null) return;
        
        // 縁取りシェーダーを取得
        Shader outlineShader = Shader.Find("Custom/ParticleOutline");
        if (outlineShader == null)
        {
            Debug.LogWarning("[HeartTrailSystem] ParticleOutlineシェーダーが見つかりません。");
            return;
        }
        
        // 縁取りマテリアルを作成
        Material outlineMaterial = new Material(outlineShader);
        outlineMaterial.name = $"OutlineMaterial_{particle.GetInstanceID()}";
        outlineMaterial.hideFlags = HideFlags.DontSaveInEditor;
        
        // 縁取りパラメータを設定
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetFloat("_OutlineBrightness", outlineBrightness);
        outlineMaterial.SetFloat("_Softness", outlineSoftness);
        
        // マテリアルを適用
        renderer.material = outlineMaterial;
    }
}
