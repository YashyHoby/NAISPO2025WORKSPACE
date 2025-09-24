using UnityEngine;

/// <summary>
/// 水中・宇宙風背景の動的制御を行うコントローラー
/// </summary>
public class BackgroundController : MonoBehaviour
{
    [Header("背景設定")]
    [Tooltip("背景用のマテリアル")]
    public Material backgroundMaterial;
    
    [Header("色彩変化")]
    [Tooltip("メインカラーの変化速度")]
    public float colorChangeSpeed = 0.5f;
    
    [Tooltip("色相の変化範囲")]
    public Vector2 hueRange = new Vector2(0.5f, 0.8f);
    
    [Tooltip("色相のみ変化（明度・彩度は固定）")]
    public bool hueOnlyMode = true;
    
    [Header("明度・彩度調整")]
    [Tooltip("メインカラーの明度")]
    [Range(0.0f, 1.0f)]
    public float mainBrightness = 0.6f;
    
    [Tooltip("メインカラーの彩度")]
    [Range(0.0f, 1.0f)]
    public float mainSaturation = 0.7f;
    
    [Tooltip("セカンドカラーの明度")]
    [Range(0.0f, 1.0f)]
    public float secondBrightness = 0.3f;
    
    [Tooltip("セカンドカラーの彩度")]
    [Range(0.0f, 1.0f)]
    public float secondSaturation = 0.8f;
    
    [Tooltip("アクセントカラーの明度")]
    [Range(0.0f, 1.0f)]
    public float accentBrightness = 0.9f;
    
    [Tooltip("アクセントカラーの彩度")]
    [Range(0.0f, 1.0f)]
    public float accentSaturation = 0.9f;
    
    [Header("動的パラメータ")]
    [Tooltip("波の速度の変化範囲")]
    public Vector2 waveSpeedRange = new Vector2(0.5f, 2.0f);
    
    [Tooltip("光の強度の変化範囲")]
    public Vector2 lightIntensityRange = new Vector2(0.8f, 1.5f);
    
    [Tooltip("デジタルグリッチの変化範囲")]
    public Vector2 glitchRange = new Vector2(0.1f, 0.5f);
    
    [Header("液体揺れ制御")]
    [Tooltip("液体揺れの強度")]
    [Range(0.0f, 2.0f)]
    public float liquidIntensity = 1.0f;
    
    [Tooltip("液体揺れの速度")]
    [Range(0.1f, 3.0f)]
    public float liquidSpeed = 1.2f;
    
    [Tooltip("液体揺れの複雑さ")]
    [Range(1.0f, 8.0f)]
    public float liquidComplexity = 4.0f;
    
    [Tooltip("液体の粘性")]
    [Range(0.1f, 2.0f)]
    public float liquidViscosity = 0.8f;
    
    [Header("動的パラメータ制御")]
    [Tooltip("波のスケール基準値")]
    [Range(1.0f, 15.0f)]
    public float baseWaveScale = 5.0f;
    
    [Tooltip("波のスケール変動範囲")]
    [Range(0.0f, 10.0f)]
    public float waveScaleVariation = 3.0f;
    
    [Tooltip("液体強度の基準値")]
    [Range(0.0f, 2.0f)]
    public float baseLiquidIntensity = 1.0f;
    
    [Tooltip("液体強度の変動範囲")]
    [Range(0.0f, 1.0f)]
    public float liquidIntensityVariation = 0.5f;
    
    [Tooltip("液体速度の基準値")]
    [Range(0.1f, 3.0f)]
    public float baseLiquidSpeed = 1.2f;
    
    [Tooltip("液体速度の変動範囲")]
    [Range(0.0f, 2.0f)]
    public float liquidSpeedVariation = 0.8f;
    
    [Tooltip("ノイズスケールの基準値")]
    [Range(0.5f, 8.0f)]
    public float baseNoiseScale = 2.5f;
    
    [Tooltip("ノイズスケールの変動範囲")]
    [Range(0.0f, 4.0f)]
    public float noiseScaleVariation = 1.5f;
    
    [Header("環境応答")]
    [Tooltip("心オブジェクトの数に応じた背景変化")]
    public bool respondToHeartCount = true;
    
    [Tooltip("心オブジェクトの活動に応じた光の変化")]
    public bool respondToHeartActivity = true;
    
    private HeartManager heartManager;
    private float timeOffset;
    private Color baseMainColor;
    private Color baseSecondColor;
    private Color baseAccentColor;
    
