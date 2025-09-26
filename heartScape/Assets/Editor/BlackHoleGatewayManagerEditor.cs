using UnityEngine;
using UnityEditor;
using HeartScape.IO.Gateway;

/// <summary>
/// BlackHoleGatewayManagerのカスタムエディタ
/// 統合された設定管理UI
/// </summary>
[CustomEditor(typeof(BlackHoleGatewayManager))]
public class BlackHoleGatewayManagerEditor : Editor
{
    private BlackHoleGatewayManager targetScript;
    private bool showGlobalSettings = true;
    private bool showIndividualSettings = false;
    private bool showDebugSettings = false;
    
    void OnEnable()
    {
        targetScript = (BlackHoleGatewayManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Black Hole Gateway Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // デフォルトのInspectorを表示
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        // グローバル設定の折りたたみ
        showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Global Settings");
        if (showGlobalSettings)
        {
            EditorGUI.indentLevel++;
            DrawGlobalSettings();
            EditorGUI.indentLevel--;
        }
        
        // 個別設定の折りたたみ
        showIndividualSettings = EditorGUILayout.Foldout(showIndividualSettings, "Individual Emitter Settings");
        if (showIndividualSettings)
        {
            EditorGUI.indentLevel++;
            DrawIndividualSettings();
            EditorGUI.indentLevel--;
        }
        
        // デバッグ設定の折りたたみ
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug & Actions");
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            DrawDebugSettings();
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. This manager automatically controls all Emitters (A, B, C, D)\n" +
            "2. Changes are applied in real-time when 'Auto Apply Changes' is enabled\n" +
            "3. Use 'Individual Settings' to customize each emitter separately\n" +
            "4. All settings are preserved in the scene file",
            MessageType.Info
        );
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void DrawGlobalSettings()
    {
        EditorGUILayout.LabelField("Black Hole Appearance", EditorStyles.boldLabel);
        targetScript.globalHoleColor = EditorGUILayout.ColorField("Hole Color", targetScript.globalHoleColor);
        targetScript.globalHoleCenterColor = EditorGUILayout.ColorField("Hole Center Color", targetScript.globalHoleCenterColor);
        targetScript.globalHoleSize = EditorGUILayout.Slider("Hole Size", targetScript.globalHoleSize, 0.1f, 3f);
        targetScript.globalHoleFadeRadius = EditorGUILayout.Slider("Hole Fade Radius", targetScript.globalHoleFadeRadius, 0.1f, 1f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bubble Emission", EditorStyles.boldLabel);
        targetScript.globalEnableBubbleEmission = EditorGUILayout.Toggle("Enable Bubble Emission", targetScript.globalEnableBubbleEmission);
        targetScript.globalBubbleEmissionRate = EditorGUILayout.Slider("Emission Rate", targetScript.globalBubbleEmissionRate, 0f, 10f);
        targetScript.globalBubbleLifetime = EditorGUILayout.Slider("Bubble Lifetime", targetScript.globalBubbleLifetime, 0.1f, 10f);
        targetScript.globalBubbleSize = EditorGUILayout.Slider("Bubble Size", targetScript.globalBubbleSize, 0.01f, 1f);
        targetScript.globalBubbleSpeed = EditorGUILayout.Slider("Bubble Speed", targetScript.globalBubbleSpeed, 0.1f, 5f);
        targetScript.globalBubbleColor = EditorGUILayout.ColorField("Bubble Color", targetScript.globalBubbleColor);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bubble Direction", EditorStyles.boldLabel);
        targetScript.globalUseEmitterDirection = EditorGUILayout.Toggle("Use Emitter Direction", targetScript.globalUseEmitterDirection);
        targetScript.globalBubbleDirectionStrength = EditorGUILayout.Slider("Direction Strength", targetScript.globalBubbleDirectionStrength, 0f, 3f);
        targetScript.globalBubbleSpreadAngle = EditorGUILayout.Slider("Spread Angle", targetScript.globalBubbleSpreadAngle, 0f, 90f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Object Appearance", EditorStyles.boldLabel);
        targetScript.globalObjectAppearSpeed = EditorGUILayout.Slider("Appear Speed", targetScript.globalObjectAppearSpeed, 0.1f, 20f);
        targetScript.globalObjectMaxScale = EditorGUILayout.Slider("Max Scale", targetScript.globalObjectMaxScale, 0.1f, 3f);
    }
    
    void DrawIndividualSettings()
    {
        targetScript.enableIndividualSettings = EditorGUILayout.Toggle("Enable Individual Settings", targetScript.enableIndividualSettings);
        
        if (targetScript.enableIndividualSettings)
        {
            EditorGUILayout.HelpBox("Individual settings override global settings for each emitter.", MessageType.Info);
            
            // 個別設定の配列を表示
            SerializedProperty individualSettings = serializedObject.FindProperty("individualEmitterSettings");
            EditorGUILayout.PropertyField(individualSettings, true);
        }
    }
    
    void DrawDebugSettings()
    {
        targetScript.enableDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs", targetScript.enableDebugLogs);
        targetScript.autoApplyChanges = EditorGUILayout.Toggle("Auto Apply Changes", targetScript.autoApplyChanges);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Apply All Settings Now", GUILayout.Height(25)))
        {
            targetScript.ManualApplySettings();
        }
        
        if (GUILayout.Button("Reset All Emitters", GUILayout.Height(25)))
        {
            targetScript.ResetAllEmitters();
        }
        
        if (GUILayout.Button("Debug Status", GUILayout.Height(25)))
        {
            targetScript.DebugStatus();
        }
    }
}
