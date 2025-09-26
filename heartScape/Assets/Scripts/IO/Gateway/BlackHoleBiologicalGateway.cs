using UnityEngine;
using System.Collections;
using HeartScape.IO.Gateway;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// ブラックホール風生物学的オーガンゲートウェイ
    /// 常時表示されるブラックホールと泡の噴出、オブジェクト出現アニメーション
    /// </summary>
    public class BlackHoleBiologicalGateway : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeartManager heartManager;
        [SerializeField] private HeartGateway heartGateway;
        
        [Header("Black Hole Emitter Visuals")]
        [SerializeField] private BlackHoleEmitterVisual[] emitterVisuals;
        
        [Header("Timing Settings")]
        [SerializeField] private float preGenerationDelay = 0.2f;
        [SerializeField] private float postGenerationDelay = 0.1f;
        // [SerializeField] private float objectScaleUpDuration = 0.8f; // 将来の拡張用
        [SerializeField] private float objectAppearSpeed = 5f;
        
        [Header("Object Appearance")]
        [SerializeField] private AnimationCurve objectScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.3f, 1.2f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] private AnimationCurve objectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 0.2f, 1f);
        
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
                    Debug.LogError("[BlackHoleBiologicalGateway] HeartGateway component not found on this GameObject!");
                }
            }
            
            if (heartManager == null)
            {
                heartManager = FindFirstObjectByType<HeartManager>();
                if (heartManager == null)
                {
                    Debug.LogError("[BlackHoleBiologicalGateway] HeartManager not found in scene!");
                }
            }
            
            // BlackHoleEmitterVisualsの自動設定
            SetupEmitterVisuals();
            
            // 初期状態を設定
            canGenerate = true;
            
            Debug.Log($"[BlackHoleBiologicalGateway] Initialized - HeartGateway: {heartGateway != null}, HeartManager: {heartManager != null}, EmitterVisuals: {emitterVisuals?.Length ?? 0}");
        }
        
        /// <summary>
        /// BlackHoleEmitterVisualsを取得（手動配置されたものを使用）
        /// </summary>
        void SetupEmitterVisuals()
        {
            if (heartGateway == null || heartGateway.emitters == null) return;
            
            emitterVisuals = new BlackHoleEmitterVisual[heartGateway.emitters.Length];
            
            for (int i = 0; i < heartGateway.emitters.Length; i++)
            {
                var emitter = heartGateway.emitters[i];
                if (emitter == null) continue;
                
                // 既存のBlackHoleEmitterVisualコンポーネントを取得
                BlackHoleEmitterVisual emitterVisual = emitter.GetComponent<BlackHoleEmitterVisual>();
                if (emitterVisual == null)
                {
                    Debug.LogWarning($"[BlackHoleBiologicalGateway] BlackHoleEmitterVisual not found on {emitter.name}. Please add BlackHoleEmitterSetup component and run setup.");
                }
                
                emitterVisuals[i] = emitterVisual;
            }
        }
        
        /// <summary>
        /// ブラックホール風オーガン射出処理
        /// </summary>
        public void SpawnWithBlackHoleOrgan(HeartGateway.HeartInput input)
        {
            if (!canGenerate)
            {
                Debug.LogWarning($"[BlackHoleBiologicalGateway] Cannot generate - canGenerate: {canGenerate}");
                return;
            }
            
            // 既存のシーケンスを停止
            if (generationSequence != null)
            {
                StopCoroutine(generationSequence);
                generationSequence = null;
            }
            
            // 状態をリセット
            canGenerate = true;
            
            generationSequence = StartCoroutine(BlackHoleSpawnSequence(input));
        }
        
        /// <summary>
        /// ブラックホール射出シーケンス
        /// </summary>
        IEnumerator BlackHoleSpawnSequence(HeartGateway.HeartInput input)
        {
            Debug.Log($"[BlackHoleBiologicalGateway] Starting black hole spawn sequence for switch {input.switchNo}");
            
            // 生成フラグを一時的に無効化（連続スポーン防止）
            canGenerate = false;
            
            try
            {
                // 1. 生成前の待機
                if (preGenerationDelay > 0)
                {
                    yield return new WaitForSeconds(preGenerationDelay);
                }
                
                // 2. 対応するBlackHoleEmitterVisualのオブジェクト出現アニメーション開始
                int emitterIndex = Mathf.Clamp(input.switchNo - 1, 0, emitterVisuals.Length - 1);
                BlackHoleEmitterVisual targetVisual = emitterVisuals[emitterIndex];
                
                if (targetVisual != null)
                {
                    // オブジェクトを生成してアニメーション開始
                    GameObject spawnedObject = SpawnHeartObject(input);
                    if (spawnedObject != null)
                    {
                        // オブジェクトを一時的に停止
                        Rigidbody2D rb = spawnedObject.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector2.zero;
                            rb.bodyType = RigidbodyType2D.Kinematic;
                        }
                        
                        targetVisual.StartObjectAppearance(spawnedObject);
                        Debug.Log($"[BlackHoleBiologicalGateway] Object appearance animation started for {targetVisual.name}");
                        
                        // 3. オブジェクト出現完了まで待機（拡大スピードに基づく）
                        float actualDuration = 1f / objectAppearSpeed;
                        yield return new WaitForSeconds(actualDuration);
                        
                        // 4. オブジェクトを動かし始める
                        if (rb != null)
                        {
                            rb.bodyType = RigidbodyType2D.Dynamic;
                            // 速度を再設定
                            Vector2 dir = heartGateway.emitters[emitterIndex].right;
                            if (heartGateway.spreadDeg > 0f)
                            {
                                float ang = Random.Range(-heartGateway.spreadDeg, heartGateway.spreadDeg);
                                dir = (Vector2)(Quaternion.Euler(0, 0, ang) * dir);
                            }
                            float speed = Mathf.Max(0f, heartGateway.baseSpeed + Random.Range(-heartGateway.speedJitter, heartGateway.speedJitter));
                            rb.linearVelocity = dir.normalized * speed;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[BlackHoleBiologicalGateway] BlackHoleEmitterVisual for switch {input.switchNo} not available");
                }
                
                // 5. 生成後の遅延
                if (postGenerationDelay > 0)
                {
                    yield return new WaitForSeconds(postGenerationDelay);
                }
                
                Debug.Log($"[BlackHoleBiologicalGateway] Heart object spawned from switch {input.switchNo}");
                
                // 6. 短いクールダウン
                yield return new WaitForSeconds(0.1f);
            }
            finally
            {
                // 生成フラグを即座に有効化（連続スポーン可能）
                canGenerate = true;
                generationSequence = null;
                Debug.Log($"[BlackHoleBiologicalGateway] Spawn sequence completed for switch {input.switchNo}");
            }
        }
        
        /// <summary>
        /// ハートオブジェクトを射出（速度なしで生成）
        /// </summary>
        GameObject SpawnHeartObject(HeartGateway.HeartInput input)
        {
            if (heartManager == null || heartGateway == null)
            {
                Debug.LogError("[BlackHoleBiologicalGateway] HeartManager or HeartGateway not found");
                return null;
            }
            
            // エミッターを取得
            int idx = Mathf.Clamp(input.switchNo - 1, 0, heartGateway.emitters.Length - 1);
            var emitter = heartGateway.emitters[idx];
            
            if (emitter == null)
            {
                Debug.LogWarning($"[BlackHoleBiologicalGateway] Emitter for switch {input.switchNo} missing");
                return null;
            }
            
            // プロファイルを決定（HeartGatewayのロジックを使用）
            HeartProfile hp = DetermineProfile(input);
            
            // 射出位置（速度は0で生成）
            Vector2 pos = emitter.position;
            Vector2 vel = Vector2.zero; // 速度を0に設定
            
            // ハートオブジェクトを生成
            HeartAgent spawnedAgent = heartManager.Spawn(hp, pos, vel);
            
            if (spawnedAgent != null)
            {
                heartGateway.ApplyInputOverrides(input, spawnedAgent);
                return spawnedAgent.gameObject;
            }
            
            return null;
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
                    : $"BHOLE_{System.Guid.NewGuid():N}".Substring(0, 8);
                
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
        /// 外部から射出を開始
        /// </summary>
        public void TriggerSpawn(HeartGateway.HeartInput input)
        {
            Debug.Log($"[BlackHoleBiologicalGateway] TriggerSpawn called for switch {input.switchNo}");
            Debug.Log($"[BlackHoleBiologicalGateway] canGenerate: {canGenerate}, emitterVisuals length: {emitterVisuals?.Length ?? 0}");
            
            if (emitterVisuals != null)
            {
                for (int i = 0; i < emitterVisuals.Length; i++)
                {
                    Debug.Log($"[BlackHoleBiologicalGateway] EmitterVisual[{i}]: {(emitterVisuals[i] != null ? emitterVisuals[i].name : "NULL")}");
                }
            }
            
            SpawnWithBlackHoleOrgan(input);
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
        
        /// <summary>
        /// 手動テスト用の射出メソッド
        /// </summary>
        [ContextMenu("Test Spawn Switch 1")]
        public void TestSpawnSwitch1()
        {
            var testInput = new HeartGateway.HeartInput
            {
                uid = "TEST_1",
                switchNo = 1,
                hr = 80f,
                cv = 0.1f,
                range = 20f,
                mean = 80f
            };
            TriggerSpawn(testInput);
        }
        
        /// <summary>
        /// 手動テスト用の射出メソッド
        /// </summary>
        [ContextMenu("Test Spawn Switch 2")]
        public void TestSpawnSwitch2()
        {
            var testInput = new HeartGateway.HeartInput
            {
                uid = "TEST_2",
                switchNo = 2,
                hr = 90f,
                cv = 0.15f,
                range = 25f,
                mean = 90f
            };
            TriggerSpawn(testInput);
        }
        
        /// <summary>
        /// ブラックホール設定を更新
        /// </summary>
        public void UpdateBlackHoleSettings(int emitterIndex, Color holeColor, Color centerColor, float size, float fadeRadius)
        {
            if (emitterIndex >= 0 && emitterIndex < emitterVisuals.Length && emitterVisuals[emitterIndex] != null)
            {
                emitterVisuals[emitterIndex].UpdateHoleSettings(holeColor, centerColor, size, fadeRadius);
            }
        }
        
        /// <summary>
        /// 泡設定を更新
        /// </summary>
        public void UpdateBubbleSettings(int emitterIndex, float emissionRate, float lifetime, float size, float speed, Color color)
        {
            if (emitterIndex >= 0 && emitterIndex < emitterVisuals.Length && emitterVisuals[emitterIndex] != null)
            {
                emitterVisuals[emitterIndex].UpdateBubbleSettings(emissionRate, lifetime, size, speed, color);
            }
        }
        
        /// <summary>
        /// オブジェクト出現設定を更新
        /// </summary>
        public void UpdateObjectAppearanceSettings(int emitterIndex, float appearSpeed, float maxScale)
        {
            if (emitterIndex >= 0 && emitterIndex < emitterVisuals.Length && emitterVisuals[emitterIndex] != null)
            {
                emitterVisuals[emitterIndex].UpdateObjectAppearanceSettings(appearSpeed, maxScale);
            }
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
                    
                    // ブラックホールの表示
                    Gizmos.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                    Gizmos.DrawWireSphere(emitter.position, 0.5f);
                    
                    // 射出方向の表示
                    Gizmos.color = Color.cyan;
                    Vector3 direction = emitter.right * 1f;
                    Gizmos.DrawLine(emitter.position, emitter.position + direction);
                }
            }
        }
    }
}
