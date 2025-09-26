using UnityEngine;
using System.Collections;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// 排出口のビジュアル制御
    /// </summary>
    public class EmitterVisual : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer organRenderer;
        [SerializeField] private ParticleSystem emissionEffect;
        [SerializeField] private AudioSource emissionSound;
        
        [Header("Organ Appearance")]
        [SerializeField] private Color organBaseColor = Color.red;
        [SerializeField] private Color organPulseColor = Color.white;
        [SerializeField] private float organSize = 1f;
        [SerializeField] private float generationTime = 2f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.3f;
        
        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Object Appearance")]
        // これらのフィールドは将来の拡張用に予約
        // [SerializeField] private float objectFadeInDuration = 0.5f;
        // [SerializeField] private float objectScaleUpDuration = 0.8f;
        [SerializeField] private AnimationCurve objectScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.7f, 1.1f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] private AnimationCurve objectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // 内部変数
        private bool isGenerating = false;
        private bool canGenerate = true;
        private Coroutine generationSequence;
        private Vector3 originalScale;
        private Color originalColor;
        
        void Awake()
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
            
            // OrganRendererの自動設定
            if (organRenderer == null)
            {
                organRenderer = visualRoot.GetComponent<SpriteRenderer>();
                if (organRenderer == null)
                {
                    GameObject organObj = new GameObject("Organ");
                    organObj.transform.SetParent(visualRoot);
                    organObj.transform.localPosition = Vector3.zero;
                    organObj.transform.localRotation = Quaternion.identity;
                    organObj.transform.localScale = Vector3.one;
                    
                    organRenderer = organObj.AddComponent<SpriteRenderer>();
                    // デフォルトスプライトを設定（必要に応じて）
                    organRenderer.sprite = CreateDefaultOrganSprite();
                }
            }
            
            // 初期状態を保存
            if (organRenderer != null)
            {
                originalScale = organRenderer.transform.localScale;
                originalColor = organRenderer.color;
            }
            
            // 初期状態を非表示に設定
            SetOrganVisible(false);
        }
        
        /// <summary>
        /// デフォルトのオーガンスプライトを作成
        /// </summary>
        private Sprite CreateDefaultOrganSprite()
        {
            // 簡単な円形スプライトを作成
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            
            Vector2 center = new Vector2(32, 32);
            float radius = 30f;
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 生成開始
        /// </summary>
        public void StartGeneration()
        {
            if (!canGenerate || isGenerating)
            {
                Debug.LogWarning($"[EmitterVisual] Cannot start generation - canGenerate: {canGenerate}, isGenerating: {isGenerating}");
                return;
            }
            
            if (generationSequence != null)
            {
                StopCoroutine(generationSequence);
            }
            
            generationSequence = StartCoroutine(GenerationSequence());
        }
        
        /// <summary>
        /// 生成シーケンス
        /// </summary>
        private IEnumerator GenerationSequence()
        {
            isGenerating = true;
            canGenerate = false;
            
            try
            {
                Debug.Log($"[EmitterVisual] Starting generation sequence on {gameObject.name}");
                
                // 1. オーガンの表示とアニメーション
                ShowOrgan();
                yield return StartCoroutine(AnimateOrgan());
                
                // 2. 生成完了まで待機
                yield return new WaitForSeconds(generationTime);
                
                // 3. オーガンをリセット
                HideOrgan();
                
                // 4. クールダウン
                yield return new WaitForSeconds(0.5f);
            }
            finally
            {
                isGenerating = false;
                canGenerate = true;
                generationSequence = null;
                Debug.Log($"[EmitterVisual] Generation sequence completed on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// オーガンを表示
        /// </summary>
        private void ShowOrgan()
        {
            if (organRenderer != null)
            {
                organRenderer.gameObject.SetActive(true);
                organRenderer.color = organBaseColor;
                organRenderer.transform.localScale = Vector3.zero;
            }
            
            // エフェクト開始
            if (emissionEffect != null)
            {
                emissionEffect.Play();
            }
            
            // サウンド再生
            if (emissionSound != null)
            {
                emissionSound.Play();
            }
        }
        
        /// <summary>
        /// オーガンを非表示
        /// </summary>
        private void HideOrgan()
        {
            if (organRenderer != null)
            {
                organRenderer.gameObject.SetActive(false);
            }
            
            // エフェクト停止
            if (emissionEffect != null)
            {
                emissionEffect.Stop();
            }
        }
        
        /// <summary>
        /// オーガンのアニメーション
        /// </summary>
        private IEnumerator AnimateOrgan()
        {
            if (organRenderer == null) yield break;
            
            float elapsedTime = 0f;
            float duration = generationTime;
            
            while (elapsedTime < duration)
            {
                float progress = elapsedTime / duration;
                
                // スケールアニメーション
                float scaleValue = scaleCurve.Evaluate(progress) * organSize;
                organRenderer.transform.localScale = Vector3.one * scaleValue;
                
                // フェードアニメーション
                float fadeValue = fadeCurve.Evaluate(progress);
                Color currentColor = Color.Lerp(organBaseColor, organPulseColor, fadeValue);
                organRenderer.color = currentColor;
                
                // パルス効果
                float pulseValue = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
                organRenderer.color = Color.Lerp(currentColor, organPulseColor, pulseValue);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 最終状態を設定
            organRenderer.transform.localScale = Vector3.one * organSize;
            organRenderer.color = organBaseColor;
        }
        
        /// <summary>
        /// オーガンの表示状態を設定
        /// </summary>
        private void SetOrganVisible(bool visible)
        {
            if (organRenderer != null)
            {
                organRenderer.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 生成可能かどうか
        /// </summary>
        public bool CanGenerate => canGenerate && !isGenerating;
        
        /// <summary>
        /// 生成中かどうか
        /// </summary>
        public bool IsGenerating => isGenerating;
        
        void OnDestroy()
        {
            if (generationSequence != null)
            {
                StopCoroutine(generationSequence);
            }
        }
    }
}
