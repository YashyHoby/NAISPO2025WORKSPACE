using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// ブラックホール風排出口ビジュアル
    /// 常時表示される穴と泡の噴出、オブジェクト出現アニメーション
    /// </summary>
    public class BlackHoleEmitterVisual : MonoBehaviour
    {
        [Header("Black Hole Appearance")]
        [SerializeField] public Color holeColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] public Color holeCenterColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] public float holeSize = 1f;
        [SerializeField] public float holeFadeRadius = 0.8f;
        [SerializeField] public AnimationCurve holeFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("Bubble Emission")]
        [SerializeField] public bool enableBubbleEmission = true;
        [SerializeField] public float bubbleEmissionRate = 2f;
        [SerializeField] public float bubbleLifetime = 3f;
        [SerializeField] public float bubbleSize = 0.1f;
        [SerializeField] public float bubbleSpeed = 0.5f;
        [SerializeField] public Color bubbleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        [SerializeField] public AnimationCurve bubbleFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] public AnimationCurve bubbleScaleCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f);
        
        [Header("Bubble Direction")]
        [SerializeField] public bool useEmitterDirection = true;
        [SerializeField] public float bubbleDirectionStrength = 1f;
        [SerializeField] public float bubbleSpreadAngle = 30f;
        
        [Header("Object Appearance")]
        [SerializeField] public float objectAppearSpeed = 5f;
        [SerializeField] public float objectMaxScale = 1.2f;
        [SerializeField] public AnimationCurve objectScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.3f, 1.2f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] public AnimationCurve objectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 0.2f, 1f);
        
        // デバッグ用
        [Header("Debug")]
        [SerializeField] public bool enableDebugLogs = true;
        
        [Header("Visual Components")]
        [SerializeField] public Transform visualRoot;
        [SerializeField] private SpriteRenderer holeRenderer;
        [SerializeField] private ParticleSystem bubbleParticleSystem;
        [SerializeField] public Transform objectSpawnPoint;
        
        // 内部変数
        // private bool isObjectAppearing = false; // 将来の拡張用
        private Coroutine objectAppearCoroutine;
        private List<BubbleData> activeBubbles = new List<BubbleData>();
        // private float bubbleTimer = 0f; // 将来の拡張用
        
        // 泡データ構造
        [System.Serializable]
        private class BubbleData
        {
            public GameObject bubbleObj;
            public SpriteRenderer renderer;
            public Vector3 startPosition;
            public Vector3 direction;
            public float lifetime;
            public float maxLifetime;
            public float speed;
        }
        
        void Awake()
        {
            SetupVisualComponents();
            SetupBubbleParticleSystem();
            
            // アニメーションカーブの初期化
            if (objectScaleCurve == null || objectScaleCurve.keys.Length == 0)
            {
                objectScaleCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.3f, 1.2f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            }
            
            if (objectFadeCurve == null || objectFadeCurve.keys.Length == 0)
            {
                objectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 0.2f, 1f);
            }
        }
        
        void Start()
        {
            UpdateHoleVisual();
        }
        
        void Update()
        {
            if (enableBubbleEmission)
            {
                UpdateBubbleEmission();
                
                // Emitterの回転に合わせてバブル方向を更新（フレームレート制限）
                if (useEmitterDirection && Time.frameCount % 30 == 0) // 30フレームに1回のみ更新
                {
                    UpdateBubbleDirection();
                }
            }
            UpdateBubbles();
        }
        
        /// <summary>
        /// ビジュアルコンポーネントの設定
        /// </summary>
        void SetupVisualComponents()
        {
            // VisualRootの自動設定
            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot");
                if (visualRoot == null)
                {
                    GameObject visualRootObj = new GameObject("VisualRoot");
                    visualRoot = visualRootObj.transform;
                    visualRoot.SetParent(transform);
                    visualRoot.localPosition = Vector3.zero;
                    visualRoot.localRotation = Quaternion.identity;
                    visualRoot.localScale = Vector3.one;
                }
            }
            
            // ブラックホールレンダラーの設定
            if (holeRenderer == null)
            {
                GameObject holeObj = new GameObject("BlackHole");
                holeObj.transform.SetParent(visualRoot);
                holeObj.transform.localPosition = Vector3.zero;
                holeObj.transform.localRotation = Quaternion.identity;
                holeObj.transform.localScale = Vector3.one;
                
                holeRenderer = holeObj.AddComponent<SpriteRenderer>();
                holeRenderer.sprite = CreateBlackHoleSprite();
                holeRenderer.sortingOrder = -1; // 心オブジェクトより後ろに描画
            }
            
            // オブジェクトスポーンポイントの設定
            if (objectSpawnPoint == null)
            {
                GameObject spawnObj = new GameObject("ObjectSpawnPoint");
                spawnObj.transform.SetParent(visualRoot);
                spawnObj.transform.localPosition = Vector3.zero;
                objectSpawnPoint = spawnObj.transform;
            }
        }
        
        /// <summary>
        /// ブラックホールスプライトの作成
        /// </summary>
        private Sprite CreateBlackHoleSprite()
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
                    
                    float fadeValue = holeFadeCurve.Evaluate(normalizedDistance);
                    Color pixelColor = Color.Lerp(holeCenterColor, holeColor, fadeValue);
                    pixelColor.a *= fadeValue;
                    
                    pixels[y * textureSize + x] = pixelColor;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 泡パーティクルシステムの設定
        /// </summary>
        void SetupBubbleParticleSystem()
        {
            if (bubbleParticleSystem == null)
            {
                GameObject particleObj = new GameObject("BubbleParticles");
                particleObj.transform.SetParent(visualRoot);
                particleObj.transform.localPosition = Vector3.zero;
                
                bubbleParticleSystem = particleObj.AddComponent<ParticleSystem>();
            }
            
            var main = bubbleParticleSystem.main;
            main.startLifetime = bubbleLifetime;
            main.startSpeed = bubbleSpeed;
            main.startSize = bubbleSize;
            main.startColor = bubbleColor;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            // マテリアルを設定して色を正しく表示
            var renderer = bubbleParticleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // デフォルトマテリアルを使用
                renderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            var emission = bubbleParticleSystem.emission;
            emission.enabled = enableBubbleEmission;
            emission.rateOverTime = bubbleEmissionRate;
            
            var shape = bubbleParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = holeSize * 0.3f;
            
            // velocityOverLifetimeを完全に無効化（エラー回避のため）
            var velocityOverLifetime = bubbleParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = false;
            
            // startSpeedは既に上で設定済み
            
            // サイズと色の設定を簡素化
            var sizeOverLifetime = bubbleParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = false; // 簡素化のため無効化
            
            var colorOverLifetime = bubbleParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false; // 簡素化のため無効化
        }
        
        // 前回の方向を記録（不要な更新を避けるため）
        private Vector2 lastEmitterDirection = Vector2.zero;
        private bool directionInitialized = false;
        
        /// <summary>
        /// バブルの方向制御を更新（velocityOverLifetimeを使わない方法）
        /// </summary>
        public void UpdateBubbleDirection()
        {
            if (bubbleParticleSystem == null) return;
            
            Vector2 currentDirection = transform.right;
            
            // 方向が変わった場合のみ更新
            if (!directionInitialized || Vector2.Distance(currentDirection, lastEmitterDirection) > 0.01f)
            {
                var main = bubbleParticleSystem.main;
                var shape = bubbleParticleSystem.shape;
                
                if (useEmitterDirection)
                {
                    // Emitterの向きに基づいてバブルの方向を設定
                    Vector2 emitterDirection = currentDirection;
                    
                    // 方向の強度を適用
                    float directionalSpeed = bubbleSpeed * bubbleDirectionStrength;
                    
                    // mainモジュールで速度を設定
                    main.startSpeed = directionalSpeed;
                    
                    // shapeモジュールで方向を設定
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = bubbleSpreadAngle;
                    shape.rotation = new Vector3(0, 0, Mathf.Atan2(emitterDirection.y, emitterDirection.x) * Mathf.Rad2Deg);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[BlackHoleEmitterVisual] Bubble direction updated: {emitterDirection} (strength: {bubbleDirectionStrength})");
                    }
                }
                else
                {
                    // 従来の放射状の方向
                    main.startSpeed = bubbleSpeed;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = holeSize * 0.3f;
                }
                
                lastEmitterDirection = currentDirection;
                directionInitialized = true;
            }
        }
        
        
        /// <summary>
        /// ブラックホールビジュアルの更新
        /// </summary>
        public void UpdateHoleVisual()
        {
            if (holeRenderer != null)
            {
                holeRenderer.transform.localScale = Vector3.one * holeSize;
                holeRenderer.color = holeColor;
                
                // 描画順序を心オブジェクトより後ろに設定
                holeRenderer.sortingOrder = -1;
            }
        }
        
        /// <summary>
        /// 泡の噴出更新
        /// </summary>
        public void UpdateBubbleEmission()
        {
            if (bubbleParticleSystem != null)
            {
                var emission = bubbleParticleSystem.emission;
                emission.enabled = enableBubbleEmission;
                emission.rateOverTime = bubbleEmissionRate;
                
                var main = bubbleParticleSystem.main;
                main.startLifetime = bubbleLifetime;
                main.startSpeed = bubbleSpeed;
                main.startSize = bubbleSize;
                main.startColor = bubbleColor;
            }
        }
        
        /// <summary>
        /// 泡の更新
        /// </summary>
        void UpdateBubbles()
        {
            for (int i = activeBubbles.Count - 1; i >= 0; i--)
            {
                var bubble = activeBubbles[i];
                bubble.lifetime += Time.deltaTime;
                
                if (bubble.lifetime >= bubble.maxLifetime)
                {
                    if (bubble.bubbleObj != null)
                    {
                        Destroy(bubble.bubbleObj);
                    }
                    activeBubbles.RemoveAt(i);
                    continue;
                }
                
                // 泡の移動
                if (bubble.bubbleObj != null)
                {
                    bubble.bubbleObj.transform.position += bubble.direction * bubble.speed * Time.deltaTime;
                    
                    // フェードとスケールの更新
                    float progress = bubble.lifetime / bubble.maxLifetime;
                    float fadeValue = bubbleFadeCurve.Evaluate(progress);
                    float scaleValue = bubbleScaleCurve.Evaluate(progress);
                    
                    if (bubble.renderer != null)
                    {
                        Color color = bubbleColor;
                        color.a *= fadeValue;
                        bubble.renderer.color = color;
                        bubble.bubbleObj.transform.localScale = Vector3.one * scaleValue * bubbleSize;
                    }
                }
            }
        }
        
        /// <summary>
        /// オブジェクト出現アニメーション開始
        /// </summary>
        public void StartObjectAppearance(GameObject targetObject)
        {
            if (targetObject == null) return;
            
            // 既存のアニメーションを停止
            if (objectAppearCoroutine != null)
            {
                StopCoroutine(objectAppearCoroutine);
                objectAppearCoroutine = null;
            }
            
            // 状態をリセット
            // isObjectAppearing = false; // 将来の拡張用
            
            objectAppearCoroutine = StartCoroutine(ObjectAppearAnimation(targetObject));
        }
        
        /// <summary>
        /// オブジェクト出現アニメーション
        /// </summary>
        IEnumerator ObjectAppearAnimation(GameObject targetObject)
        {
            // isObjectAppearing = true; // 将来の拡張用
            
            // 初期状態を設定
            Vector3 originalScale = targetObject.transform.localScale;
            
            // スポーンポイントが設定されていない場合は、このオブジェクトの位置を使用
            Vector3 spawnPosition = objectSpawnPoint != null ? objectSpawnPoint.position : transform.position;
            targetObject.transform.position = spawnPosition;
            targetObject.transform.localScale = Vector3.zero;
            
            SpriteRenderer renderer = targetObject.GetComponent<SpriteRenderer>();
            Color originalColor = renderer != null ? renderer.color : Color.white;
            Color startColor = originalColor;
            startColor.a = 0f;
            
            if (renderer != null)
            {
                renderer.color = startColor;
            }
            
            float elapsedTime = 0f;
            float duration = 1f / objectAppearSpeed;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BlackHoleEmitterVisual] Starting object appearance animation - Duration: {duration}s, Speed: {objectAppearSpeed}, SpawnPos: {spawnPosition}, OriginalScale: {originalScale}");
            }
            
            while (elapsedTime < duration)
            {
                float progress = elapsedTime / duration;
                
                // スケールアニメーション
                float scaleValue = objectScaleCurve.Evaluate(progress);
                Vector3 currentScale = Vector3.Lerp(Vector3.zero, originalScale, scaleValue);
                targetObject.transform.localScale = currentScale;
                
                // フェードアニメーション
                if (renderer != null)
                {
                    float fadeValue = objectFadeCurve.Evaluate(progress);
                    Color currentColor = Color.Lerp(startColor, originalColor, fadeValue);
                    renderer.color = currentColor;
                }
                
                // デバッグログ（0.1秒ごと）
                if (enableDebugLogs && Mathf.FloorToInt(elapsedTime * 10) % 1 == 0)
                {
                    Debug.Log($"[BlackHoleEmitterVisual] Animation Progress: {progress:F2}, Scale: {currentScale}, ScaleValue: {scaleValue:F2}");
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 最終状態を設定
            targetObject.transform.localScale = originalScale;
            if (renderer != null)
            {
                renderer.color = originalColor;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BlackHoleEmitterVisual] Object appearance animation completed - Final Scale: {originalScale}");
            }
            
            // isObjectAppearing = false; // 将来の拡張用
            objectAppearCoroutine = null;
        }
        
        /// <summary>
        /// ブラックホールの設定を更新
        /// </summary>
        public void UpdateHoleSettings(Color newHoleColor, Color newCenterColor, float newSize, float newFadeRadius)
        {
            holeColor = newHoleColor;
            holeCenterColor = newCenterColor;
            holeSize = newSize;
            holeFadeRadius = newFadeRadius;
            
            UpdateHoleVisual();
        }
        
        /// <summary>
        /// 泡の設定を更新
        /// </summary>
        public void UpdateBubbleSettings(float newEmissionRate, float newLifetime, float newSize, float newSpeed, Color newColor)
        {
            bubbleEmissionRate = newEmissionRate;
            bubbleLifetime = newLifetime;
            bubbleSize = newSize;
            bubbleSpeed = newSpeed;
            bubbleColor = newColor;
            
            UpdateBubbleEmission();
        }
        
        /// <summary>
        /// オブジェクト出現設定を更新
        /// </summary>
        public void UpdateObjectAppearanceSettings(float newAppearSpeed, float newMaxScale)
        {
            objectAppearSpeed = newAppearSpeed;
            objectMaxScale = newMaxScale;
        }
        
        void OnDestroy()
        {
            if (objectAppearCoroutine != null)
            {
                StopCoroutine(objectAppearCoroutine);
            }
            
            // アクティブな泡をクリーンアップ
            foreach (var bubble in activeBubbles)
            {
                if (bubble.bubbleObj != null)
                {
                    Destroy(bubble.bubbleObj);
                }
            }
            activeBubbles.Clear();
        }
        
        void OnDrawGizmos()
        {
            if (objectSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(objectSpawnPoint.position, 0.2f);
            }
            
            Gizmos.color = holeColor;
            Gizmos.DrawWireSphere(transform.position, holeSize * 0.5f);
        }
    }
}
