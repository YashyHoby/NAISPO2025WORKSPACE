using UnityEngine;

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
            // エディターでパラメータが変更された時に自動更新
            if (Application.isPlaying && slimeMaterial != null && autoUpdateMaterial && !protectMaterialSettings)
            {
                ApplySlimeParameters();
                UpdateVisualSize();
            }
            else if (Application.isPlaying)
            {
                // 保護モードでもサイズは更新
                UpdateVisualSize();
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

            // スライムマテリアルを作成（インスタンス化）
            Shader slimeShader = Shader.Find("Custom/SlimeRectangle");
            if (slimeShader != null)
            {
                // 既存のマテリアルがある場合はそれをベースに
                if (spriteRenderer.material != null && spriteRenderer.material.shader == slimeShader)
                {
                    slimeMaterial = new Material(spriteRenderer.material);
                }
                else
                {
                    slimeMaterial = new Material(slimeShader);
                }
                
                slimeMaterial.name = $"SlimeMaterial_{gameObject.GetInstanceID()}";
                spriteRenderer.material = slimeMaterial;
            }
            else
            {
                Debug.LogError("SlimeRectangle shader not found!");
            }

            originalPulseIntensity = pulseIntensity;
        }

        void CreateAndSetWhiteSprite()
        {
            // 白いテクスチャを作成
            Texture2D whiteTexture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            whiteTexture.SetPixels(pixels);
            whiteTexture.Apply();

            // スプライトを作成
            Sprite whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            
            // SpriteRendererに設定
            spriteRenderer.sprite = whiteSprite;
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
            // 保護モードの場合は更新しない
            if (protectMaterialSettings)
            {
                Debug.Log("Material settings are protected. Skipping parameter update.");
                return;
            }
            
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

        void OnDestroy()
        {
            if (slimeMaterial != null)
            {
                DestroyImmediate(slimeMaterial);
            }
        }
    }
}
