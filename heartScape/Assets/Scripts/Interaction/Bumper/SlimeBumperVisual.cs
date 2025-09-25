using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeartScape.Interaction.Bumper
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SlimeBumperVisual : MonoBehaviour
    {
        [Header("長方形形状設定")]
        [Tooltip("長方形の幅")]
        [Range(0.1f, 5.0f)]
        public float rectWidth = 1.2f;
        
        [Tooltip("長方形の高さ")]
        [Range(0.1f, 5.0f)]
        public float rectHeight = 0.8f;
        
        [Tooltip("角の丸み")]
        [Range(0.0f, 0.5f)]
        public float cornerRadius = 0.2f;
        
        [Header("スライム外観")]
        [Tooltip("スライムの基本色")]
        public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 1.0f);
        
        [Tooltip("外郭の厚み")]
        [Range(0.0f, 0.2f)]
        public float outerThickness = 0.05f;
        
        [Tooltip("外郭の透明度")]
        [Range(0.0f, 1.0f)]
        public float outerAlpha = 0.3f;
        
        [Tooltip("外郭の色")]
        public Color outerColor = new Color(0.1f, 0.6f, 0.2f, 1.0f);
        
        [Tooltip("内郭の厚み")]
        [Range(0.0f, 0.2f)]
        public float innerThickness = 0.03f;
        
        [Tooltip("内郭の透明度")]
        [Range(0.0f, 1.0f)]
        public float innerAlpha = 0.8f;
        
        [Tooltip("内郭の色")]
        public Color innerColor = new Color(0.3f, 0.9f, 0.4f, 1.0f);
        
        [Header("光沢と粘性")]
        [Tooltip("光沢の強度")]
        [Range(0.0f, 2.0f)]
        public float glossiness = 0.8f;
        
        [Tooltip("粘性の強度")]
        [Range(0.0f, 1.0f)]
        public float viscosity = 0.9f;
        
        [Tooltip("スペキュラーの強度")]
        [Range(1.0f, 100.0f)]
        public float specularPower = 8.0f;
        
        [Header("アニメーション")]
        [Tooltip("アニメーション速度")]
        [Range(0.0f, 5.0f)]
        public float animationSpeed = 1.0f;
        
        [Tooltip("パルスの強度")]
        [Range(0.0f, 0.3f)]
        public float pulseIntensity = 0.1f;
        
        [Header("衝突反応")]
        [Tooltip("衝突時のパルス強度")]
        [Range(0.0f, 1.0f)]
        public float collisionPulseIntensity = 0.3f;
        
        [Tooltip("衝突反応の持続時間")]
        [Range(0.1f, 2.0f)]
        public float collisionDuration = 0.5f;
        
        [Header("マテリアル設定")]
        [Tooltip("スクリプトでマテリアルを自動更新する")]
        public bool autoUpdateMaterial = true;
        
        [Tooltip("シーン再生時にパラメータをリセットする")]
        public bool resetOnPlay = false;
        
        [Tooltip("マテリアルの設定を保護する（手動変更を維持）")]
        public bool protectMaterialSettings = false;

        // プライベート変数
        private SpriteRenderer spriteRenderer;
        private Material slimeMaterial;
        private float collisionTime;
        private bool isCollisionActive;
        private float originalPulseIntensity;
        private Texture2D whiteTexture;
        private Sprite whiteSprite;
        
        // パブリックプロパティ
        public Material SlimeMaterial => slimeMaterial;

        void Awake()
        {
            InitializeVisual();
        }

        void Start()
        {
            // シーン再生時のリセット処理
            if (resetOnPlay)
            {
                ApplySlimeParameters();
            }
            else if (autoUpdateMaterial && !protectMaterialSettings)
            {
                ApplySlimeParameters();
            }
            
            UpdateVisualSize();
        }

        void Update()
        {
            // 衝突反応の更新
            if (isCollisionActive)
            {
                UpdateCollisionReaction();
            }
        }

        void OnValidate()
        {
            // OnValidateでは何もしない（エラーを避けるため）
            // 手動で「即座に更新」ボタンを使用してください
        }

        public void InitializeForEditor()
        {
#if UNITY_EDITOR
            // エディター時の安全な初期化
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            // スプライトの設定
            if (spriteRenderer.sprite == null)
            {
                CreateAndSetWhiteSprite();
            }

            // マテリアルの設定（エディター時はsharedMaterialを使用）
            CreateOrGetSlimeMaterial();

            // パラメータを適用
            if (slimeMaterial != null)
            {
                ApplySlimeParametersForced();
            }

            // サイズを更新
            UpdateVisualSizeEditor();
#endif
        }

        public void UpdateVisualSizeEditor()
        {
#if UNITY_EDITOR
            if (spriteRenderer == null) return;

            // エディター時の安全なサイズ更新
            try
            {
                spriteRenderer.size = new Vector2(rectWidth, rectHeight);
                transform.localScale = Vector3.one;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not update sprite size in editor: {e.Message}");
            }
#endif
        }

        void CreateOrGetSlimeMaterial()
        {
            // 常に新しいマテリアルインスタンスを作成（独立性を確保）
            Shader slimeShader = Shader.Find("Custom/SlimeRectangle");
            if (slimeShader != null)
            {
                // 既存のマテリアルをクリーンアップ
                if (slimeMaterial != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(slimeMaterial);
                    }
                    else
                    {
                        DestroyImmediate(slimeMaterial);
                    }
                }
                
                // 新しいマテリアルインスタンスを作成（完全に独立）
                slimeMaterial = new Material(slimeShader);
                slimeMaterial.name = $"SlimeMaterial_{gameObject.name}_{gameObject.GetInstanceID()}_{System.Guid.NewGuid().ToString("N")[..8]}";
                
                // エディター時とランタイム時で適切な設定方法を使用
                if (Application.isPlaying)
                {
                    spriteRenderer.material = slimeMaterial;
                }
                else
                {
                    spriteRenderer.sharedMaterial = slimeMaterial;
                }
                
                Debug.Log($"Created unique material: {slimeMaterial.name} for {gameObject.name}");
            }
            else
            {
                Debug.LogError("SlimeRectangle shader not found!");
            }
        }

        void InitializeVisual()
        {
            // SpriteRendererを取得または追加
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // 白いスプライトを作成・設定
            CreateAndSetWhiteSprite();

            // スライムマテリアルを作成
            CreateOrGetSlimeMaterial();

            originalPulseIntensity = pulseIntensity;
        }

        void CreateAndSetWhiteSprite()
        {
            // 既存のテクスチャがある場合は再利用
            if (whiteTexture == null)
            {
                // 白いテクスチャを作成
                whiteTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                whiteTexture.name = $"WhiteTexture_{gameObject.GetInstanceID()}";
                Color[] pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                whiteTexture.SetPixels(pixels);
                whiteTexture.Apply(false, false);
            }

            // 既存のスプライトがある場合は再利用
            if (whiteSprite == null)
            {
                // スプライトを作成
                whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
                whiteSprite.name = $"WhiteSprite_{gameObject.GetInstanceID()}";
            }
            
            // SpriteRendererに設定
            if (spriteRenderer.sprite != whiteSprite)
            {
                spriteRenderer.sprite = whiteSprite;
            }
        }

        public void UpdateVisualSize()
        {
            if (spriteRenderer == null) return;

            // SpriteRendererのサイズを直接設定（スケールは使わない）
            spriteRenderer.size = new Vector2(rectWidth, rectHeight);
            
            // ローカルスケールは1に固定
            transform.localScale = Vector3.one;
        }

        public void ApplySlimeParameters()
        {
            ApplySlimeParametersForced();
        }
        
        public void ApplySlimeParametersForced()
        {
            if (slimeMaterial == null) 
            {
                Debug.LogWarning("SlimeMaterial is null! Trying to reinitialize...");
                InitializeVisual();
                if (slimeMaterial == null) return;
            }
            

            // 基本色とサイズ
            slimeMaterial.SetColor("_SlimeColor", slimeColor);
            slimeMaterial.SetFloat("_RectWidth", rectWidth);
            slimeMaterial.SetFloat("_RectHeight", rectHeight);
            slimeMaterial.SetFloat("_CornerRadius", cornerRadius);

            // 外郭設定
            slimeMaterial.SetFloat("_OuterThickness", outerThickness);
            slimeMaterial.SetFloat("_OuterAlpha", outerAlpha);
            slimeMaterial.SetColor("_OuterColor", outerColor);

            // 内郭設定
            slimeMaterial.SetFloat("_InnerThickness", innerThickness);
            slimeMaterial.SetFloat("_InnerAlpha", innerAlpha);
            slimeMaterial.SetColor("_InnerColor", innerColor);

            // 光沢と粘性
            slimeMaterial.SetFloat("_Glossiness", glossiness);
            slimeMaterial.SetFloat("_Viscosity", viscosity);
            slimeMaterial.SetFloat("_SpecularPower", specularPower);

            // アニメーション
            slimeMaterial.SetFloat("_AnimationSpeed", animationSpeed);
            slimeMaterial.SetFloat("_PulseIntensity", pulseIntensity);
        }

        public void OnBumperCollision()
        {
            // 衝突反応を開始
            collisionTime = 0f;
            isCollisionActive = true;
            
            // パルス強度を一時的に増加
            pulseIntensity = collisionPulseIntensity;
            ApplySlimeParameters();
        }

        void UpdateCollisionReaction()
        {
            collisionTime += Time.deltaTime;
            
            if (collisionTime >= collisionDuration)
            {
                isCollisionActive = false;
                // 元のパルス強度に戻す
                pulseIntensity = originalPulseIntensity;
                ApplySlimeParameters();
            }
        }

        // プリセット機能
        public void SetViscousPreset()
        {
            slimeColor = new Color(0.1f, 0.7f, 0.2f, 1.0f);
            outerThickness = 0.08f;
            outerAlpha = 0.2f;
            innerThickness = 0.05f;
            innerAlpha = 0.9f;
            glossiness = 1.2f;
            viscosity = 1.0f;
            specularPower = 6.0f;
            ApplySlimeParameters();
        }

        public void SetStandardPreset()
        {
            slimeColor = new Color(0.2f, 0.8f, 0.3f, 1.0f);
            outerThickness = 0.05f;
            outerAlpha = 0.3f;
            innerThickness = 0.03f;
            innerAlpha = 0.8f;
            glossiness = 0.8f;
            viscosity = 0.9f;
            specularPower = 8.0f;
            ApplySlimeParameters();
        }

        public void SetDryPreset()
        {
            slimeColor = new Color(0.6f, 0.4f, 0.2f, 1.0f);
            outerThickness = 0.02f;
            outerAlpha = 0.6f;
            innerThickness = 0.01f;
            innerAlpha = 0.5f;
            glossiness = 0.4f;
            viscosity = 0.3f;
            specularPower = 15.0f;
            ApplySlimeParameters();
        }

        /// <summary>
        /// マテリアルから現在の設定を読み込む
        /// </summary>
        public void LoadParametersFromMaterial()
        {
            if (slimeMaterial == null) return;
            
            // マテリアルから値を読み込んでスクリプトのパラメータを更新
            if (slimeMaterial.HasProperty("_SlimeColor"))
                slimeColor = slimeMaterial.GetColor("_SlimeColor");
            if (slimeMaterial.HasProperty("_RectWidth"))
                rectWidth = slimeMaterial.GetFloat("_RectWidth");
            if (slimeMaterial.HasProperty("_RectHeight"))
                rectHeight = slimeMaterial.GetFloat("_RectHeight");
            if (slimeMaterial.HasProperty("_CornerRadius"))
                cornerRadius = slimeMaterial.GetFloat("_CornerRadius");
            
            if (slimeMaterial.HasProperty("_OuterThickness"))
                outerThickness = slimeMaterial.GetFloat("_OuterThickness");
            if (slimeMaterial.HasProperty("_OuterAlpha"))
                outerAlpha = slimeMaterial.GetFloat("_OuterAlpha");
            if (slimeMaterial.HasProperty("_OuterColor"))
                outerColor = slimeMaterial.GetColor("_OuterColor");
            
            if (slimeMaterial.HasProperty("_InnerThickness"))
                innerThickness = slimeMaterial.GetFloat("_InnerThickness");
            if (slimeMaterial.HasProperty("_InnerAlpha"))
                innerAlpha = slimeMaterial.GetFloat("_InnerAlpha");
            if (slimeMaterial.HasProperty("_InnerColor"))
                innerColor = slimeMaterial.GetColor("_InnerColor");
            
            if (slimeMaterial.HasProperty("_Glossiness"))
                glossiness = slimeMaterial.GetFloat("_Glossiness");
            if (slimeMaterial.HasProperty("_Viscosity"))
                viscosity = slimeMaterial.GetFloat("_Viscosity");
            if (slimeMaterial.HasProperty("_SpecularPower"))
                specularPower = slimeMaterial.GetFloat("_SpecularPower");
            
            if (slimeMaterial.HasProperty("_AnimationSpeed"))
                animationSpeed = slimeMaterial.GetFloat("_AnimationSpeed");
            if (slimeMaterial.HasProperty("_PulseIntensity"))
                pulseIntensity = slimeMaterial.GetFloat("_PulseIntensity");
                
            Debug.Log("Parameters loaded from material");
        }
        
        /// <summary>
        /// 保護モードを切り替える
        /// </summary>
        public void ToggleProtection()
        {
            protectMaterialSettings = !protectMaterialSettings;
            
            if (protectMaterialSettings)
            {
                LoadParametersFromMaterial();
                Debug.Log("Material protection enabled. Current material settings loaded to script.");
            }
            else
            {
                Debug.Log("Material protection disabled. Script will update material automatically.");
            }
        }

        /// <summary>
        /// 色を設定（メイン、外郭、内郭を一括設定）
        /// </summary>
        public void SetSlimeColors(Color main, Color outer, Color inner)
        {
            slimeColor = main;
            outerColor = outer;
            innerColor = inner;
            
            // 強制的にマテリアルを更新
            ApplySlimeParameters();
        }

        /// <summary>
        /// メイン色のみを設定
        /// </summary>
        public void SetMainColor(Color color)
        {
            slimeColor = color;
            
            // 強制的にマテリアルを更新
            ApplySlimeParameters();
        }

        /// <summary>
        /// 色相を変更（明度・彩度は維持）
        /// </summary>
        public void SetHue(float hue)
        {
            Color.RGBToHSV(slimeColor, out float h, out float s, out float v);
            slimeColor = Color.HSVToRGB(hue, s, v);
            
            // 外郭と内郭も同じ色相に調整
            Color.RGBToHSV(outerColor, out h, out s, out v);
            outerColor = Color.HSVToRGB(hue, s * 0.8f, v * 0.6f);
            
            Color.RGBToHSV(innerColor, out h, out s, out v);
            innerColor = Color.HSVToRGB(hue, s * 1.2f, v * 1.1f);
            
            // 強制的にマテリアルを更新
            ApplySlimeParameters();
        }

        /// <summary>
        /// ランダムな色を設定
        /// </summary>
        public void SetRandomColors()
        {
            float hue = Random.Range(0f, 1f);
            float saturation = Random.Range(0.6f, 1.0f);
            float brightness = Random.Range(0.5f, 0.9f);
            
            slimeColor = Color.HSVToRGB(hue, saturation, brightness);
            outerColor = Color.HSVToRGB(hue, saturation * 0.8f, brightness * 0.6f);
            innerColor = Color.HSVToRGB(hue, saturation * 1.2f, Mathf.Min(brightness * 1.1f, 1.0f));
            
            // 強制的にマテリアルを更新
            ApplySlimeParameters();
        }

        /// <summary>
        /// 全ての設定を即座に反映
        /// </summary>
        public void ForceUpdateAll()
        {
            InitializeForEditor();
            ApplySlimeParametersForced();
            UpdateVisualSizeEditor();
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        void OnDestroy()
        {
            // マテリアルのクリーンアップ
            if (slimeMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(slimeMaterial);
                }
                else
                {
                    DestroyImmediate(slimeMaterial);
                }
                slimeMaterial = null;
            }
            
            // テクスチャとスプライトのクリーンアップ
            if (whiteSprite != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(whiteSprite);
                }
                else
                {
                    DestroyImmediate(whiteSprite);
                }
                whiteSprite = null;
            }
            
            if (whiteTexture != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(whiteTexture);
                }
                else
                {
                    DestroyImmediate(whiteTexture);
                }
                whiteTexture = null;
            }
        }
    }
}
