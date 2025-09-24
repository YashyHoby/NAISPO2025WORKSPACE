using UnityEngine;

/// <summary>
/// 液体が溶けて離散するような軌跡パーティクル
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TrailParticle : MonoBehaviour
{
    [Header("パーティクル設定")]
    [Tooltip("基本スプライト（円形推奨）")]
    public Sprite particleSprite;
    
    [Header("離散化効果")]
    [Tooltip("分裂する子パーティクルの数")]
    public int fragmentCount = 3;
    
    [Tooltip("子パーティクルのサイズ比率")]
    public float fragmentSizeRatio = 0.3f;
    
    [Tooltip("離散化開始のタイミング（生存時間の割合）")]
    public float discretizationStart = 0.6f;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float originalSize;
    private float lifetime;
    private float discretizationSpeed;
    private Vector3 velocity;
    private float elapsedTime;
    private bool isInitialized = false;
    private bool isDiscretizing = false;
    
    // 子パーティクル用のデータ
    private struct FragmentData
    {
        public Vector3 position;
        public Vector3 velocity;
        public float size;
        public float alpha;
    }
    
    private FragmentData[] fragments;
    private bool useFragments = false;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // レンダリング順序を心オブジェクトより後ろに設定
        spriteRenderer.sortingOrder = -10; // 心オブジェクトより後ろに描画
        
        // デフォルトスプライトの設定（動的生成を避ける）
        if (particleSprite != null)
        {
            spriteRenderer.sprite = particleSprite;
        }
        else if (spriteRenderer.sprite == null)
        {
            // フォールバック：Unity標準の円形スプライトを使用
            Sprite fallbackSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (fallbackSprite != null)
            {
                spriteRenderer.sprite = fallbackSprite;
            }
            else
            {
                Debug.LogWarning("[TrailParticle] particleSpriteが設定されていません。プレハブでスプライトを設定してください。");
            }
        }
    }
    
    /// <summary>
    /// パーティクルを初期化
    /// </summary>
    public void Initialize(Color color, float size, float lifetime, Vector3 velocity, float discretizationSpeed)
    {
        this.originalColor = color;
        this.originalSize = size;
        this.lifetime = lifetime;
        this.velocity = velocity;
        this.discretizationSpeed = discretizationSpeed;
        
        elapsedTime = 0f;
        isInitialized = true;
        isDiscretizing = false;
        useFragments = false;
        
        // 初期設定を適用
        spriteRenderer.color = color;
        transform.localScale = Vector3.one * size;
        
        // フラグメントデータを初期化
        InitializeFragments();
    }
    
    /// <summary>
    /// フラグメントデータを初期化
    /// </summary>
    void InitializeFragments()
    {
        fragments = new FragmentData[fragmentCount];
        
        for (int i = 0; i < fragmentCount; i++)
        {
            float angle = (float)i / fragmentCount * 2f * Mathf.PI;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            
            fragments[i] = new FragmentData
            {
                position = transform.position,
                velocity = velocity + direction * Random.Range(0.5f, 2f),
                size = originalSize * fragmentSizeRatio * Random.Range(0.8f, 1.2f),
                alpha = originalColor.a
            };
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        elapsedTime += Time.deltaTime;
        float normalizedTime = elapsedTime / lifetime;
        
        if (normalizedTime >= 1f)
        {
            // 生存時間終了
            gameObject.SetActive(false);
            return;
        }
        
        // 離散化の開始判定
        if (!isDiscretizing && normalizedTime >= discretizationStart)
        {
            StartDiscretization();
        }
        
        if (useFragments)
        {
            UpdateFragments();
        }
        else
        {
            UpdateMainParticle();
        }
    }
    
    /// <summary>
    /// メインパーティクルの更新
    /// </summary>
    void UpdateMainParticle()
    {
        float normalizedTime = elapsedTime / lifetime;
        
        // 位置の更新
        transform.position += velocity * Time.deltaTime;
        
        // フェードアウト
        float alpha = Mathf.Lerp(originalColor.a, 0f, normalizedTime);
        Color currentColor = originalColor;
        currentColor.a = alpha;
        spriteRenderer.color = currentColor;
        
        // サイズの変化（わずかに拡大）
        float sizeMultiplier = 1f + normalizedTime * 0.2f;
        transform.localScale = Vector3.one * originalSize * sizeMultiplier;
        
        // 速度の減衰
        velocity *= 0.98f;
    }
    
    /// <summary>
    /// 離散化を開始
    /// </summary>
    void StartDiscretization()
    {
        isDiscretizing = true;
        useFragments = true;
        
        // 簡略化：メインスプライトは表示したまま、効果で離散化をシミュレート
        // spriteRenderer.enabled = false; // コメントアウト
    }
    
    /// <summary>
    /// フラグメントの更新（簡略化版）
    /// </summary>
    void UpdateFragments()
    {
        float normalizedTime = elapsedTime / lifetime;
        float discretizationProgress = (normalizedTime - discretizationStart) / (1f - discretizationStart);
        
        // 簡略化：メインスプライトでフラグメント効果をシミュレート
        Color fragmentColor = originalColor;
        fragmentColor.a = Mathf.Lerp(originalColor.a * 0.8f, 0f, discretizationProgress);
        spriteRenderer.color = fragmentColor;
        
        // サイズの縮小とランダムな揺れ
        float sizeMultiplier = 1f - discretizationProgress * 0.5f;
        float shake = Mathf.Sin(Time.time * 10f) * discretizationProgress * 0.1f;
        transform.localScale = Vector3.one * originalSize * fragmentSizeRatio * (sizeMultiplier + shake);
        
        // 位置の微調整（離散化効果）
        Vector3 offset = new Vector3(
            Mathf.Sin(Time.time * 8f) * discretizationProgress * 0.2f,
            -discretizationProgress * Time.deltaTime * 2f, // 重力効果
            0f
        );
        transform.position += offset * Time.deltaTime;
    }
    
    // カスタム描画は複雑すぎるため、簡略化されたアプローチを使用
    // フラグメント効果は透明度変化とスケール変化で代替
    
    // 動的オブジェクト生成を削除してエディター保存時のエラーを回避
    
    /// <summary>
    /// パーティクルがアクティブかどうか
    /// </summary>
    public bool IsActive()
    {
        return isInitialized && elapsedTime < lifetime;
    }
    
    /// <summary>
    /// パーティクルをリセット
    /// </summary>
    public void ResetParticle()
    {
        isInitialized = false;
        isDiscretizing = false;
        useFragments = false;
        elapsedTime = 0f;
        spriteRenderer.enabled = true;
        
        if (fragments != null)
        {
            System.Array.Clear(fragments, 0, fragments.Length);
        }
    }
    
    void OnDisable()
    {
        ResetParticle();
    }
}
