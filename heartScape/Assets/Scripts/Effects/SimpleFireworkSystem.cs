using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// シンプルな花火システム
/// パーティクル数20個、liquidオブジェクトの色に基づく色変化、軌跡付き
/// </summary>
public class SimpleFireworkSystem : MonoBehaviour
{
    [Header("花火基本設定")]
    [Tooltip("パーティクル数")]
    [Range(5, 50)]
    public int particleCount = 20;
    
    [Tooltip("花火の速度")]
    [Range(1f, 20f)]
    public float speed = 8f;
    
    [Tooltip("花火の生存時間")]
    [Range(0.5f, 5f)]
    public float lifetime = 2f;
    
    [Tooltip("花火のサイズ")]
    [Range(0.01f, 0.2f)]
    public float particleSize = 0.05f;
    
    [Header("色設定")]
    [Tooltip("パーティクルの明度調整")]
    [Range(0f, 3f)]
    public float particleBrightness = 1.2f;
    
    [Tooltip("パーティクルの彩度調整")]
    [Range(0f, 2f)]
    public float particleSaturation = 1.1f;
    
    [Tooltip("パーティクルの透明度")]
    [Range(0f, 1f)]
    public float particleAlpha = 1f;
    
    [Header("軌跡設定")]
    [Tooltip("軌跡を有効にする")]
    public bool enableTrail = true;
    
    [Tooltip("軌跡の明度調整")]
    [Range(0f, 3f)]
    public float trailBrightness = 1.0f;
    
    [Tooltip("軌跡の彩度調整")]
    [Range(0f, 2f)]
    public float trailSaturation = 1.0f;
    
    [Tooltip("軌跡の透明度")]
    [Range(0f, 1f)]
    public float trailAlpha = 0.8f;
    
    [Tooltip("軌跡の長さ")]
    [Range(5, 30)]
    public int trailLength = 15;
    
    [Tooltip("軌跡の幅")]
    [Range(0.01f, 0.1f)]
    public float trailWidth = 0.03f;
    
    [Header("破裂タイミング")]
    [Tooltip("破裂までの遅延時間")]
    [Range(0f, 2f)]
    public float burstDelay = 0.1f;
    
    // 内部変数
    private List<FireworkParticle> particles = new List<FireworkParticle>();
    private List<Color> liquidColors = new List<Color>();
    private bool isActive = false;
    private bool hasCreatedParticles = false; // パーティクル生成済みフラグ
    
    // パーティクルデータ構造
    [System.Serializable]
    public class FireworkParticle
    {
        public GameObject gameObject;
        public LineRenderer trail;
        public Vector3 velocity;
        public Color color;
        public float lifetime;
        public float maxLifetime;
        public List<Vector3> trailPositions = new List<Vector3>();
        public bool isActive;
        public bool hasBurst; // 破裂したかどうか
        public float burstTimer; // 破裂タイマー
    }
    
    void Update()
    {
        if (isActive)
        {
            UpdateParticles();
        }
    }
    
    /// <summary>
    /// 花火を開始
    /// </summary>
    public void StartFirework(List<Color> colors)
    {
        if (colors == null || colors.Count == 0)
        {
            colors = new List<Color> { Color.white };
        }
        
        liquidColors = new List<Color>(colors);
        CreateParticles();
        isActive = true;
    }
    
    /// <summary>
    /// パーティクルを作成
    /// </summary>
    void CreateParticles()
    {
        // 既存のパーティクルをクリーンアップ
        ClearParticles();
        
        // 破裂タイミングでパーティクルを生成（遅延なし）
        for (int i = 0; i < particleCount; i++)
        {
            CreateParticle(i);
        }
        hasCreatedParticles = true;
    }
    
