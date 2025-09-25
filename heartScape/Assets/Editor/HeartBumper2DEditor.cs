using UnityEngine;
using UnityEditor;
using HeartScape.Interaction.Bumper;

[CustomEditor(typeof(HeartBumper2D))]
public class HeartBumper2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HeartBumper2D bumper = (HeartBumper2D)target;
        
        // 変更前の値を記録
        float oldColliderWidth = bumper.colliderWidth;
        float oldColliderHeight = bumper.colliderHeight;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        // コライダーサイズが変更されたかチェック
        bool colliderSizeChanged = oldColliderWidth != bumper.colliderWidth || oldColliderHeight != bumper.colliderHeight;
        
        if (colliderSizeChanged)
        {
            // コライダーサイズが変更された場合は即座に更新
            bumper.UpdateColliderSize();
            
            // ビジュアルも同期（エディター時）
            if (bumper.slimeVisual != null && !Application.isPlaying)
            {
                bumper.slimeVisual.rectWidth = bumper.colliderWidth;
                bumper.slimeVisual.rectHeight = bumper.colliderHeight;
                bumper.slimeVisual.UpdateVisualSizeEditor();
                bumper.slimeVisual.ApplySlimeParametersForced();
            }
            
            EditorUtility.SetDirty(bumper);
        }
        
        // スライダー即座反映システム（更新ボタンは削除）
        
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
        
        // 複数Bumper生成
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("複数Bumper生成", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("横に複製"))
        {
            CreateBumperCopy(bumper, new Vector3(2f, 0f, 0f));
        }
        
        if (GUILayout.Button("縦に複製"))
        {
            CreateBumperCopy(bumper, new Vector3(0f, 2f, 0f));
        }
        
        if (GUILayout.Button("対角に複製"))
        {
            CreateBumperCopy(bumper, new Vector3(2f, 2f, 0f));
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("3x3グリッド生成"))
        {
            CreateBumperGrid(bumper, 3, 3, 2f);
        }
        
        if (GUILayout.Button("2x4グリッド生成"))
        {
            CreateBumperGrid(bumper, 2, 4, 2f);
        }
        
        if (GUILayout.Button("円形配置"))
        {
            CreateBumperCircle(bumper, 6, 4f);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // エディター時の調整
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("エディター時調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("シーン再生前でも調整・プレビューできます", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("バンパーを完全初期化"))
            {
                bumper.ForceConvertToBoxCollider();
                bumper.ForceCreateVisualChild();
            }
            
            if (GUILayout.Button("ビジュアルを更新"))
            {
                if (bumper.slimeVisual != null)
                {
                    bumper.slimeVisual.InitializeForEditor();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 必要最小限の初期化ボタンのみ残す
        }
        else
        {
            EditorGUILayout.HelpBox("シーン実行中", MessageType.Info);
            
            if (GUILayout.Button("バンパーを完全初期化"))
            {
                bumper.ForceConvertToBoxCollider();
                bumper.ForceCreateVisualChild();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // 変更を保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(bumper);
        }
        
        // エディター時は頻繁に再描画
        if (!Application.isPlaying)
        {
            Repaint();
        }
    }
    
    void CreateBumperCopy(HeartBumper2D original, Vector3 offset)
    {
        GameObject newBumper = Instantiate(original.gameObject);
        newBumper.transform.position = original.transform.position + offset;
        newBumper.name = $"Bumper_{Random.Range(1000, 9999)}";
        
        // ランダムな色を設定
        HeartScape.Interaction.Bumper.SlimeBumperVisual visual = newBumper.GetComponentInChildren<HeartScape.Interaction.Bumper.SlimeBumperVisual>();
        if (visual != null)
        {
            visual.SetRandomColors();
        }
        
        Selection.activeGameObject = newBumper;
    }
    
    void CreateBumperGrid(HeartBumper2D original, int rows, int cols, float spacing)
    {
        Vector3 startPos = original.transform.position;
        startPos.x -= (cols - 1) * spacing * 0.5f;
        startPos.y -= (rows - 1) * spacing * 0.5f;
        
        Color[] gridColors = {
            new Color(0.2f, 0.8f, 0.3f, 1.0f), // 緑
            new Color(0.2f, 0.5f, 0.9f, 1.0f), // 青
            new Color(0.6f, 0.3f, 0.9f, 1.0f), // 紫
            new Color(0.9f, 0.8f, 0.2f, 1.0f), // 黄
            new Color(0.9f, 0.4f, 0.7f, 1.0f), // ピンク
            new Color(0.9f, 0.6f, 0.2f, 1.0f), // オレンジ
            new Color(0.2f, 0.8f, 0.9f, 1.0f), // シアン
        };
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (row == 0 && col == 0) continue; // 元のオブジェクトはスキップ
                
                Vector3 pos = startPos + new Vector3(col * spacing, row * spacing, 0);
                GameObject newBumper = Instantiate(original.gameObject);
                newBumper.transform.position = pos;
                newBumper.name = $"Bumper_Grid_{row}_{col}";
                
                // グリッドに応じた色を設定
                HeartScape.Interaction.Bumper.SlimeBumperVisual visual = newBumper.GetComponentInChildren<HeartScape.Interaction.Bumper.SlimeBumperVisual>();
                if (visual != null)
                {
                    int colorIndex = (row * cols + col) % gridColors.Length;
                    Color baseColor = gridColors[colorIndex];
                    visual.SetSlimeColors(
                        baseColor,
                        baseColor * 0.6f,
                        Color.Lerp(baseColor, Color.white, 0.3f)
                    );
                }
            }
        }
    }
    
    void CreateBumperCircle(HeartBumper2D original, int count, float radius)
    {
        Vector3 center = original.transform.position;
        
        for (int i = 0; i < count; i++)
        {
            if (i == 0) continue; // 元のオブジェクトはスキップ
            
            float angle = (float)i / count * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            
            GameObject newBumper = Instantiate(original.gameObject);
            newBumper.transform.position = pos;
            newBumper.name = $"Bumper_Circle_{i}";
            
            // 円周に応じた色を設定（虹色）
            HeartScape.Interaction.Bumper.SlimeBumperVisual visual = newBumper.GetComponentInChildren<HeartScape.Interaction.Bumper.SlimeBumperVisual>();
            if (visual != null)
            {
                float hue = (float)i / count;
                visual.SetHue(hue);
            }
        }
    }
}