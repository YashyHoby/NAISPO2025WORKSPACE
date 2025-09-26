using UnityEngine;
using System.Collections.Generic;
using HeartScape.IO.Gateway;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// ブラックホールゲートウェイ統合管理システム
    /// すべてのEmitterの設定を一括管理し、リアルタイムで反映
    /// </summary>
    public class BlackHoleGatewayManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeartManager heartManager;
        [SerializeField] private HeartGateway heartGateway;
        [SerializeField] private BlackHoleBiologicalGateway biologicalGateway;
        
        [Header("Global Black Hole Settings")]
        [SerializeField] public Color globalHoleColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] public Color globalHoleCenterColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] public float globalHoleSize = 1f;
        [SerializeField] public float globalHoleFadeRadius = 0.8f;
        [SerializeField] public AnimationCurve globalHoleFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("Global Bubble Settings")]
        [SerializeField] public bool globalEnableBubbleEmission = true;
        [SerializeField] public float globalBubbleEmissionRate = 2f;
        [SerializeField] public float globalBubbleLifetime = 3f;
        [SerializeField] public float globalBubbleSize = 0.1f;
        [SerializeField] public float globalBubbleSpeed = 0.5f;
        [SerializeField] public Color globalBubbleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        [SerializeField] public AnimationCurve globalBubbleFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] public AnimationCurve globalBubbleScaleCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f);
        
        [Header("Global Bubble Direction Settings")]
        [SerializeField] public bool globalUseEmitterDirection = true;
        [SerializeField] public float globalBubbleDirectionStrength = 1f;
        [SerializeField] public float globalBubbleSpreadAngle = 30f;
        
        [Header("Global Object Appearance Settings")]
        [SerializeField] public float globalObjectAppearSpeed = 5f;
        [SerializeField] public float globalObjectMaxScale = 1.2f;
        [SerializeField] public AnimationCurve globalObjectScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.3f, 1.2f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] public AnimationCurve globalObjectFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 0.2f, 1f);
        
        [Header("Individual Emitter Settings")]
        [SerializeField] public bool enableIndividualSettings = false;
        [SerializeField] public List<EmitterSettings> individualEmitterSettings = new List<EmitterSettings>();
        
        [Header("Debug")]
        [SerializeField] public bool enableDebugLogs = true;
        [SerializeField] public bool autoApplyChanges = true;
        
        // 内部変数
        private BlackHoleEmitterVisual[] emitterVisuals;
        private bool isInitialized = false;
        
        [System.Serializable]
        public class EmitterSettings
        {
            [Header("Emitter A Settings")]
            public Color holeColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            public Color holeCenterColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            public float holeSize = 1f;
            public float holeFadeRadius = 0.8f;
            public bool enableBubbleEmission = true;
            public float bubbleEmissionRate = 2f;
            public float bubbleLifetime = 3f;
            public float bubbleSize = 0.1f;
            public float bubbleSpeed = 0.5f;
            public Color bubbleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            public bool useEmitterDirection = true;
            public float bubbleDirectionStrength = 1f;
            public float bubbleSpreadAngle = 30f;
            public float objectAppearSpeed = 5f;
            public float objectMaxScale = 1.2f;
        }
        
        void Awake()
        {
            InitializeManager();
        }
        
        void Start()
        {
            SetupAllEmitters();
        }
        
        void Update()
        {
            if (autoApplyChanges && isInitialized && Time.frameCount % 30 == 0) // 30フレームに1回のみ更新
            {
                ApplyGlobalSettings();
            }
        }
        
        /// <summary>
        /// マネージャーの初期化
        /// </summary>
        void InitializeManager()
        {
            // 参照の自動取得
            if (heartGateway == null)
            {
                heartGateway = GetComponent<HeartGateway>();
                if (heartGateway == null)
                {
                    Debug.LogError("[BlackHoleGatewayManager] HeartGateway component not found!");
                    return;
                }
            }
            
            if (heartManager == null)
            {
                heartManager = FindFirstObjectByType<HeartManager>();
                if (heartManager == null)
                {
                    Debug.LogError("[BlackHoleGatewayManager] HeartManager not found in scene!");
                    return;
                }
            }
            
            if (biologicalGateway == null)
            {
                biologicalGateway = GetComponent<BlackHoleBiologicalGateway>();
                if (biologicalGateway == null)
                {
                    biologicalGateway = gameObject.AddComponent<BlackHoleBiologicalGateway>();
                    Debug.Log("[BlackHoleGatewayManager] BlackHoleBiologicalGateway added automatically");
                }
            }
            
            isInitialized = true;
            Debug.Log("[BlackHoleGatewayManager] Manager initialized successfully");
        }
        
        /// <summary>
        /// すべてのEmitterをセットアップ
        /// </summary>
        void SetupAllEmitters()
        {
            if (heartGateway == null || heartGateway.emitters == null) return;
            
            emitterVisuals = new BlackHoleEmitterVisual[heartGateway.emitters.Length];
            
            for (int i = 0; i < heartGateway.emitters.Length; i++)
            {
                var emitter = heartGateway.emitters[i];
                if (emitter == null) continue;
                
                // BlackHoleEmitterVisualを取得または追加
                BlackHoleEmitterVisual emitterVisual = emitter.GetComponent<BlackHoleEmitterVisual>();
                if (emitterVisual == null)
                {
                    emitterVisual = emitter.gameObject.AddComponent<BlackHoleEmitterVisual>();
                    Debug.Log($"[BlackHoleGatewayManager] BlackHoleEmitterVisual added to {emitter.name}");
                }
                
                emitterVisuals[i] = emitterVisual;
                
                // 個別設定がある場合は適用
                if (enableIndividualSettings && i < individualEmitterSettings.Count)
                {
                    ApplyIndividualSettings(emitterVisual, individualEmitterSettings[i]);
                }
                else
                {
                    ApplyGlobalSettingsToEmitter(emitterVisual);
                }
            }
            
            Debug.Log($"[BlackHoleGatewayManager] Setup completed for {emitterVisuals.Length} emitters");
        }
        
        /// <summary>
        /// グローバル設定をすべてのEmitterに適用
        /// </summary>
        public void ApplyGlobalSettings()
        {
            if (emitterVisuals == null) return;
            
            for (int i = 0; i < emitterVisuals.Length; i++)
            {
                if (emitterVisuals[i] != null)
                {
                    if (enableIndividualSettings && i < individualEmitterSettings.Count)
                    {
                        ApplyIndividualSettings(emitterVisuals[i], individualEmitterSettings[i]);
                    }
                    else
                    {
                        ApplyGlobalSettingsToEmitter(emitterVisuals[i]);
                    }
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log("[BlackHoleGatewayManager] Global settings applied to all emitters");
            }
        }
        
        /// <summary>
        /// グローバル設定を単一Emitterに適用
        /// </summary>
        void ApplyGlobalSettingsToEmitter(BlackHoleEmitterVisual emitterVisual)
        {
            if (emitterVisual == null) return;
            
            // ブラックホール設定
            emitterVisual.holeColor = globalHoleColor;
            emitterVisual.holeCenterColor = globalHoleCenterColor;
            emitterVisual.holeSize = globalHoleSize;
            emitterVisual.holeFadeRadius = globalHoleFadeRadius;
            emitterVisual.holeFadeCurve = globalHoleFadeCurve;
            
            // バブル設定
            emitterVisual.enableBubbleEmission = globalEnableBubbleEmission;
            emitterVisual.bubbleEmissionRate = globalBubbleEmissionRate;
            emitterVisual.bubbleLifetime = globalBubbleLifetime;
            emitterVisual.bubbleSize = globalBubbleSize;
            emitterVisual.bubbleSpeed = globalBubbleSpeed;
            emitterVisual.bubbleColor = globalBubbleColor;
            emitterVisual.bubbleFadeCurve = globalBubbleFadeCurve;
            emitterVisual.bubbleScaleCurve = globalBubbleScaleCurve;
            
            // バブル方向設定
            emitterVisual.useEmitterDirection = globalUseEmitterDirection;
            emitterVisual.bubbleDirectionStrength = globalBubbleDirectionStrength;
            emitterVisual.bubbleSpreadAngle = globalBubbleSpreadAngle;
            
            // オブジェクト出現設定
            emitterVisual.objectAppearSpeed = globalObjectAppearSpeed;
            emitterVisual.objectMaxScale = globalObjectMaxScale;
            emitterVisual.objectScaleCurve = globalObjectScaleCurve;
            emitterVisual.objectFadeCurve = globalObjectFadeCurve;
            
            // ビジュアルを更新
            emitterVisual.UpdateHoleVisual();
            emitterVisual.UpdateBubbleEmission();
            emitterVisual.UpdateBubbleDirection();
        }
        
        /// <summary>
        /// 個別設定をEmitterに適用
        /// </summary>
        void ApplyIndividualSettings(BlackHoleEmitterVisual emitterVisual, EmitterSettings settings)
        {
            if (emitterVisual == null || settings == null) return;
            
            // ブラックホール設定
            emitterVisual.holeColor = settings.holeColor;
            emitterVisual.holeCenterColor = settings.holeCenterColor;
            emitterVisual.holeSize = settings.holeSize;
            emitterVisual.holeFadeRadius = settings.holeFadeRadius;
            
            // バブル設定
            emitterVisual.enableBubbleEmission = settings.enableBubbleEmission;
            emitterVisual.bubbleEmissionRate = settings.bubbleEmissionRate;
            emitterVisual.bubbleLifetime = settings.bubbleLifetime;
            emitterVisual.bubbleSize = settings.bubbleSize;
            emitterVisual.bubbleSpeed = settings.bubbleSpeed;
            emitterVisual.bubbleColor = settings.bubbleColor;
            
            // バブル方向設定
            emitterVisual.useEmitterDirection = settings.useEmitterDirection;
            emitterVisual.bubbleDirectionStrength = settings.bubbleDirectionStrength;
            emitterVisual.bubbleSpreadAngle = settings.bubbleSpreadAngle;
            
            // オブジェクト出現設定
            emitterVisual.objectAppearSpeed = settings.objectAppearSpeed;
            emitterVisual.objectMaxScale = settings.objectMaxScale;
            
            // ビジュアルを更新
            emitterVisual.UpdateHoleVisual();
            emitterVisual.UpdateBubbleEmission();
            emitterVisual.UpdateBubbleDirection();
        }
        
        /// <summary>
        /// 手動で設定を適用
        /// </summary>
        [ContextMenu("Apply All Settings")]
        public void ManualApplySettings()
        {
            ApplyGlobalSettings();
        }
        
        /// <summary>
        /// すべてのEmitterをリセット
        /// </summary>
        [ContextMenu("Reset All Emitters")]
        public void ResetAllEmitters()
        {
            SetupAllEmitters();
            ApplyGlobalSettings();
        }
        
        /// <summary>
        /// デバッグ情報を表示
        /// </summary>
        [ContextMenu("Debug Status")]
        public void DebugStatus()
        {
            Debug.Log($"[BlackHoleGatewayManager] Status:");
            Debug.Log($"  Initialized: {isInitialized}");
            Debug.Log($"  EmitterVisuals: {(emitterVisuals != null ? emitterVisuals.Length : 0)}");
            Debug.Log($"  Individual Settings: {enableIndividualSettings}");
            Debug.Log($"  Auto Apply: {autoApplyChanges}");
            
            if (emitterVisuals != null)
            {
                for (int i = 0; i < emitterVisuals.Length; i++)
                {
                    Debug.Log($"  Emitter[{i}]: {(emitterVisuals[i] != null ? emitterVisuals[i].name : "NULL")}");
                }
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
                    Gizmos.color = globalHoleColor;
                    Gizmos.DrawWireSphere(emitter.position, globalHoleSize * 0.5f);
                    
                    // 射出方向の表示
                    Gizmos.color = Color.cyan;
                    Vector3 direction = emitter.right * 1f;
                    Gizmos.DrawLine(emitter.position, emitter.position + direction);
                }
            }
        }
    }
}
