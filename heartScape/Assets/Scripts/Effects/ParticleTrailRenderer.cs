using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// パーティクル軌跡専用レンダラー
/// パーティクルシステムと独立した軌跡管理を行い、美しい軌跡描画を実現
/// </summary>
public class ParticleTrailRenderer : MonoBehaviour
{
    [Header("軌跡基本設定")]
    [Tooltip("軌跡の最大数")]
    [Range(5, 100)]
    public int maxTrailCount = 50;
    
    [Tooltip("軌跡の長さ（点の数）")]
    [Range(5, 50)]
    public int trailLength = 20;
    
    [Tooltip("軌跡の幅")]
    [Range(0.01f, 0.5f)]
    public float trailWidth = 0.05f;
    
    [Tooltip("軌跡の更新間隔（秒）")]
    [Range(0.01f, 0.1f)]
    public float updateInterval = 0.02f;
    
    [Header("軌跡の見た目")]
    [Tooltip("軌跡の明度調整（0-3）")]
    [Range(0f, 3f)]
    public float trailBrightness = 1.2f;
    
    [Tooltip("軌跡の彩度調整（0-2）")]
    [Range(0f, 2f)]
    public float trailSaturation = 1.1f;
    
    [Tooltip("軌跡の透明度調整（0-1）")]
    [Range(0f, 1f)]
    public float trailAlpha = 0.9f;
    
    [Tooltip("軌跡のフェードアウト速度")]
    [Range(0.5f, 5f)]
    public float fadeOutSpeed = 2f;
    
    [Header("軌跡の動き")]
    [Tooltip("軌跡の滑らかさ（0-1）")]
    [Range(0f, 1f)]
    public float trailSmoothness = 0.8f;
    
    [Tooltip("軌跡の最小移動距離")]
    [Range(0.001f, 0.1f)]
    public float minMoveDistance = 0.01f;
    
    // 軌跡データ構造
    [System.Serializable]
    public class TrailData
    {
        public GameObject gameObject;
        public LineRenderer lineRenderer;
        public List<Vector3> positions = new List<Vector3>();
        public List<Color> colors = new List<Color>();
        public float lifetime;
        public float maxLifetime;
        public bool isActive;
        public bool isFollowingParticle;
        public int particleId = -1; // パーティクルのユニークID
        public Vector3 lastPosition;
        public Color lastColor;
        public float fadeProgress = 0f; // フェードアウト進行度
    }
    
    // 内部変数
    private List<TrailData> activeTrails = new List<TrailData>();
    private Queue<TrailData> trailPool = new Queue<TrailData>();
    private float lastUpdateTime;
    private int nextTrailId = 0;
    
    // パーティクル追跡用
    private Dictionary<int, TrailData> particleToTrailMap = new Dictionary<int, TrailData>();
    
