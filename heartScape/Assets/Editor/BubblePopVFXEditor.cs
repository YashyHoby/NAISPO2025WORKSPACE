using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BubblePopVFX))]
public class BubblePopVFXEditor : Editor
{
    private BubblePopVFX bubblePopVFX;
    
    void OnEnable()
    {
        bubblePopVFX = (BubblePopVFX)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("シンプル花火システム", EditorStyles.boldLabel);
        
        if (bubblePopVFX.fireworkSystem != null)
        {
            EditorGUILayout.HelpBox("シンプル花火システムが設定されています。", MessageType.Info);
            
            if (GUILayout.Button("花火をテスト"))
            {
                TestFirework();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("シンプル花火システムが設定されていません。", MessageType.Warning);
            
            if (GUILayout.Button("花火システムを自動設定"))
            {
                SetupFireworkSystem();
            }
        }
        
        EditorGUILayout.Space();
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("ランタイム情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("花火システム:", bubblePopVFX.fireworkSystem != null ? "アクティブ" : "非アクティブ");
        }
    }
    
    void TestFirework()
    {
        if (bubblePopVFX.fireworkSystem != null)
        {
            // テスト用の色リストを作成
            List<Color> testColors = new List<Color> { Color.red, Color.blue, Color.green, Color.yellow };
            bubblePopVFX.fireworkSystem.SendMessage("StartFirework", testColors, SendMessageOptions.DontRequireReceiver);
            
            Debug.Log("[BubblePopVFXEditor] 花火テストを開始しました。");
        }
    }
    
    void SetupFireworkSystem()
    {
        // SimpleFireworkSystemコンポーネントを追加
        var fireworkSystem = bubblePopVFX.gameObject.GetComponent("SimpleFireworkSystem") as Component;
        if (fireworkSystem == null)
        {
            // より確実な方法で型を取得
            System.Type fireworkType = null;
            
            // 複数の方法で型を取得を試行
            string[] typeNames = {
                "SimpleFireworkSystem, Assembly-CSharp",
                "SimpleFireworkSystem, Assembly-CSharp-firstpass",
                "SimpleFireworkSystem"
            };
            
            foreach (string typeName in typeNames)
            {
                fireworkType = System.Type.GetType(typeName);
                if (fireworkType != null) break;
            }
            
            if (fireworkType != null)
            {
                fireworkSystem = bubblePopVFX.gameObject.AddComponent(fireworkType) as Component;
                bubblePopVFX.fireworkSystem = fireworkSystem;
                Debug.Log($"[BubblePopVFXEditor] SimpleFireworkSystemを追加しました。型: {fireworkType.Name}");
            }
            else
            {
                Debug.LogError("[BubblePopVFXEditor] SimpleFireworkSystemクラスが見つかりません。コンパイルを確認してください。");
            }
        }
    }
}