    /// <summary>
    /// 個別パーティクルを作成
    /// </summary>
    void CreateParticle(int index)
    {
        // パーティクルオブジェクトを作成
        GameObject particleObj = new GameObject($"FireworkParticle_{index}");
        particleObj.transform.SetParent(transform);
        particleObj.transform.position = transform.position; // BubblePopVFXの位置に設定
        
        // 破裂前は完全に非表示（ヒエラルキーにも表示されない）
        particleObj.SetActive(false);
        
        // スプライトレンダラーを追加
        SpriteRenderer spriteRenderer = particleObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateParticleSprite();
        spriteRenderer.sortingOrder = 10;
        
        // 軌跡を作成
        LineRenderer trail = null;
        if (enableTrail)
        {
            trail = CreateTrail(particleObj);
        }
        
        // ランダムな方向と速度
        Vector3 direction = Random.insideUnitSphere.normalized;
        direction.z = 0; // 2D平面に制限
        Vector3 velocity = direction * speed * Random.Range(0.7f, 1.3f);
        
        // 色を選択
        Color baseColor = liquidColors[Random.Range(0, liquidColors.Count)];
        Color particleColor = ApplyColorAdjustments(baseColor, particleBrightness, particleSaturation, particleAlpha);
        
        // パーティクルデータを作成
        FireworkParticle particle = new FireworkParticle
        {
            gameObject = particleObj,
            trail = trail,
            velocity = velocity,
            color = particleColor,
            lifetime = lifetime * Random.Range(0.8f, 1.2f),
            maxLifetime = lifetime,
            trailPositions = new List<Vector3>(),
            isActive = true,
            hasBurst = false,
            burstTimer = burstDelay
        };
        
        // 初期位置を設定（パーティクルの現在位置）
        particle.trailPositions.Add(particleObj.transform.position);
        
        // スプライトの色を設定
        spriteRenderer.color = particleColor;
        
        // 初期状態では完全に非表示（破裂するまで見えない）
        spriteRenderer.enabled = false;
        particleObj.SetActive(false);
        
        particles.Add(particle);
    }
    
    /// <summary>
    /// 軌跡を作成
    /// </summary>
    LineRenderer CreateTrail(GameObject parent)
    {
        GameObject trailObj = new GameObject("Trail");
        trailObj.transform.SetParent(parent.transform);
        trailObj.transform.localPosition = Vector3.zero;
        
        LineRenderer trail = trailObj.AddComponent<LineRenderer>();
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.material.name = $"TrailMaterial_{GetInstanceID()}";
        trail.material.hideFlags = HideFlags.HideAndDontSave;
        trail.material.SetFloat("_Mode", 3); // 透明度ブレンドモード
        
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth;
        trail.useWorldSpace = true;
        trail.sortingOrder = 5;
        trail.sortingLayerName = "Default";
        trail.enabled = true;
        
        return trail;
    }
    
