using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HeartManager))]
public class HeartManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HeartManager heartManager = (HeartManager)target;
        
        // タイトル
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("心オブジェクト管理システム", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 基本設定
        EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
        heartManager.appearance = (HeartAppearanceMapper)EditorGUILayout.ObjectField(
            "Appearance Mapper", 
            heartManager.appearance, 
            typeof(HeartAppearanceMapper), 
            true
        );
        heartManager.agentPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Agent Prefab", 
            heartManager.agentPrefab, 
            typeof(GameObject), 
            false
        );
        heartManager.maxAgents = EditorGUILayout.IntField("Max Agents", heartManager.maxAgents);
        
        EditorGUILayout.Space();
        
        // 軌跡システム
        EditorGUILayout.LabelField("軌跡システム", EditorStyles.boldLabel);
        heartManager.trailSystem = (HeartTrailSystem)EditorGUILayout.ObjectField(
            "Trail System", 
            heartManager.trailSystem, 
            typeof(HeartTrailSystem), 
            true
        );
        
        EditorGUILayout.Space();
        
        // 心オブジェクト色調整
        EditorGUILayout.LabelField("心オブジェクト色調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        heartManager.applyColorAdjustments = EditorGUILayout.Toggle("色調整を適用", heartManager.applyColorAdjustments);
        
        if (heartManager.applyColorAdjustments)
        {
            EditorGUILayout.Space();
            
            // 明度調整
            EditorGUILayout.LabelField("明度調整", EditorStyles.boldLabel);
            heartManager.globalBrightness = EditorGUILayout.Slider("明度", heartManager.globalBrightness, 0.0f, 2.0f);
            
            // 明度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("暗め (0.5)"))
            {
                heartManager.globalBrightness = 0.5f;
            }
            if (GUILayout.Button("標準 (1.0)"))
            {
                heartManager.globalBrightness = 1.0f;
            }
            if (GUILayout.Button("明るめ (1.5)"))
            {
                heartManager.globalBrightness = 1.5f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 彩度調整
            EditorGUILayout.LabelField("彩度調整", EditorStyles.boldLabel);
            heartManager.globalSaturation = EditorGUILayout.Slider("彩度", heartManager.globalSaturation, 0.0f, 2.0f);
            
            // 彩度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("低彩度 (0.5)"))
            {
                heartManager.globalSaturation = 0.5f;
            }
            if (GUILayout.Button("標準 (1.0)"))
            {
                heartManager.globalSaturation = 1.0f;
            }
            if (GUILayout.Button("高彩度 (1.5)"))
            {
                heartManager.globalSaturation = 1.5f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 色相調整
            EditorGUILayout.LabelField("色相調整", EditorStyles.boldLabel);
            heartManager.globalHueShift = EditorGUILayout.Slider("色相シフト", heartManager.globalHueShift, -1.0f, 1.0f);
            
            // 色相プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("赤系 (-0.2)"))
            {
                heartManager.globalHueShift = -0.2f;
            }
            if (GUILayout.Button("標準 (0.0)"))
            {
                heartManager.globalHueShift = 0.0f;
            }
            if (GUILayout.Button("青系 (0.2)"))
            {
                heartManager.globalHueShift = 0.2f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 透明度調整
            EditorGUILayout.LabelField("透明度調整", EditorStyles.boldLabel);
            heartManager.globalAlpha = EditorGUILayout.Slider("透明度", heartManager.globalAlpha, 0.0f, 1.0f);
            
            // 透明度プリセット
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("透明 (0.3)"))
            {
                heartManager.globalAlpha = 0.3f;
            }
            if (GUILayout.Button("半透明 (0.7)"))
            {
                heartManager.globalAlpha = 0.7f;
            }
            if (GUILayout.Button("不透明 (1.0)"))
            {
                heartManager.globalAlpha = 1.0f;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // デバッグ情報
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("デバッグ情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"アクティブな心オブジェクト数: {heartManager.agents.Count}");
            
            if (GUILayout.Button("色調整リセット"))
            {
                heartManager.SetGlobalColorAdjustments(1.0f, 1.0f, 0.0f, 1.0f);
            }
            
            if (GUILayout.Button("ランダム色調整"))
            {
                heartManager.SetGlobalColorAdjustments(
                    Random.Range(0.5f, 1.5f),
                    Random.Range(0.5f, 1.5f),
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0.3f, 1.0f)
                );
            }
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(heartManager);
        }
    }
}