    void Awake()
    {
        // ランダムな時間オフセットで個性を持たせる
        timeOffset = Random.Range(0f, 100f);
        
        // デバッグ情報
        if (backgroundMaterial == null)
        {
            Debug.LogWarning("[BackgroundController] backgroundMaterialが設定されていません。InspectorでBackground MaterialフィールドにBackgroundMaterial.matを設定してください。");
        }
        else
        {
            Debug.Log($"[BackgroundController] backgroundMaterial設定済み: {backgroundMaterial.name}");
        }
        
        // 基本色を保存
        if (backgroundMaterial != null)
        {
            // プロパティが存在するかチェックしてから取得
            if (backgroundMaterial.HasProperty("_MainColor"))
                baseMainColor = backgroundMaterial.GetColor("_MainColor");
            if (backgroundMaterial.HasProperty("_SecondColor"))
                baseSecondColor = backgroundMaterial.GetColor("_SecondColor");
            if (backgroundMaterial.HasProperty("_AccentColor"))
                baseAccentColor = backgroundMaterial.GetColor("_AccentColor");
        }
    }
    
    void Start()
    {
        // HeartManagerへの参照を取得
        if (respondToHeartCount || respondToHeartActivity)
        {
            heartManager = FindFirstObjectByType<HeartManager>();
            if (heartManager == null)
            {
                Debug.LogWarning("[BackgroundController] HeartManagerが見つかりません。環境応答機能が無効になります。");
            }
        }
    }
    
    void Update()
    {
        if (backgroundMaterial == null) return;
        
        float time = Time.time + timeOffset;
        
        // 基本的な時間変化
        UpdateTimeBasedChanges(time);
        
        // 心オブジェクトに応じた変化
        if (heartManager != null)
        {
            UpdateHeartBasedChanges();
        }
    }
    
    /// <summary>
    /// 時間ベースの背景変化を更新
    /// </summary>
    void UpdateTimeBasedChanges(float time)
    {
        // 色相の変化
        float hue = Mathf.Lerp(hueRange.x, hueRange.y, 
            (Mathf.Sin(time * colorChangeSpeed) + 1f) * 0.5f);
        
        if (hueOnlyMode)
        {
            // 色相のみ変化、明度・彩度は固定
            UpdateHueOnlyColors(hue);
        }
        else
        {
            // 従来の色変化（互換性維持）
            UpdateTraditionalColors(hue);
        }
        
        // 液体揺れパラメータの更新
        UpdateLiquidParameters();
        
        // 動的パラメータの更新
        UpdateDynamicParameters(time);
        
        // 波の速度変化（水中らしくゆったりと）
        if (backgroundMaterial.HasProperty("_WaveSpeed"))
        {
            float waveSpeed = Mathf.Lerp(waveSpeedRange.x, waveSpeedRange.y,
                (Mathf.Sin(time * 0.2f) + 1f) * 0.5f);
            backgroundMaterial.SetFloat("_WaveSpeed", waveSpeed);
        }
        
        // 光の強度変化（水中の光の揺らぎ）
        if (backgroundMaterial.HasProperty("_LightIntensity"))
        {
            float lightIntensity = Mathf.Lerp(lightIntensityRange.x, lightIntensityRange.y,
                (Mathf.Sin(time * 0.5f + 1.0f) + 1f) * 0.5f);
            backgroundMaterial.SetFloat("_LightIntensity", lightIntensity);
        }
        
        // コースティクス強度の変化
        if (backgroundMaterial.HasProperty("_CausticsIntensity"))
        {
            float causticsIntensity = 0.8f + 0.4f * Mathf.Sin(time * 0.3f);
            backgroundMaterial.SetFloat("_CausticsIntensity", causticsIntensity);
        }
        
        // 歪み強度の変化
        if (backgroundMaterial.HasProperty("_DistortionStrength"))
        {
            float distortionStrength = 0.05f + 0.1f * Mathf.Sin(time * 0.4f);
            backgroundMaterial.SetFloat("_DistortionStrength", distortionStrength);
        }
    }
    
