using UnityEngine;
using System.Collections;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// 生物学的オーガンゲートウェイ（新設計）
    /// EmitterVisualを使用して各排出口のビジュアルを制御
    /// </summary>
    public class BiologicalOrganGateway : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeartManager heartManager;
        [SerializeField] private HeartGateway heartGateway;
        
        [Header("Emitter Visuals")]
        [SerializeField] private EmitterVisual[] emitterVisuals;
        
        [Header("Timing Settings")]
        [SerializeField] private float preGenerationDelay = 0.5f;
        [SerializeField] private float postGenerationDelay = 0.3f;
        // [SerializeField] private float objectFadeInDuration = 0.5f; // 将来の拡張用
        [SerializeField] private float objectScaleUpDuration = 0.8f;
        
        [Header("Object Appearance")]
        [SerializeField] private AnimationCurve objectScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.7f, 1.1f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] private AnimationCurve objectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // 制御フラグ
        public bool canGenerate = true;
        
        // 内部変数
        private Coroutine generationSequence;
        
        void Awake()
        {
            // 自動で参照を取得
            if (heartGateway == null)
            {
                heartGateway = GetComponent<HeartGateway>();
                if (heartGateway == null)
                {
                    Debug.LogError("[BiologicalOrganGateway] HeartGateway component not found on this GameObject!");
                }
            }
            
            if (heartManager == null)
            {
                heartManager = FindFirstObjectByType<HeartManager>();
                if (heartManager == null)
                {
                    Debug.LogError("[BiologicalOrganGateway] HeartManager not found in scene!");
                }
            }
            
            // EmitterVisualsの自動設定
            SetupEmitterVisuals();
            
            // 初期状態を設定
            canGenerate = true;
            
            Debug.Log($"[BiologicalOrganGateway] Initialized - HeartGateway: {heartGateway != null}, HeartManager: {heartManager != null}, EmitterVisuals: {emitterVisuals?.Length ?? 0}");
        }
        
        /// <summary>
        /// EmitterVisualsを自動設定
        /// </summary>
        void SetupEmitterVisuals()
        {
            if (heartGateway == null || heartGateway.emitters == null) return;
            
            emitterVisuals = new EmitterVisual[heartGateway.emitters.Length];
            
            for (int i = 0; i < heartGateway.emitters.Length; i++)
            {
                var emitter = heartGateway.emitters[i];
                if (emitter == null) continue;
                
                // EmitterVisualコンポーネントを取得または追加
                EmitterVisual emitterVisual = emitter.GetComponent<EmitterVisual>();
                if (emitterVisual == null)
                {
                    emitterVisual = emitter.gameObject.AddComponent<EmitterVisual>();
                    Debug.Log($"[BiologicalOrganGateway] EmitterVisual added to {emitter.name}");
                }
                
                emitterVisuals[i] = emitterVisual;
            }
        }
        
        /// <summary>
        /// 生物学的オーガン射出処理
        /// </summary>
        public void SpawnWithBiologicalOrgan(HeartGateway.HeartInput input)
        {
            if (!canGenerate)
            {
                Debug.LogWarning($"[BiologicalOrganGateway] Cannot generate - canGenerate: {canGenerate}");
                return;
            }
            
            if (generationSequence != null)
            {
                StopCoroutine(generationSequence);
                generationSequence = null;
            }
            
            generationSequence = StartCoroutine(BiologicalSpawnSequence(input));
        }
        
        /// <summary>
        /// 生物学的射出シーケンス
        /// </summary>
        IEnumerator BiologicalSpawnSequence(HeartGateway.HeartInput input)
        {
            Debug.Log($"[BiologicalOrganGateway] Starting biological spawn sequence for switch {input.switchNo}");
            
            canGenerate = false;
            
            try
            {
                // 1. 生成前の待機
                if (preGenerationDelay > 0)
                {
                    yield return new WaitForSeconds(preGenerationDelay);
                }
                
                // 2. 対応するEmitterVisualの生成アニメーション開始
                int emitterIndex = Mathf.Clamp(input.switchNo - 1, 0, emitterVisuals.Length - 1);
                EmitterVisual targetVisual = emitterVisuals[emitterIndex];
                
                if (targetVisual != null && targetVisual.CanGenerate)
                {
                    targetVisual.StartGeneration();
                    Debug.Log($"[BiologicalOrganGateway] EmitterVisual generation started for {targetVisual.name}");
                }
                else
                {
                    Debug.LogWarning($"[BiologicalOrganGateway] EmitterVisual for switch {input.switchNo} not available");
                }
                
                // 3. 生成完了まで待機
                yield return new WaitForSeconds(2f); // EmitterVisualの生成時間
                
                // 4. 生成後の遅延
                if (postGenerationDelay > 0)
                {
                    yield return new WaitForSeconds(postGenerationDelay);
                }
                
                // 5. 実際のオブジェクト射出
                SpawnHeartObject(input);
                Debug.Log($"[BiologicalOrganGateway] Heart object spawned from switch {input.switchNo}");
                
                // 6. クールダウン
                yield return new WaitForSeconds(0.5f);
            }
            finally
            {
                canGenerate = true;
                generationSequence = null;
                Debug.Log($"[BiologicalOrganGateway] Spawn sequence completed for switch {input.switchNo}");
            }
        }
        
        /// <summary>
        /// ハートオブジェクトを射出
        /// </summary>
        void SpawnHeartObject(HeartGateway.HeartInput input)
        {
            if (heartManager == null || heartGateway == null)
            {
                Debug.LogError("[BiologicalOrganGateway] HeartManager or HeartGateway not found");
                return;
            }
            
            // エミッターを取得
            int idx = Mathf.Clamp(input.switchNo - 1, 0, heartGateway.emitters.Length - 1);
            var emitter = heartGateway.emitters[idx];
            
            if (emitter == null)
            {
                Debug.LogWarning($"[BiologicalOrganGateway] Emitter for switch {input.switchNo} missing");
                return;
            }
            
            // プロファイルを決定（HeartGatewayのロジックを使用）
            HeartProfile hp = DetermineProfile(input);
            
            // 射出位置・向き・初速を計算
            Vector2 pos = emitter.position;
            Vector2 dir = emitter.right;
            
            // スプレッドを適用
            if (heartGateway.spreadDeg > 0f)
            {
                float ang = Random.Range(-heartGateway.spreadDeg, heartGateway.spreadDeg);
                dir = (Vector2)(Quaternion.Euler(0, 0, ang) * dir);
            }
            
            float speed = Mathf.Max(0f, heartGateway.baseSpeed + Random.Range(-heartGateway.speedJitter, heartGateway.speedJitter));
            Vector2 vel = dir.normalized * speed;
            
            // ハートオブジェクトを生成
            HeartAgent spawnedAgent = heartManager.Spawn(hp, pos, vel);
            
            if (spawnedAgent != null)
            {
                // 生成されたオブジェクトに徐々生成エフェクトを適用
                StartCoroutine(ApplyGradualAppearance(spawnedAgent));
            }
        }
        
        /// <summary>
        /// プロファイルを決定（HeartGatewayのロジックを複製）
        /// </summary>
        HeartProfile DetermineProfile(HeartGateway.HeartInput input)
        {
            HeartProfile hp = null;
            bool hasMsgValues = !(Mathf.Approximately(input.hr, 0f) && Mathf.Approximately(input.cv, 0f)
                                  && Mathf.Approximately(input.range, 0f) && Mathf.Approximately(input.mean, 0f));
            
            if (heartGateway.preferDbIfUid && !string.IsNullOrEmpty(input.uid) && !hasMsgValues)
            {
                hp = HeartDB.Get(input.uid);
            }
            else if (hasMsgValues)
            {
                string resolvedUid = !string.IsNullOrEmpty(input.uid)
                    ? input.uid
                    : $"BIO_{System.Guid.NewGuid():N}".Substring(0, 8);
                
                hp = new HeartProfile
                {
                    uid = resolvedUid,
                    hr = input.hr,
                    cv = input.cv,
                    range = input.range,
                    mean = input.mean
                };
            }
            else if (!string.IsNullOrEmpty(input.uid))
            {
                hp = HeartDB.Get(input.uid);
            }
            else
            {
                hp = HeartDB.Get(null);
            }
            
            return hp;
        }
        
        /// <summary>
        /// 生成されたオブジェクトに徐々生成エフェクトを適用
        /// </summary>
        IEnumerator ApplyGradualAppearance(HeartAgent agent)
        {
            if (agent == null) yield break;
            
            GameObject obj = agent.gameObject;
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            
            if (renderer == null) yield break;
            
            // 初期状態を設定（小さく、透明）
            Vector3 originalScale = obj.transform.localScale;
            obj.transform.localScale = Vector3.zero;
            
            Color originalColor = renderer.color;
            Color startColor = originalColor;
            startColor.a = 0f;
            renderer.color = startColor;
            
            // 徐々に拡大・フェードイン
            float duration = objectScaleUpDuration;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                
                // スケールアニメーション
                float scaleValue = objectScaleCurve.Evaluate(t);
                obj.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, scaleValue);
                
                // フェードインアニメーション
                float fadeValue = objectFadeCurve.Evaluate(t);
                Color currentColor = Color.Lerp(startColor, originalColor, fadeValue);
                renderer.color = currentColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 最終状態を設定
            obj.transform.localScale = originalScale;
            renderer.color = originalColor;
        }
        
        /// <summary>
        /// 外部から射出を開始
        /// </summary>
        public void TriggerSpawn(HeartGateway.HeartInput input)
        {
            SpawnWithBiologicalOrgan(input);
        }
        
        /// <summary>
        /// 生成状態をリセット
        /// </summary>
        public void ResetGeneration()
        {
            if (generationSequence != null)
            {
                StopCoroutine(generationSequence);
                generationSequence = null;
            }
            
            canGenerate = true;
        }
        
        void OnDrawGizmos()
        {
            if (heartGateway != null && heartGateway.emitters != null)
            {
                // エミッターの表示
                for (int i = 0; i < heartGateway.emitters.Length; i++)
                {
                    var emitter = heartGateway.emitters[i];
                    if (emitter == null) continue;
                    
                    // 生物学的オーガンの表示
                    Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.8f);
                    Gizmos.DrawWireSphere(emitter.position, 0.3f);
                    
                    // 射出方向の表示
                    Gizmos.color = Color.green;
                    Vector3 direction = emitter.right * 0.8f;
                    Gizmos.DrawLine(emitter.position, emitter.position + direction);
                }
            }
        }
    }
}