    void Awake()
    {
        // 軌跡プールを初期化
        InitializeTrailPool();
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateAllTrails();
            lastUpdateTime = Time.time;
        }
    }
    
    void OnDestroy()
    {
        // 全軌跡をクリーンアップ
        CleanupAllTrails();
    }
    
    /// <summary>
    /// 軌跡プールを初期化
    /// </summary>
    void InitializeTrailPool()
    {
        for (int i = 0; i < maxTrailCount; i++)
        {
            TrailData trail = CreateTrailData();
            trail.gameObject.SetActive(false);
            trailPool.Enqueue(trail);
        }
    }
    
    /// <summary>
    /// 軌跡データを作成
    /// </summary>
    TrailData CreateTrailData()
    {
        GameObject trailObj = new GameObject($"Trail_{nextTrailId++}");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;
        
        LineRenderer lineRenderer = trailObj.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer);
        
        return new TrailData
        {
            gameObject = trailObj,
            lineRenderer = lineRenderer,
            positions = new List<Vector3>(),
            colors = new List<Color>(),
            lifetime = 0f,
            maxLifetime = 3f,
            isActive = false,
            isFollowingParticle = false,
            particleId = -1,
            lastPosition = Vector3.zero,
            lastColor = Color.white,
            fadeProgress = 0f
        };
    }
    
    /// <summary>
    /// LineRendererを設定
    /// </summary>
    void SetupLineRenderer(LineRenderer lr)
    {
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.name = $"TrailMaterial_{GetInstanceID()}";
        lr.material.hideFlags = HideFlags.DontSaveInEditor;
        lr.material.SetFloat("_Mode", 3); // 透明度ブレンドモード
        
        lr.startWidth = trailWidth;
        lr.endWidth = trailWidth;
        lr.useWorldSpace = true;
        lr.sortingOrder = 10;
        lr.sortingLayerName = "Default";
        lr.enabled = false;
    }
    
    /// <summary>
    /// 全軌跡を更新
    /// </summary>
    void UpdateAllTrails()
    {
        // アクティブな軌跡を更新
        for (int i = activeTrails.Count - 1; i >= 0; i--)
        {
            TrailData trail = activeTrails[i];
            if (trail == null || !trail.isActive)
            {
                RemoveTrail(trail);
                continue;
            }
            
            // 軌跡の生存時間を更新
            trail.lifetime += Time.deltaTime;
            
            if (trail.isFollowingParticle)
            {
                // パーティクル追従中：通常の更新
                UpdateTrailFollowing(trail);
            }
            else
            {
                // フェードアウト中
                UpdateTrailFadeOut(trail);
            }
            
            // 軌跡が完全に消滅したかチェック
            if (trail.lifetime >= trail.maxLifetime || trail.fadeProgress >= 1f)
            {
                RemoveTrail(trail);
            }
        }
    }
    
    /// <summary>
    /// パーティクル追従中の軌跡を更新
    /// </summary>
    void UpdateTrailFollowing(TrailData trail)
    {
        // 軌跡の位置と色を更新（外部から呼び出される）
        // この部分は UpdateTrailPosition メソッドで処理
    }
    
    /// <summary>
    /// フェードアウト中の軌跡を更新
    /// </summary>
    void UpdateTrailFadeOut(TrailData trail)
    {
        if (trail.lineRenderer == null) return;
        
        // フェードアウト進行度を更新
        trail.fadeProgress += Time.deltaTime * fadeOutSpeed;
        trail.fadeProgress = Mathf.Clamp01(trail.fadeProgress);
        
        // アルファ値を段階的に減少
        float fadeAlpha = Mathf.Lerp(1f, 0f, trail.fadeProgress);
        fadeAlpha = Mathf.Pow(fadeAlpha, 2f); // より自然なフェードアウト
        
        // 軌跡の色を更新
        UpdateTrailColors(trail, fadeAlpha);
        
        // 軌跡の位置を滑らかに更新
        if (trail.positions.Count > 1)
        {
            UpdateTrailPositions(trail);
        }
    }
    
    /// <summary>
    /// 軌跡の位置を更新
    /// </summary>
    void UpdateTrailPositions(TrailData trail)
    {
        if (trail.lineRenderer == null || trail.positions.Count < 2) return;
        
        trail.lineRenderer.positionCount = trail.positions.Count;
        
        for (int i = 0; i < trail.positions.Count; i++)
        {
            trail.lineRenderer.SetPosition(i, trail.positions[i]);
        }
    }
    
    /// <summary>
    /// 軌跡の色を更新
    /// </summary>
    void UpdateTrailColors(TrailData trail, float alphaMultiplier = 1f)
    {
        if (trail.lineRenderer == null) return;
        
        if (trail.colors.Count >= 2)
        {
            // グラデーション色を適用
            Color startColor = trail.colors[0];
            Color endColor = trail.colors[trail.colors.Count - 1];
            
            startColor.a *= alphaMultiplier;
            endColor.a *= alphaMultiplier * 0.3f; // 後端は薄く
            
            trail.lineRenderer.startColor = startColor;
            trail.lineRenderer.endColor = endColor;
        }
        else if (trail.colors.Count == 1)
        {
            Color color = trail.colors[0];
            color.a *= alphaMultiplier;
            trail.lineRenderer.startColor = color;
            trail.lineRenderer.endColor = color;
        }
    }
    
    /// <summary>
    /// 軌跡の位置を更新（外部から呼び出し）
    /// </summary>
    public void UpdateTrailPosition(int particleId, Vector3 position, Color color)
    {
        UpdateTrailPositionInternal(particleId, position, color);
    }
    
    /// <summary>
    /// SendMessage用の軌跡位置更新メソッド（単一パラメータ）
    /// </summary>
    public void UpdateTrailPositionInternal(object[] parameters)
    {
        if (parameters != null && parameters.Length >= 3)
        {
            int particleId = (int)parameters[0];
            Vector3 position = (Vector3)parameters[1];
            Color color = (Color)parameters[2];
            UpdateTrailPositionInternal(particleId, position, color);
        }
    }
    
    /// <summary>
    /// 内部軌跡位置更新メソッド
    /// </summary>
    private void UpdateTrailPositionInternal(int particleId, Vector3 position, Color color)
    {
        if (particleToTrailMap.TryGetValue(particleId, out TrailData trail))
        {
            if (trail.isFollowingParticle)
            {
                // 移動距離をチェック
                float moveDistance = Vector3.Distance(position, trail.lastPosition);
                if (moveDistance < minMoveDistance) return;
                
                // 新しい位置を追加
                trail.positions.Add(position);
                trail.colors.Add(ApplyTrailColorAdjustments(color));
                
                // 軌跡の長さを制限
                if (trail.positions.Count > trailLength)
                {
                    trail.positions.RemoveAt(0);
                    trail.colors.RemoveAt(0);
                }
                
                // 軌跡を更新
                UpdateTrailPositions(trail);
                UpdateTrailColors(trail);
                
                // 最後の位置と色を記録
                trail.lastPosition = position;
                trail.lastColor = color;
            }
        }
    }
    
    /// <summary>
    /// 新しい軌跡を開始
    /// </summary>
    public void StartTrail(int particleId, Vector3 position, Color color)
    {
        StartTrailInternal(particleId, position, color);
    }
    
    /// <summary>
    /// SendMessage用の軌跡開始メソッド（単一パラメータ）
    /// </summary>
    public void StartTrailInternal(object[] parameters)
    {
        if (parameters != null && parameters.Length >= 3)
        {
            int particleId = (int)parameters[0];
            Vector3 position = (Vector3)parameters[1];
            Color color = (Color)parameters[2];
            StartTrailInternal(particleId, position, color);
        }
    }
    
    /// <summary>
    /// 内部軌跡開始メソッド
    /// </summary>
    private void StartTrailInternal(int particleId, Vector3 position, Color color)
    {
        if (particleToTrailMap.ContainsKey(particleId)) return;
        
        // プールから軌跡を取得
        TrailData trail = GetTrailFromPool();
        if (trail == null) return;
        
        // 軌跡を初期化
        trail.gameObject.SetActive(true);
        trail.lineRenderer.enabled = true;
        trail.isActive = true;
        trail.isFollowingParticle = true;
        trail.particleId = particleId;
        trail.lifetime = 0f;
        trail.fadeProgress = 0f;
        
        // 初期位置と色を設定
        trail.positions.Clear();
        trail.colors.Clear();
        trail.positions.Add(position);
        trail.colors.Add(ApplyTrailColorAdjustments(color));
        trail.lastPosition = position;
        trail.lastColor = color;
        
        // 軌跡を描画
        UpdateTrailPositions(trail);
        UpdateTrailColors(trail);
        
        // マップに追加
        particleToTrailMap[particleId] = trail;
        activeTrails.Add(trail);
    }
    
    /// <summary>
    /// 軌跡の追従を停止
    /// </summary>
    public void StopTrailFollowing(int particleId)
    {
        if (particleToTrailMap.TryGetValue(particleId, out TrailData trail))
        {
            trail.isFollowingParticle = false;
            trail.fadeProgress = 0f; // フェードアウト開始
            particleToTrailMap.Remove(particleId);
        }
    }
    
    /// <summary>
    /// 軌跡を削除
    /// </summary>
    void RemoveTrail(TrailData trail)
    {
        if (trail == null) return;
        
        // アクティブリストから削除
        activeTrails.Remove(trail);
        
        // パーティクルマップから削除
        if (trail.particleId >= 0)
        {
            particleToTrailMap.Remove(trail.particleId);
        }
        
        // 軌跡をプールに戻す
        ReturnTrailToPool(trail);
    }
    
    /// <summary>
    /// プールから軌跡を取得
    /// </summary>
    TrailData GetTrailFromPool()
    {
        if (trailPool.Count > 0)
        {
            return trailPool.Dequeue();
        }
        
        // プールが空の場合は新しい軌跡を作成
        return CreateTrailData();
    }
    
    /// <summary>
    /// 軌跡をプールに戻す
    /// </summary>
    void ReturnTrailToPool(TrailData trail)
    {
        if (trail == null) return;
        
        // 軌跡をリセット
        trail.gameObject.SetActive(false);
        trail.lineRenderer.enabled = false;
        trail.isActive = false;
        trail.isFollowingParticle = false;
        trail.particleId = -1;
        trail.lifetime = 0f;
        trail.fadeProgress = 0f;
        trail.positions.Clear();
        trail.colors.Clear();
        
        // プールに戻す
        trailPool.Enqueue(trail);
    }
    
    /// <summary>
    /// 軌跡の色調整を適用
    /// </summary>
    Color ApplyTrailColorAdjustments(Color originalColor)
    {
        // HSVに変換
        Color.RGBToHSV(originalColor, out float h, out float s, out float v);
        
        // 明度を調整
        v *= trailBrightness;
        v = Mathf.Clamp01(v);
        
        // 彩度を調整
        s *= trailSaturation;
        s = Mathf.Clamp01(s);
        
        // 透明度を調整
        float alpha = originalColor.a * trailAlpha;
        alpha = Mathf.Clamp01(alpha);
        
        // RGBに戻す
        Color adjustedColor = Color.HSVToRGB(h, s, v);
        adjustedColor.a = alpha;
        
        return adjustedColor;
    }
    
    /// <summary>
    /// 全軌跡をクリーンアップ
    /// </summary>
    void CleanupAllTrails()
    {
        // アクティブな軌跡を全て削除
        foreach (var trail in activeTrails)
        {
            if (trail != null && trail.gameObject != null)
            {
                DestroyImmediate(trail.gameObject);
            }
        }
        activeTrails.Clear();
        particleToTrailMap.Clear();
        
        // プールの軌跡も削除
        while (trailPool.Count > 0)
        {
            TrailData trail = trailPool.Dequeue();
            if (trail != null && trail.gameObject != null)
            {
                DestroyImmediate(trail.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 軌跡の統計情報を取得
    /// </summary>
    public string GetTrailStats()
    {
        return $"Active Trails: {activeTrails.Count}, Pool: {trailPool.Count}, Mapped: {particleToTrailMap.Count}";
    }
}