    /// <summary>
    /// 色相のみ変化する色計算
    /// </summary>
    void UpdateHueOnlyColors(float hue)
    {
        // メインカラー
        if (backgroundMaterial.HasProperty("_MainColor"))
        {
            Color mainColor = Color.HSVToRGB(hue, mainSaturation, mainBrightness);
            backgroundMaterial.SetColor("_MainColor", mainColor);
        }
        
        // セカンドカラー
        if (backgroundMaterial.HasProperty("_SecondColor"))
        {
            Color secondColor = Color.HSVToRGB(hue, secondSaturation, secondBrightness);
            backgroundMaterial.SetColor("_SecondColor", secondColor);
        }
        
        // アクセントカラー
        if (backgroundMaterial.HasProperty("_AccentColor"))
        {
            float accentHue = (hue + 0.1f) % 1.0f; // 少し色相をずらす
            Color accentColor = Color.HSVToRGB(accentHue, accentSaturation, accentBrightness);
            backgroundMaterial.SetColor("_AccentColor", accentColor);
        }
    }
    
    /// <summary>
    /// 従来の色変化（互換性維持）
    /// </summary>
    void UpdateTraditionalColors(float hue)
    {
        // 明度を固定にして色相のみ変化
        Color dynamicMainColor = Color.HSVToRGB(hue, 0.7f, baseMainColor.a);
        Color dynamicSecondColor = Color.HSVToRGB(hue, 0.8f, baseSecondColor.a);
        Color dynamicAccentColor = Color.HSVToRGB(hue + 0.1f, 0.9f, baseAccentColor.a);
        
        // 基本色とブレンド（明度は維持）
        if (backgroundMaterial.HasProperty("_MainColor"))
        {
            Color finalMainColor = Color.Lerp(baseMainColor, dynamicMainColor, 0.3f);
            backgroundMaterial.SetColor("_MainColor", finalMainColor);
        }
        if (backgroundMaterial.HasProperty("_SecondColor"))
        {
            Color finalSecondColor = Color.Lerp(baseSecondColor, dynamicSecondColor, 0.3f);
            backgroundMaterial.SetColor("_SecondColor", finalSecondColor);
        }
        if (backgroundMaterial.HasProperty("_AccentColor"))
        {
            Color finalAccentColor = Color.Lerp(baseAccentColor, dynamicAccentColor, 0.4f);
            backgroundMaterial.SetColor("_AccentColor", finalAccentColor);
        }
    }
    
    /// <summary>
    /// 液体揺れパラメータの更新
    /// </summary>
    void UpdateLiquidParameters()
    {
        if (backgroundMaterial.HasProperty("_LiquidIntensity"))
            backgroundMaterial.SetFloat("_LiquidIntensity", liquidIntensity);
        
        if (backgroundMaterial.HasProperty("_LiquidSpeed"))
            backgroundMaterial.SetFloat("_LiquidSpeed", liquidSpeed);
        
        if (backgroundMaterial.HasProperty("_LiquidComplexity"))
            backgroundMaterial.SetFloat("_LiquidComplexity", liquidComplexity);
        
        if (backgroundMaterial.HasProperty("_LiquidViscosity"))
            backgroundMaterial.SetFloat("_LiquidViscosity", liquidViscosity);
    }
    
    /// <summary>
    /// 動的パラメータの更新（基準値と変動範囲を使用）
    /// </summary>
    void UpdateDynamicParameters(float time)
    {
        // 波のスケールの動的変化
        if (backgroundMaterial.HasProperty("_WaveScale"))
        {
            float waveScaleVariationValue = Mathf.Sin(time * 0.3f) * waveScaleVariation;
            float dynamicWaveScale = baseWaveScale + waveScaleVariationValue;
            backgroundMaterial.SetFloat("_WaveScale", dynamicWaveScale);
        }
        
        // 液体強度の動的変化
        if (backgroundMaterial.HasProperty("_LiquidIntensity"))
        {
            float liquidIntensityVariationValue = Mathf.Sin(time * 0.4f + 1.0f) * liquidIntensityVariation;
            float dynamicLiquidIntensity = baseLiquidIntensity + liquidIntensityVariationValue;
            backgroundMaterial.SetFloat("_LiquidIntensity", dynamicLiquidIntensity);
        }
        
        // 液体速度の動的変化
        if (backgroundMaterial.HasProperty("_LiquidSpeed"))
        {
            float liquidSpeedVariationValue = Mathf.Cos(time * 0.5f + 2.0f) * liquidSpeedVariation;
            float dynamicLiquidSpeed = baseLiquidSpeed + liquidSpeedVariationValue;
            backgroundMaterial.SetFloat("_LiquidSpeed", dynamicLiquidSpeed);
        }
        
        // ノイズスケールの動的変化
        if (backgroundMaterial.HasProperty("_NoiseScale"))
        {
            float noiseScaleVariationValue = Mathf.Sin(time * 0.2f + 3.0f) * noiseScaleVariation;
            float dynamicNoiseScale = baseNoiseScale + noiseScaleVariationValue;
            backgroundMaterial.SetFloat("_NoiseScale", dynamicNoiseScale);
        }
    }
    
