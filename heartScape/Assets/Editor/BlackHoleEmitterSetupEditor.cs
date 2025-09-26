using UnityEngine;
using UnityEditor;
using HeartScape.IO.Gateway;

/// <summary>
/// BlackHoleEmitterSetupのカスタムエディタ
/// シーンに直接配置するためのヘルパー
/// </summary>
[CustomEditor(typeof(BlackHoleEmitterSetup))]
public class BlackHoleEmitterSetupEditor : Editor
{
    private BlackHoleEmitterSetup targetScript;
    
    void OnEnable()
    {
        targetScript = (BlackHoleEmitterSetup)target;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Black Hole Emitter Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // デフォルトのInspectorを表示
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Setup Actions", EditorStyles.boldLabel);
        
        // セットアップボタン
        if (GUILayout.Button("Setup Black Hole Emitter", GUILayout.Height(30)))
        {
            targetScript.SetupBlackHoleEmitter();
            EditorUtility.SetDirty(targetScript);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Add this component to each Emitter (A, B, C, D)\n" +
            "2. Configure the settings above\n" +
            "3. Click 'Setup Black Hole Emitter' button\n" +
            "4. The setup will create all necessary components and objects\n" +
            "5. Settings will be preserved in the scene",
            MessageType.Info
        );
        
        serializedObject.ApplyModifiedProperties();
    }
}
