using UnityEngine;
using UnityEditor;
using HeartScape.Interaction.Bumper;

[CustomEditor(typeof(SlimeBumperVisual))]
public class SlimeBumperVisualEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SlimeBumperVisual visual = (SlimeBumperVisual)target;
        
        // 変更前の値を記録（全パラメータ）
        Color oldSlimeColor = visual.slimeColor;
        Color oldOuterColor = visual.outerColor;
        Color oldInnerColor = visual.innerColor;
        float oldRectWidth = visual.rectWidth;
        float oldRectHeight = visual.rectHeight;
        float oldCornerRadius = visual.cornerRadius;
        float oldOuterThickness = visual.outerThickness;
        float oldOuterAlpha = visual.outerAlpha;
        float oldInnerThickness = visual.innerThickness;
        float oldInnerAlpha = visual.innerAlpha;
        float oldGlossiness = visual.glossiness;
        float oldViscosity = visual.viscosity;
        float oldSpecularPower = visual.specularPower;
        float oldAnimationSpeed = visual.animationSpeed;
        float oldPulseIntensity = visual.pulseIntensity;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
        // 値が変更されたかチェック（全パラメータ）
        bool colorChanged = oldSlimeColor != visual.slimeColor || oldOuterColor != visual.outerColor || oldInnerColor != visual.innerColor;
        bool sizeChanged = oldRectWidth != visual.rectWidth || oldRectHeight != visual.rectHeight || oldCornerRadius != visual.cornerRadius;
        bool appearanceChanged = oldOuterThickness != visual.outerThickness || oldOuterAlpha != visual.outerAlpha || 
                                oldInnerThickness != visual.innerThickness || oldInnerAlpha != visual.innerAlpha;
        bool materialChanged = oldGlossiness != visual.glossiness || oldViscosity != visual.viscosity || oldSpecularPower != visual.specularPower;
        bool animationChanged = oldAnimationSpeed != visual.animationSpeed || oldPulseIntensity != visual.pulseIntensity;
        
        if (colorChanged || sizeChanged || appearanceChanged || materialChanged || animationChanged)
        {
            // 変更があった場合は即座に更新
            visual.ApplySlimeParametersForced();
            if (sizeChanged)
            {
                visual.UpdateVisualSizeEditor();
            }
            EditorUtility.SetDirty(visual);
            SceneView.RepaintAll();
        }
        
        // スライダー即座反映システム（ボタンは削除）
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("スライム制御", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // プリセットボタン
        EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("粘性スライム"))
        {
            visual.SetViscousPreset();
        }
        
        if (GUILayout.Button("標準スライム"))
        {
            visual.SetStandardPreset();
        }
        
        if (GUILayout.Button("乾燥スライム"))
        {
            visual.SetDryPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 形状調整
        EditorGUILayout.LabelField("形状調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("正方形"))
        {
            visual.rectWidth = 1.0f;
            visual.rectHeight = 1.0f;
            visual.UpdateVisualSize();
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("横長"))
        {
            visual.rectWidth = 1.5f;
            visual.rectHeight = 0.8f;
            visual.UpdateVisualSize();
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("縦長"))
        {
            visual.rectWidth = 0.8f;
            visual.rectHeight = 1.5f;
            visual.UpdateVisualSize();
            visual.ApplySlimeParameters();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 角の丸み調整
        EditorGUILayout.LabelField("角の丸み", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("角ばった"))
        {
            visual.cornerRadius = 0.0f;
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("少し丸い"))
        {
            visual.cornerRadius = 0.1f;
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("とても丸い"))
        {
            visual.cornerRadius = 0.3f;
            visual.ApplySlimeParameters();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 色調整
        EditorGUILayout.LabelField("色調整", EditorStyles.boldLabel);
        
        // 基本色プリセット
        EditorGUILayout.LabelField("基本色プリセット", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("緑色"))
        {
            visual.SetSlimeColors(
                new Color(0.2f, 0.8f, 0.3f, 1.0f),
                new Color(0.1f, 0.5f, 0.2f, 1.0f),
                new Color(0.4f, 1.0f, 0.5f, 1.0f)
            );
            
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(visual);
                SceneView.RepaintAll();
            }
        }
        
        if (GUILayout.Button("青色"))
        {
            visual.SetSlimeColors(
                new Color(0.2f, 0.5f, 0.9f, 1.0f),
                new Color(0.1f, 0.3f, 0.6f, 1.0f),
                new Color(0.4f, 0.7f, 1.0f, 1.0f)
            );
        }
        
        if (GUILayout.Button("紫色"))
        {
            visual.SetSlimeColors(
                new Color(0.6f, 0.3f, 0.9f, 1.0f),
                new Color(0.4f, 0.2f, 0.6f, 1.0f),
                new Color(0.8f, 0.5f, 1.0f, 1.0f)
            );
        }
        
        if (GUILayout.Button("黄色"))
        {
            visual.SetSlimeColors(
                new Color(0.9f, 0.8f, 0.2f, 1.0f),
                new Color(0.6f, 0.5f, 0.1f, 1.0f),
                new Color(1.0f, 1.0f, 0.4f, 1.0f)
            );
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 特殊色プリセット
        EditorGUILayout.LabelField("特殊色プリセット", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("ピンク"))
        {
            visual.SetSlimeColors(
                new Color(0.9f, 0.4f, 0.7f, 1.0f),
                new Color(0.6f, 0.2f, 0.4f, 1.0f),
                new Color(1.0f, 0.6f, 0.9f, 1.0f)
            );
        }
        
        if (GUILayout.Button("オレンジ"))
        {
            visual.SetSlimeColors(
                new Color(0.9f, 0.6f, 0.2f, 1.0f),
                new Color(0.6f, 0.3f, 0.1f, 1.0f),
                new Color(1.0f, 0.8f, 0.4f, 1.0f)
            );
        }
        
        if (GUILayout.Button("シアン"))
        {
            visual.SetSlimeColors(
                new Color(0.2f, 0.8f, 0.9f, 1.0f),
                new Color(0.1f, 0.5f, 0.6f, 1.0f),
                new Color(0.4f, 1.0f, 1.0f, 1.0f)
            );
        }
        
        if (GUILayout.Button("ランダム"))
        {
            visual.SetRandomColors();
            
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(visual);
                SceneView.RepaintAll();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 色相調整
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("色相調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("色相+"))
        {
            Color.RGBToHSV(visual.slimeColor, out float h, out float s, out float v);
            visual.SetHue((h + 0.1f) % 1.0f);
            
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(visual);
                SceneView.RepaintAll();
            }
        }
        
        if (GUILayout.Button("色相-"))
        {
            Color.RGBToHSV(visual.slimeColor, out float h, out float s, out float v);
            visual.SetHue((h - 0.1f + 1.0f) % 1.0f);
            
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(visual);
                SceneView.RepaintAll();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 液体風光沢調整
        EditorGUILayout.LabelField("液体風光沢調整", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("とろとろ"))
        {
            visual.glossiness = 1.5f;
            visual.viscosity = 1.0f;
            visual.specularPower = 4.0f;
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("ぷるぷる"))
        {
            visual.glossiness = 1.0f;
            visual.viscosity = 0.8f;
            visual.specularPower = 6.0f;
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("さらさら"))
        {
            visual.glossiness = 0.6f;
            visual.viscosity = 0.4f;
            visual.specularPower = 12.0f;
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("ねばねば"))
        {
            visual.glossiness = 0.4f;
            visual.viscosity = 0.9f;
            visual.specularPower = 3.0f;
            visual.ApplySlimeParameters();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // マテリアル保護機能
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("マテリアル保護機能", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (visual.protectMaterialSettings)
        {
            EditorGUILayout.HelpBox("マテリアル設定が保護されています。手動変更が維持されます。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("スクリプトがマテリアルを自動更新します。", MessageType.Warning);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("保護を切り替え"))
        {
            visual.ToggleProtection();
        }
        
        if (GUILayout.Button("マテリアルから読み込み"))
        {
            visual.LoadParametersFromMaterial();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // エディター時の調整機能
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("エディター調整機能", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("シーン再生前の調整：パラメータを変更すると自動で反映されます", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("シーン実行中：パラメータ変更が自動で反映されます", MessageType.Info);
        }
        
        // マテリアルの状態表示
        if (visual.SlimeMaterial != null)
        {
            EditorGUILayout.LabelField($"マテリアル: {visual.SlimeMaterial.name}");
            
            // 現在のマテリアルパラメータを表示
            if (visual.SlimeMaterial.HasProperty("_OuterThickness"))
            {
                float currentOuterThickness = visual.SlimeMaterial.GetFloat("_OuterThickness");
                EditorGUILayout.LabelField($"現在の外郭厚み: {currentOuterThickness:F3}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("マテリアルが設定されていません。「完全初期化」を実行してください。", MessageType.Warning);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("完全初期化"))
        {
            visual.InitializeForEditor();
        }
        
        if (Application.isPlaying && GUILayout.Button("衝突反応テスト"))
        {
            visual.OnBumperCollision();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // 変更を保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(visual);
        }
        
        // エディター時は頻繁に再描画
        if (!Application.isPlaying)
        {
            Repaint();
        }
    }
}
