using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HeartTrailSystem))]
public class HeartTrailSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HeartTrailSystem trailSystem = (HeartTrailSystem)target;
        
        // タイトル
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("軌跡システム", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 軌跡設定
        EditorGUILayout.LabelField("軌跡設定", EditorStyles.boldLabel);
        trailSystem.trailParticlePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Trail Particle Prefab", 
            trailSystem.trailParticlePrefab, 
            typeof(GameObject), 
            false
        );
        
        trailSystem.spawnInterval = EditorGUILayout.FloatField("Spawn Interval", trailSystem.spawnInterval);
        trailSystem.maxTrailParticles = EditorGUILayout.IntField("Max Trail Particles", trailSystem.maxTrailParticles);
        
        EditorGUILayout.Space();
        
        // 軌跡の外観
        EditorGUILayout.LabelField("軌跡の外観", EditorStyles.boldLabel);
        trailSystem.baseTrailColor = EditorGUILayout.ColorField("Base Trail Color", trailSystem.baseTrailColor);
        trailSystem.trailSizeRange = EditorGUILayout.Vector2Field("Trail Size Range", trailSystem.trailSizeRange);
        trailSystem.trailLifetime = EditorGUILayout.FloatField("Trail Lifetime", trailSystem.trailLifetime);
        
        EditorGUILayout.Space();
        
        // 液体効果
        EditorGUILayout.LabelField("液体効果", EditorStyles.boldLabel);
        trailSystem.diffusionStrength = EditorGUILayout.FloatField("Diffusion Strength", trailSystem.diffusionStrength);
        trailSystem.discretizationSpeed = EditorGUILayout.FloatField("Discretization Speed", trailSystem.discretizationSpeed);
        trailSystem.gravityEffect = EditorGUILayout.FloatField("Gravity Effect", trailSystem.gravityEffect);
        
        EditorGUILayout.Space();
        
        // パフォーマンス
        EditorGUILayout.LabelField("パフォーマンス", EditorStyles.boldLabel);
        trailSystem.minMovementDistance = EditorGUILayout.FloatField("Min Movement Distance", trailSystem.minMovementDistance);
        trailSystem.minMovementSpeed = EditorGUILayout.FloatField("Min Movement Speed", trailSystem.minMovementSpeed);
        
        EditorGUILayout.Space();
        
        // レンダリング設定
        EditorGUILayout.LabelField("レンダリング設定", EditorStyles.boldLabel);
        
        // レンダリング順序の詳細設定
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("描画順序設定", EditorStyles.boldLabel);
        
        trailSystem.trailSortingOrder = EditorGUILayout.IntField("Trail Sorting Order", trailSystem.trailSortingOrder);
        
        EditorGUILayout.HelpBox(
            "レンダリング順序の説明:\n" +
            "• 負の値: 心オブジェクトより後ろに描画\n" +
            "• 0: 心オブジェクトと同じ層\n" +
            "• 正の値: 心オブジェクトより前に描画\n" +
            "推奨値: -10 (心オブジェクトより後ろ)", 
            MessageType.Info
        );
        
        // レンダリング順序のプリセット
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("背景に近い (-20)"))
        {
            trailSystem.trailSortingOrder = -20;
        }
        if (GUILayout.Button("心オブジェクト後ろ (-10)"))
        {
            trailSystem.trailSortingOrder = -10;
        }
        if (GUILayout.Button("心オブジェクト前 (+10)"))
        {
            trailSystem.trailSortingOrder = 10;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 軌跡色調整
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("軌跡色調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        trailSystem.applyTrailColorAdjustments = EditorGUILayout.Toggle("軌跡色調整を適用", trailSystem.applyTrailColorAdjustments);
        
        if (trailSystem.applyTrailColorAdjustments)
        {
            EditorGUILayout.Space();
            
            // 明度調整
            EditorGUILayout.LabelField("明度調整", EditorStyles.boldLabel);
            trailSystem.trailBrightness = EditorGUILayout.Slider("明度", trailSystem.trailBrightness, 0.0f, 2.0f);
            
            // 明度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("暗め (0.5)"))
            {
                trailSystem.trailBrightness = 0.5f;
            }
            if (GUILayout.Button("標準 (1.0)"))
            {
                trailSystem.trailBrightness = 1.0f;
            }
            if (GUILayout.Button("明るめ (1.5)"))
            {
                trailSystem.trailBrightness = 1.5f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 彩度調整
            EditorGUILayout.LabelField("彩度調整", EditorStyles.boldLabel);
            trailSystem.trailSaturation = EditorGUILayout.Slider("彩度", trailSystem.trailSaturation, 0.0f, 2.0f);
            
            // 彩度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("低彩度 (0.5)"))
            {
                trailSystem.trailSaturation = 0.5f;
            }
            if (GUILayout.Button("標準 (1.0)"))
            {
                trailSystem.trailSaturation = 1.0f;
            }
            if (GUILayout.Button("高彩度 (1.5)"))
            {
                trailSystem.trailSaturation = 1.5f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 色相調整
            EditorGUILayout.LabelField("色相調整", EditorStyles.boldLabel);
            trailSystem.trailHueShift = EditorGUILayout.Slider("色相シフト", trailSystem.trailHueShift, -1.0f, 1.0f);
            
            // 色相プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("赤系 (-0.2)"))
            {
                trailSystem.trailHueShift = -0.2f;
            }
            if (GUILayout.Button("標準 (0.0)"))
            {
                trailSystem.trailHueShift = 0.0f;
            }
            if (GUILayout.Button("青系 (0.2)"))
            {
                trailSystem.trailHueShift = 0.2f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 透明度調整
            EditorGUILayout.LabelField("透明度調整", EditorStyles.boldLabel);
            trailSystem.trailAlpha = EditorGUILayout.Slider("透明度", trailSystem.trailAlpha, 0.0f, 1.0f);
            
            // 透明度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("透明 (0.3)"))
            {
                trailSystem.trailAlpha = 0.3f;
            }
            if (GUILayout.Button("半透明 (0.7)"))
            {
                trailSystem.trailAlpha = 0.7f;
            }
            if (GUILayout.Button("不透明 (1.0)"))
            {
                trailSystem.trailAlpha = 1.0f;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        
        // デバッグ情報
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("デバッグ情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"アクティブパーティクル数: {trailSystem.ActiveParticleCount}");
            EditorGUILayout.LabelField($"登録エージェント数: {trailSystem.RegisteredAgentCount}");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("軌跡システムリセット"))
            {
                trailSystem.ResetTrailSystem();
            }
            if (GUILayout.Button("軌跡色調整リセット"))
            {
                trailSystem.ResetTrailColorAdjustments();
            }
            if (GUILayout.Button("ランダム軌跡色"))
            {
                trailSystem.SetTrailColorAdjustments(
                    Random.Range(0.5f, 1.5f),
                    Random.Range(0.5f, 1.5f),
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0.3f, 1.0f)
                );
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(trailSystem);
        }
    }
}