    /// <summary>
    /// 心オブジェクトの状態に応じた背景変化を更新
    /// </summary>
    void UpdateHeartBasedChanges()
    {
        if (!respondToHeartCount && !respondToHeartActivity) return;
        
        int heartCount = heartManager.agents.Count;
        float maxHearts = heartManager.maxAgents;
        float heartRatio = Mathf.Clamp01(heartCount / maxHearts);
        
        if (respondToHeartCount)
        {
            // 心オブジェクトの数に応じて背景の活発さを変更
            float activityMultiplier = 0.5f + heartRatio * 1.5f;
            
            // 波のスケールを調整
            if (backgroundMaterial.HasProperty("_WaveScale"))
            {
                float baseWaveScale = backgroundMaterial.GetFloat("_WaveScale");
                backgroundMaterial.SetFloat("_WaveScale", baseWaveScale * activityMultiplier);
            }
            
            // ノイズスケールを調整
            if (backgroundMaterial.HasProperty("_NoiseScale"))
            {
                float baseNoiseScale = backgroundMaterial.GetFloat("_NoiseScale");
                backgroundMaterial.SetFloat("_NoiseScale", baseNoiseScale * activityMultiplier);
            }
        }
        
        if (respondToHeartActivity)
        {
            // 心オブジェクトの活動度を計算（平均速度など）
            float totalActivity = 0f;
            int activeHearts = 0;
            
            foreach (var agent in heartManager.agents)
            {
                if (agent != null && agent.gameObject.activeInHierarchy)
                {
                    Rigidbody2D rb = agent.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        totalActivity += rb.linearVelocity.magnitude;
                        activeHearts++;
                    }
                }
            }
            
            if (activeHearts > 0)
            {
                float averageActivity = totalActivity / activeHearts;
                float activityFactor = Mathf.Clamp01(averageActivity / 5f); // 5は基準速度
                
                // 活動度に応じて光の強度を調整
                if (backgroundMaterial.HasProperty("_LightIntensity"))
                {
                    float currentLightIntensity = backgroundMaterial.GetFloat("_LightIntensity");
                    float targetLightIntensity = currentLightIntensity * (1f + activityFactor * 0.5f);
                    backgroundMaterial.SetFloat("_LightIntensity", targetLightIntensity);
                }
            }
        }
    }
    
    /// <summary>
    /// 外部からの背景効果トリガー（イベント発生時など）
    /// </summary>
    public void TriggerBackgroundFlash(Color flashColor, float duration = 0.5f)
    {
        StartCoroutine(FlashCoroutine(flashColor, duration));
    }
    
    private System.Collections.IEnumerator FlashCoroutine(Color flashColor, float duration)
    {
        if (backgroundMaterial == null) yield break;
        
        Color originalAccent = backgroundMaterial.HasProperty("_AccentColor") ? 
            backgroundMaterial.GetColor("_AccentColor") : Color.white;
        float originalIntensity = backgroundMaterial.HasProperty("_LightIntensity") ? 
            backgroundMaterial.GetFloat("_LightIntensity") : 1f;
        
        // フラッシュ開始
        if (backgroundMaterial.HasProperty("_AccentColor"))
            backgroundMaterial.SetColor("_AccentColor", flashColor);
        if (backgroundMaterial.HasProperty("_LightIntensity"))
            backgroundMaterial.SetFloat("_LightIntensity", originalIntensity * 2f);
        
        yield return new WaitForSeconds(duration * 0.1f);
        
        // フェードアウト
        float elapsed = 0f;
        while (elapsed < duration * 0.9f)
        {
            float t = elapsed / (duration * 0.9f);
            if (backgroundMaterial.HasProperty("_AccentColor"))
                backgroundMaterial.SetColor("_AccentColor", Color.Lerp(flashColor, originalAccent, t));
            if (backgroundMaterial.HasProperty("_LightIntensity"))
                backgroundMaterial.SetFloat("_LightIntensity", Mathf.Lerp(originalIntensity * 2f, originalIntensity, t));
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 完全に元に戻す
        if (backgroundMaterial.HasProperty("_AccentColor"))
            backgroundMaterial.SetColor("_AccentColor", originalAccent);
        if (backgroundMaterial.HasProperty("_LightIntensity"))
            backgroundMaterial.SetFloat("_LightIntensity", originalIntensity);
    }
}
