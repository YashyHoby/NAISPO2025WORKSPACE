using UnityEngine;
using UnityEditor;
using HeartScape.Interaction.Bumper;

[CustomEditor(typeof(HeartBumper2D))]
public class HeartBumper2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HeartBumper2D bumper = (HeartBumper2D)target;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // コライダー変換
        EditorGUILayout.LabelField("コライダー設定", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // 現在のコライダータイプを表示
        CircleCollider2D circleCollider = bumper.GetComponent<CircleCollider2D>();
        BoxCollider2D boxCollider = bumper.GetComponent<BoxCollider2D>();
        
        if (circleCollider != null)
        {
            EditorGUILayout.HelpBox("現在: CircleCollider2D（長方形バンパーには不適切）", MessageType.Warning);
            if (GUILayout.Button("BoxCollider2Dに変換"))
            {
                bumper.ForceConvertToBoxCollider();
            }
        }
        else if (boxCollider != null)
        {
            EditorGUILayout.HelpBox("現在: BoxCollider2D（適切な設定）", MessageType.Info);
            EditorGUILayout.LabelField($"サイズ: {boxCollider.size.x:F2} x {boxCollider.size.y:F2}");
        }
        else
        {
            EditorGUILayout.HelpBox("コライダーが見つかりません", MessageType.Error);
            if (GUILayout.Button("BoxCollider2Dを追加"))
            {
                bumper.gameObject.AddComponent<BoxCollider2D>();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // ビジュアル管理
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ビジュアル管理", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (bumper.slimeVisual == null)
        {
            EditorGUILayout.HelpBox("スライムビジュアルが設定されていません", MessageType.Warning);
            if (GUILayout.Button("スライムビジュアル子オブジェクトを作成"))
            {
                bumper.ForceCreateVisualChild();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("スライムビジュアル設定済み", MessageType.Info);
            EditorGUILayout.LabelField($"ビジュアルサイズ: {bumper.slimeVisual.rectWidth:F2} x {bumper.slimeVisual.rectHeight:F2}");
            
            if (GUILayout.Button("ビジュアルを再作成"))
            {
                bumper.ForceCreateVisualChild();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // ビジュアル演出調整
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ビジュアル演出調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("ゼリー風アニメーションプリセット", EditorStyles.boldLabel);
        
        // 第1行：基本的なゼリー
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("柔らかゼリー"))
        {
            bumper.visualScaleMultiplier = 1.2f;
            bumper.visualDuration = 1.2f;
            bumper.visualShakeDamping = 1.5f;
            bumper.visualShakeFrequency = 5f;
        }
        
        if (GUILayout.Button("標準ゼリー"))
        {
            bumper.visualScaleMultiplier = 1.15f;
            bumper.visualDuration = 0.8f;
            bumper.visualShakeDamping = 2.5f;
            bumper.visualShakeFrequency = 7f;
        }
        
        if (GUILayout.Button("硬めゼリー"))
        {
            bumper.visualScaleMultiplier = 1.1f;
            bumper.visualDuration = 0.5f;
            bumper.visualShakeDamping = 4f;
            bumper.visualShakeFrequency = 10f;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 第2行：特殊なゼリー
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("ぷるぷるゼリー"))
        {
            bumper.visualScaleMultiplier = 1.25f;
            bumper.visualDuration = 1.5f;
            bumper.visualShakeDamping = 1.2f;
            bumper.visualShakeFrequency = 4f;
        }
        
        if (GUILayout.Button("ブルブルゼリー"))
        {
            bumper.visualScaleMultiplier = 1.08f;
            bumper.visualDuration = 0.6f;
            bumper.visualShakeDamping = 3f;
            bumper.visualShakeFrequency = 15f;
        }
        
        if (GUILayout.Button("ゆらゆらゼリー"))
        {
            bumper.visualScaleMultiplier = 1.3f;
            bumper.visualDuration = 2.0f;
            bumper.visualShakeDamping = 1f;
            bumper.visualShakeFrequency = 3f;
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // 初期化
        EditorGUILayout.Space();
        if (GUILayout.Button("バンパーを完全初期化"))
        {
            bumper.ForceConvertToBoxCollider();
            bumper.ForceCreateVisualChild();
        }
        
        // 変更を保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(bumper);
        }
    }
}