    /// <summary>
    /// パーティクルスプライトを作成
    /// </summary>
    Sprite CreateParticleSprite()
    {
        // シンプルな円形スプライトを作成
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        Vector2 center = new Vector2(16, 16);
        float radius = 16f;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance / radius));
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// パーティクルを更新
    /// </summary>
    void UpdateParticles()
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            FireworkParticle particle = particles[i];
            if (!particle.isActive) continue;
            
            // 破裂タイミングをチェック
            if (!particle.hasBurst)
            {
                particle.burstTimer -= Time.deltaTime;
                if (particle.burstTimer <= 0)
                {
                    particle.hasBurst = true;
                    // 破裂時にパーティクルを表示
                    particle.gameObject.SetActive(true);
                    SpriteRenderer spriteRenderer = particle.gameObject.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.enabled = true;
                    }
                }
            }
            
            // 破裂していない場合は位置更新と軌跡更新をスキップ
            if (!particle.hasBurst)
            {
                continue;
            }
            
            // 生存時間を更新
            particle.lifetime -= Time.deltaTime;
            
            if (particle.lifetime <= 0)
            {
                // パーティクルを削除
                DestroyParticle(particle);
                particles.RemoveAt(i);
                continue;
            }
            
            // 位置を更新
            particle.gameObject.transform.position += particle.velocity * Time.deltaTime;
            
            // 軌跡を更新
            if (particle.trail != null)
            {
                UpdateTrail(particle);
            }
            
            // フェードアウト効果
            float fadeProgress = 1f - (particle.lifetime / particle.maxLifetime);
            UpdateParticleFade(particle, fadeProgress);
        }
        
        // 全パーティクルが消滅したらシステムを停止
        if (particles.Count == 0)
        {
            isActive = false;
        }
    }
    
    /// <summary>
    /// 軌跡を更新
    /// </summary>
    void UpdateTrail(FireworkParticle particle)
    {
        if (particle.trail == null) return;
        
        Vector3 currentPosition = particle.gameObject.transform.position;
        
        // 最小移動距離をチェック（軌跡の更新頻度を制御）
        if (particle.trailPositions.Count > 0)
        {
            Vector3 lastPosition = particle.trailPositions[particle.trailPositions.Count - 1];
            float moveDistance = Vector3.Distance(currentPosition, lastPosition);
            
            // 最小移動距離以下の場合は軌跡を更新しない
            if (moveDistance < 0.01f)
            {
                return;
            }
        }
        
        // 新しい位置を追加
        particle.trailPositions.Add(currentPosition);
        
        // 軌跡の長さを制限
        if (particle.trailPositions.Count > trailLength)
        {
            particle.trailPositions.RemoveAt(0);
        }
        
        // 軌跡を描画（最低3点以上で描画開始）
        if (particle.trailPositions.Count >= 3)
        {
            particle.trail.positionCount = particle.trailPositions.Count;
            
            for (int i = 0; i < particle.trailPositions.Count; i++)
            {
                particle.trail.SetPosition(i, particle.trailPositions[i]);
            }
            
            // 軌跡の色を更新
            Color trailColor = ApplyColorAdjustments(particle.color, trailBrightness, trailSaturation, trailAlpha);
            particle.trail.startColor = trailColor;
            particle.trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * 0.3f);
        }
        else if (particle.trailPositions.Count >= 2)
        {
            // 軌跡が短い場合は非表示
            particle.trail.positionCount = 0;
        }
    }
    
    /// <summary>
    /// パーティクルのフェード効果を更新
    /// </summary>
    void UpdateParticleFade(FireworkParticle particle, float fadeProgress)
    {
        SpriteRenderer spriteRenderer = particle.gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = particle.color;
            color.a = Mathf.Lerp(particleAlpha, 0f, fadeProgress);
            spriteRenderer.color = color;
        }
        
        // 軌跡もフェードアウト
        if (particle.trail != null)
        {
            Color trailColor = ApplyColorAdjustments(particle.color, trailBrightness, trailSaturation, trailAlpha);
            trailColor.a *= (1f - fadeProgress);
            particle.trail.startColor = trailColor;
            particle.trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * 0.3f);
        }
    }
    
    /// <summary>
    /// 色調整を適用
    /// </summary>
    Color ApplyColorAdjustments(Color baseColor, float brightness, float saturation, float alpha)
    {
        // HSVに変換
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        
        // 明度を調整
        v *= brightness;
        v = Mathf.Clamp01(v);
        
        // 彩度を調整
        s *= saturation;
        s = Mathf.Clamp01(s);
        
        // RGBに戻す
        Color adjustedColor = Color.HSVToRGB(h, s, v);
        adjustedColor.a = alpha;
        
        return adjustedColor;
    }
    
    /// <summary>
    /// パーティクルを削除
    /// </summary>
    void DestroyParticle(FireworkParticle particle)
    {
        if (particle.gameObject != null)
        {
            DestroyImmediate(particle.gameObject);
        }
        particle.isActive = false;
    }
    
    /// <summary>
    /// 全パーティクルをクリア
    /// </summary>
    void ClearParticles()
    {
        foreach (var particle in particles)
        {
            if (particle.gameObject != null)
            {
                DestroyImmediate(particle.gameObject);
            }
        }
        particles.Clear();
    }
    
    /// <summary>
    /// 花火を停止
    /// </summary>
    public void StopFirework()
    {
        isActive = false;
        ClearParticles();
    }
    
    void OnDestroy()
    {
        ClearParticles();
    }
}
