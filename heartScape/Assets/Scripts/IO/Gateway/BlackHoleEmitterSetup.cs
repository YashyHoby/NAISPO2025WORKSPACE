using UnityEngine;
using HeartScape.IO.Gateway;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// ブラックホールエミッターのセットアップ
    /// シーンに直接配置するためのヘルパー
    /// </summary>
    public class BlackHoleEmitterSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [SerializeField] private bool autoSetupOnAwake = true;
        [SerializeField] private bool createVisualRoot = true;
        [SerializeField] private bool createSpawnPoint = true;
        
        [Header("Black Hole Settings")]
        [SerializeField] private Color holeColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color holeCenterColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] private float holeSize = 1f;
        [SerializeField] private float holeFadeRadius = 0.8f;
        
        [Header("Bubble Settings")]
        [SerializeField] private bool enableBubbleEmission = true;
        [SerializeField] private float bubbleEmissionRate = 2f;
        [SerializeField] private float bubbleLifetime = 3f;
        [SerializeField] private float bubbleSize = 0.1f;
        [SerializeField] private float bubbleSpeed = 0.5f;
        [SerializeField] private Color bubbleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        
        [Header("Object Appearance Settings")]
        [SerializeField] private float objectAppearSpeed = 5f;
        [SerializeField] private float objectMaxScale = 1.2f;
        
        void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupBlackHoleEmitter();
            }
        }
        
        /// <summary>
        /// ブラックホールエミッターをセットアップ
        /// </summary>
        [ContextMenu("Setup Black Hole Emitter")]
        public void SetupBlackHoleEmitter()
        {
            // BlackHoleEmitterVisualコンポーネントを取得または追加
            BlackHoleEmitterVisual emitterVisual = GetComponent<BlackHoleEmitterVisual>();
            if (emitterVisual == null)
            {
                emitterVisual = gameObject.AddComponent<BlackHoleEmitterVisual>();
                Debug.Log($"[BlackHoleEmitterSetup] BlackHoleEmitterVisual added to {gameObject.name}");
            }
            
            // 設定を適用
            ApplySettings(emitterVisual);
            
            // ビジュアルコンポーネントをセットアップ
            if (createVisualRoot)
            {
                SetupVisualComponents(emitterVisual);
            }
            
            if (createSpawnPoint)
            {
                SetupSpawnPoint(emitterVisual);
            }
            
            Debug.Log($"[BlackHoleEmitterSetup] Setup completed for {gameObject.name}");
        }
        
        /// <summary>
        /// 設定を適用
        /// </summary>
        void ApplySettings(BlackHoleEmitterVisual emitterVisual)
        {
            // ブラックホール設定
            emitterVisual.holeColor = holeColor;
            emitterVisual.holeCenterColor = holeCenterColor;
            emitterVisual.holeSize = holeSize;
            emitterVisual.holeFadeRadius = holeFadeRadius;
            
            // 泡設定
            emitterVisual.enableBubbleEmission = enableBubbleEmission;
            emitterVisual.bubbleEmissionRate = bubbleEmissionRate;
            emitterVisual.bubbleLifetime = bubbleLifetime;
            emitterVisual.bubbleSize = bubbleSize;
            emitterVisual.bubbleSpeed = bubbleSpeed;
            emitterVisual.bubbleColor = bubbleColor;
            
            // オブジェクト出現設定
            emitterVisual.objectAppearSpeed = objectAppearSpeed;
            emitterVisual.objectMaxScale = objectMaxScale;
        }
        
        /// <summary>
        /// ビジュアルコンポーネントをセットアップ
        /// </summary>
        void SetupVisualComponents(BlackHoleEmitterVisual emitterVisual)
        {
            // VisualRootを作成
            Transform visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                GameObject visualRootObj = new GameObject("VisualRoot");
                visualRoot = visualRootObj.transform;
                visualRoot.SetParent(transform);
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }
            
            // ブラックホールレンダラーを作成
            Transform holeTransform = visualRoot.Find("BlackHole");
            if (holeTransform == null)
            {
                GameObject holeObj = new GameObject("BlackHole");
                holeObj.transform.SetParent(visualRoot);
                holeObj.transform.localPosition = Vector3.zero;
                holeObj.transform.localRotation = Quaternion.identity;
                holeObj.transform.localScale = Vector3.one;
                
                SpriteRenderer holeRenderer = holeObj.AddComponent<SpriteRenderer>();
                holeRenderer.sprite = CreateBlackHoleSprite();
                holeRenderer.sortingOrder = -1; // 心オブジェクトより後ろに描画
            }
            
            // 泡パーティクルシステムを作成
            Transform particleTransform = visualRoot.Find("BubbleParticles");
            if (particleTransform == null)
            {
                GameObject particleObj = new GameObject("BubbleParticles");
                particleObj.transform.SetParent(visualRoot);
                particleObj.transform.localPosition = Vector3.zero;
                
                ParticleSystem particleSystem = particleObj.AddComponent<ParticleSystem>();
                SetupParticleSystem(particleSystem);
            }
        }
        
        /// <summary>
        /// スポーンポイントをセットアップ
        /// </summary>
        void SetupSpawnPoint(BlackHoleEmitterVisual emitterVisual)
        {
            Transform spawnPoint = transform.Find("ObjectSpawnPoint");
            if (spawnPoint == null)
            {
                GameObject spawnObj = new GameObject("ObjectSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.zero;
                spawnPoint = spawnObj.transform;
            }
            
            // BlackHoleEmitterVisualのobjectSpawnPointを設定
            emitterVisual.objectSpawnPoint = spawnPoint;
        }
        
        /// <summary>
        /// パーティクルシステムをセットアップ
        /// </summary>
        void SetupParticleSystem(ParticleSystem particleSystem)
        {
            var main = particleSystem.main;
            main.startLifetime = bubbleLifetime;
            main.startSpeed = bubbleSpeed;
            main.startSize = bubbleSize;
            main.startColor = bubbleColor;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            var emission = particleSystem.emission;
            emission.enabled = enableBubbleEmission;
            emission.rateOverTime = bubbleEmissionRate;
            
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = holeSize * 0.3f;
            
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(bubbleSpeed);
            
            // マテリアルを設定
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
            }
        }
        
        /// <summary>
        /// ブラックホールスプライトを作成
        /// </summary>
        Sprite CreateBlackHoleSprite()
        {
            int textureSize = 128;
            Texture2D texture = new Texture2D(textureSize, textureSize);
            Color[] pixels = new Color[textureSize * textureSize];
            
            Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
            float maxRadius = textureSize * 0.4f;
            
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDistance = Mathf.Clamp01(distance / maxRadius);
                    
                    float fadeValue = Mathf.Lerp(1f, 0f, normalizedDistance);
                    Color pixelColor = Color.Lerp(holeCenterColor, holeColor, fadeValue);
                    pixelColor.a *= fadeValue;
                    
                    pixels[y * textureSize + x] = pixelColor;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
        }
        
        void OnDrawGizmos()
        {
            // ホールのサイズをシーンビューで表示
            Gizmos.color = holeColor;
            Gizmos.DrawWireSphere(transform.position, holeSize * 0.5f);
            
            // スポーンポイントを表示
            Transform spawnPoint = transform.Find("ObjectSpawnPoint");
            if (spawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.2f);
            }
        }
    }
}
