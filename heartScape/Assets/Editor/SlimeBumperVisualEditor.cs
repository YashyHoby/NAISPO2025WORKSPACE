using UnityEngine;
using UnityEditor;
using HeartScape.Interaction.Bumper;

[CustomEditor(typeof(SlimeBumperVisual))]
public class SlimeBumperVisualEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SlimeBumperVisual visual = (SlimeBumperVisual)target;
        
        // デフォルトのインスペクターを表示
        DrawDefaultInspector();
        
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
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("緑色"))
        {
            visual.slimeColor = Color.green;
            visual.outerColor = new Color(0.0f, 0.5f, 0.0f, 1.0f);
            visual.innerColor = new Color(0.5f, 1.0f, 0.5f, 1.0f);
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("青色"))
        {
            visual.slimeColor = Color.blue;
            visual.outerColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
            visual.innerColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("紫色"))
        {
            visual.slimeColor = Color.magenta;
            visual.outerColor = new Color(0.5f, 0.0f, 0.5f, 1.0f);
            visual.innerColor = new Color(1.0f, 0.5f, 1.0f, 1.0f);
            visual.ApplySlimeParameters();
        }
        
        if (GUILayout.Button("黄色"))
        {
            visual.slimeColor = Color.yellow;
            visual.outerColor = new Color(0.5f, 0.5f, 0.0f, 1.0f);
            visual.innerColor = new Color(1.0f, 1.0f, 0.5f, 1.0f);
            visual.ApplySlimeParameters();
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
        
        // テスト機能
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("テスト機能", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("衝突反応テスト"))
        {
            visual.OnBumperCollision();
        }
        
        if (GUILayout.Button("パラメータ更新"))
        {
            if (!visual.protectMaterialSettings)
            {
                visual.ApplySlimeParameters();
            }
            visual.UpdateVisualSize();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 変更を保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(visual);
        }
    }
}
