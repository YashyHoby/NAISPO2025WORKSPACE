using UnityEngine;
using UnityEditor;
using HeartScape.IO.Gateway;

/// <summary>
/// BlackHoleEmitterVisualのカスタムエディタ
/// シーン再生中でなくても調整可能
/// </summary>
[CustomEditor(typeof(BlackHoleEmitterVisual))]
public class BlackHoleEmitterVisualEditor : Editor
{
    private BlackHoleEmitterVisual targetScript;
    private bool showHoleSettings = true;
    private bool showBubbleSettings = true;
    private bool showObjectSettings = true;
    private bool showDebugSettings = true;
    
    void OnEnable()
    {
        targetScript = (BlackHoleEmitterVisual)target;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Black Hole Emitter Visual", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // ホール設定
        showHoleSettings = EditorGUILayout.Foldout(showHoleSettings, "Black Hole Settings", true);
        if (showHoleSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            targetScript.holeColor = EditorGUILayout.ColorField("Hole Color", targetScript.holeColor);
            targetScript.holeCenterColor = EditorGUILayout.ColorField("Center Color", targetScript.holeCenterColor);
            targetScript.holeSize = EditorGUILayout.Slider("Hole Size", targetScript.holeSize, 0.1f, 3f);
            targetScript.holeFadeRadius = EditorGUILayout.Slider("Fade Radius", targetScript.holeFadeRadius, 0.1f, 1f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Curves", EditorStyles.boldLabel);
            targetScript.holeFadeCurve = EditorGUILayout.CurveField("Fade Curve", targetScript.holeFadeCurve);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 泡設定
        showBubbleSettings = EditorGUILayout.Foldout(showBubbleSettings, "Bubble Settings", true);
        if (showBubbleSettings)
        {
            EditorGUI.indentLevel++;
            
            targetScript.enableBubbleEmission = EditorGUILayout.Toggle("Enable Bubble Emission", targetScript.enableBubbleEmission);
            targetScript.bubbleEmissionRate = EditorGUILayout.Slider("Emission Rate", targetScript.bubbleEmissionRate, 0f, 10f);
            targetScript.bubbleLifetime = EditorGUILayout.Slider("Bubble Lifetime", targetScript.bubbleLifetime, 0.5f, 10f);
            targetScript.bubbleSize = EditorGUILayout.Slider("Bubble Size", targetScript.bubbleSize, 0.01f, 0.5f);
            targetScript.bubbleSpeed = EditorGUILayout.Slider("Bubble Speed", targetScript.bubbleSpeed, 0.1f, 2f);
            targetScript.bubbleColor = EditorGUILayout.ColorField("Bubble Color", targetScript.bubbleColor);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Curves", EditorStyles.boldLabel);
            targetScript.bubbleFadeCurve = EditorGUILayout.CurveField("Fade Curve", targetScript.bubbleFadeCurve);
            targetScript.bubbleScaleCurve = EditorGUILayout.CurveField("Scale Curve", targetScript.bubbleScaleCurve);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // オブジェクト出現設定
        showObjectSettings = EditorGUILayout.Foldout(showObjectSettings, "Object Appearance Settings", true);
        if (showObjectSettings)
        {
            EditorGUI.indentLevel++;
            
            targetScript.objectAppearSpeed = EditorGUILayout.Slider("Appear Speed", targetScript.objectAppearSpeed, 0.5f, 20f);
            targetScript.objectMaxScale = EditorGUILayout.Slider("Max Scale", targetScript.objectMaxScale, 0.5f, 2f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Curves", EditorStyles.boldLabel);
            targetScript.objectScaleCurve = EditorGUILayout.CurveField("Scale Curve", targetScript.objectScaleCurve);
            targetScript.objectFadeCurve = EditorGUILayout.CurveField("Fade Curve", targetScript.objectFadeCurve);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // デバッグ設定
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            
            targetScript.enableDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs", targetScript.enableDebugLogs);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Functions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Update Hole Visual"))
            {
                targetScript.UpdateHoleVisual();
            }
            
            if (GUILayout.Button("Update Bubble Settings"))
            {
                targetScript.UpdateBubbleEmission();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // リアルタイム更新ボタン
        if (GUILayout.Button("Apply All Changes", GUILayout.Height(30)))
        {
            targetScript.UpdateHoleVisual();
            targetScript.UpdateBubbleEmission();
            EditorUtility.SetDirty(targetScript);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void OnSceneGUI()
    {
        if (targetScript == null) return;
        
        // ホールのサイズをシーンビューで表示
        Handles.color = targetScript.holeColor;
        Handles.DrawWireDisc(targetScript.transform.position, Vector3.forward, targetScript.holeSize * 0.5f);
        
        // スポーンポイントを表示
        if (targetScript.objectSpawnPoint != null)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(targetScript.objectSpawnPoint.position, Vector3.one * 0.2f);
        }
    }
}