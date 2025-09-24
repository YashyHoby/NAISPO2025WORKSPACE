using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BackgroundController))]
public class BackgroundControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BackgroundController controller = (BackgroundController)target;
        
        // タイトル
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("背景制御システム", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 背景マテリアル
        EditorGUILayout.LabelField("背景設定", EditorStyles.boldLabel);
        controller.backgroundMaterial = (Material)EditorGUILayout.ObjectField(
            "Background Material", 
            controller.backgroundMaterial, 
            typeof(Material), 
            false
        );
        
        if (controller.backgroundMaterial == null)
        {
            EditorGUILayout.HelpBox("BackgroundMaterial.matを設定してください。", MessageType.Warning);
            
            if (GUILayout.Button("BackgroundMaterial.matを自動設定"))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BackgroundMaterial.mat");
                if (mat != null)
                {
                    controller.backgroundMaterial = mat;
                    EditorUtility.SetDirty(controller);
                }
                else
                {
                    Debug.LogError("BackgroundMaterial.matが見つかりません。");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"マテリアル設定済み: {controller.backgroundMaterial.name}", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 色彩変化設定
        EditorGUILayout.LabelField("色彩変化", EditorStyles.boldLabel);
        controller.colorChangeSpeed = EditorGUILayout.FloatField("Color Change Speed", controller.colorChangeSpeed);
        controller.hueRange = EditorGUILayout.Vector2Field("Hue Range", controller.hueRange);
        controller.hueOnlyMode = EditorGUILayout.Toggle("Hue Only Mode", controller.hueOnlyMode);
        
        EditorGUILayout.Space();
        
        // 明度・彩度調整
        if (controller.hueOnlyMode)
        {
            EditorGUILayout.LabelField("明度・彩度調整", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("メインカラー", EditorStyles.boldLabel);
            controller.mainBrightness = EditorGUILayout.Slider("明度", controller.mainBrightness, 0.0f, 1.0f);
            controller.mainSaturation = EditorGUILayout.Slider("彩度", controller.mainSaturation, 0.0f, 1.0f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("セカンドカラー", EditorStyles.boldLabel);
            controller.secondBrightness = EditorGUILayout.Slider("明度", controller.secondBrightness, 0.0f, 1.0f);
            controller.secondSaturation = EditorGUILayout.Slider("彩度", controller.secondSaturation, 0.0f, 1.0f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("アクセントカラー", EditorStyles.boldLabel);
            controller.accentBrightness = EditorGUILayout.Slider("明度", controller.accentBrightness, 0.0f, 1.0f);
            controller.accentSaturation = EditorGUILayout.Slider("彩度", controller.accentSaturation, 0.0f, 1.0f);
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // 液体揺れ制御
        EditorGUILayout.LabelField("液体揺れ制御", EditorStyles.boldLabel);
        controller.liquidIntensity = EditorGUILayout.Slider("Liquid Intensity", controller.liquidIntensity, 0.0f, 2.0f);
        controller.liquidSpeed = EditorGUILayout.Slider("Liquid Speed", controller.liquidSpeed, 0.1f, 3.0f);
        controller.liquidComplexity = EditorGUILayout.Slider("Liquid Complexity", controller.liquidComplexity, 1.0f, 8.0f);
        controller.liquidViscosity = EditorGUILayout.Slider("Liquid Viscosity", controller.liquidViscosity, 0.1f, 2.0f);
        
        EditorGUILayout.Space();
        
        // 動的パラメータ制御
        EditorGUILayout.LabelField("動的パラメータ制御", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("波のスケール", EditorStyles.boldLabel);
        controller.baseWaveScale = EditorGUILayout.Slider("基準値", controller.baseWaveScale, 1.0f, 15.0f);
        controller.waveScaleVariation = EditorGUILayout.Slider("変動範囲", controller.waveScaleVariation, 0.0f, 10.0f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("液体強度", EditorStyles.boldLabel);
        controller.baseLiquidIntensity = EditorGUILayout.Slider("基準値", controller.baseLiquidIntensity, 0.0f, 2.0f);
        controller.liquidIntensityVariation = EditorGUILayout.Slider("変動範囲", controller.liquidIntensityVariation, 0.0f, 1.0f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("液体速度", EditorStyles.boldLabel);
        controller.baseLiquidSpeed = EditorGUILayout.Slider("基準値", controller.baseLiquidSpeed, 0.1f, 3.0f);
        controller.liquidSpeedVariation = EditorGUILayout.Slider("変動範囲", controller.liquidSpeedVariation, 0.0f, 2.0f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ノイズスケール", EditorStyles.boldLabel);
        controller.baseNoiseScale = EditorGUILayout.Slider("基準値", controller.baseNoiseScale, 0.5f, 8.0f);
        controller.noiseScaleVariation = EditorGUILayout.Slider("変動範囲", controller.noiseScaleVariation, 0.0f, 4.0f);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // シェーダーパラメータの直接制御
        if (controller.backgroundMaterial != null)
        {
            EditorGUILayout.LabelField("シェーダーパラメータ", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            // 基本色設定
            EditorGUILayout.LabelField("基本色設定", EditorStyles.boldLabel);
            if (controller.backgroundMaterial.HasProperty("_MainColor"))
            {
                controller.backgroundMaterial.SetColor("_MainColor", 
                    EditorGUILayout.ColorField("Main Color", controller.backgroundMaterial.GetColor("_MainColor")));
            }
            if (controller.backgroundMaterial.HasProperty("_SecondColor"))
            {
                controller.backgroundMaterial.SetColor("_SecondColor", 
                    EditorGUILayout.ColorField("Second Color", controller.backgroundMaterial.GetColor("_SecondColor")));
            }
            if (controller.backgroundMaterial.HasProperty("_AccentColor"))
            {
                controller.backgroundMaterial.SetColor("_AccentColor", 
                    EditorGUILayout.ColorField("Accent Color", controller.backgroundMaterial.GetColor("_AccentColor")));
            }
            
            EditorGUILayout.Space();
            
            // 波とノイズ設定
            EditorGUILayout.LabelField("波とノイズ設定", EditorStyles.boldLabel);
            if (controller.backgroundMaterial.HasProperty("_WaveSpeed"))
            {
                controller.backgroundMaterial.SetFloat("_WaveSpeed", 
                    EditorGUILayout.Slider("Wave Speed", controller.backgroundMaterial.GetFloat("_WaveSpeed"), 0.2f, 2.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_WaveScale"))
            {
                controller.backgroundMaterial.SetFloat("_WaveScale", 
                    EditorGUILayout.Slider("Wave Scale", controller.backgroundMaterial.GetFloat("_WaveScale"), 1.0f, 15.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_NoiseScale"))
            {
                controller.backgroundMaterial.SetFloat("_NoiseScale", 
                    EditorGUILayout.Slider("Noise Scale", controller.backgroundMaterial.GetFloat("_NoiseScale"), 0.5f, 8.0f));
            }
            
            EditorGUILayout.Space();
            
            // 深度と立体感設定
            EditorGUILayout.LabelField("深度と立体感設定", EditorStyles.boldLabel);
            if (controller.backgroundMaterial.HasProperty("_DepthFalloff"))
            {
                controller.backgroundMaterial.SetFloat("_DepthFalloff", 
                    EditorGUILayout.Slider("Depth Falloff", controller.backgroundMaterial.GetFloat("_DepthFalloff"), 0.5f, 8.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_DepthLayers"))
            {
                controller.backgroundMaterial.SetFloat("_DepthLayers", 
                    EditorGUILayout.Slider("Depth Layers", controller.backgroundMaterial.GetFloat("_DepthLayers"), 2.0f, 8.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_DepthIntensity"))
            {
                controller.backgroundMaterial.SetFloat("_DepthIntensity", 
                    EditorGUILayout.Slider("Depth Intensity", controller.backgroundMaterial.GetFloat("_DepthIntensity"), 0.5f, 3.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_ParallaxStrength"))
            {
                controller.backgroundMaterial.SetFloat("_ParallaxStrength", 
                    EditorGUILayout.Slider("Parallax Strength", controller.backgroundMaterial.GetFloat("_ParallaxStrength"), 0.0f, 1.0f));
            }
            
            EditorGUILayout.Space();
            
            // 光とエフェクト設定
            EditorGUILayout.LabelField("光とエフェクト設定", EditorStyles.boldLabel);
            if (controller.backgroundMaterial.HasProperty("_LightIntensity"))
            {
                controller.backgroundMaterial.SetFloat("_LightIntensity", 
                    EditorGUILayout.Slider("Light Intensity", controller.backgroundMaterial.GetFloat("_LightIntensity"), 0.0f, 3.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_CausticsIntensity"))
            {
                controller.backgroundMaterial.SetFloat("_CausticsIntensity", 
                    EditorGUILayout.Slider("Caustics Intensity", controller.backgroundMaterial.GetFloat("_CausticsIntensity"), 0.0f, 2.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_DistortionStrength"))
            {
                controller.backgroundMaterial.SetFloat("_DistortionStrength", 
                    EditorGUILayout.Slider("Distortion Strength", controller.backgroundMaterial.GetFloat("_DistortionStrength"), 0.0f, 0.5f));
            }
            if (controller.backgroundMaterial.HasProperty("_FresnelPower"))
            {
                controller.backgroundMaterial.SetFloat("_FresnelPower", 
                    EditorGUILayout.Slider("Fresnel Power", controller.backgroundMaterial.GetFloat("_FresnelPower"), 0.5f, 5.0f));
            }
            
            EditorGUILayout.Space();
            
            // 液体パラメータ
            EditorGUILayout.LabelField("液体パラメータ", EditorStyles.boldLabel);
            if (controller.backgroundMaterial.HasProperty("_LiquidIntensity"))
            {
                controller.backgroundMaterial.SetFloat("_LiquidIntensity", 
                    EditorGUILayout.Slider("Liquid Intensity", controller.backgroundMaterial.GetFloat("_LiquidIntensity"), 0.0f, 2.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_LiquidSpeed"))
            {
                controller.backgroundMaterial.SetFloat("_LiquidSpeed", 
                    EditorGUILayout.Slider("Liquid Speed", controller.backgroundMaterial.GetFloat("_LiquidSpeed"), 0.1f, 3.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_LiquidComplexity"))
            {
                controller.backgroundMaterial.SetFloat("_LiquidComplexity", 
                    EditorGUILayout.Slider("Liquid Complexity", controller.backgroundMaterial.GetFloat("_LiquidComplexity"), 1.0f, 8.0f));
            }
            if (controller.backgroundMaterial.HasProperty("_LiquidViscosity"))
            {
                controller.backgroundMaterial.SetFloat("_LiquidViscosity", 
                    EditorGUILayout.Slider("Liquid Viscosity", controller.backgroundMaterial.GetFloat("_LiquidViscosity"), 0.1f, 2.0f));
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // プリセット機能
            EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("デフォルト"))
            {
                SetDefaultValues(controller);
            }
            if (GUILayout.Button("液体強調"))
            {
                SetLiquidPreset(controller);
            }
            if (GUILayout.Button("立体感強調"))
            {
                SetDepthPreset(controller);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 動的パラメータ
        EditorGUILayout.LabelField("動的パラメータ", EditorStyles.boldLabel);
        controller.waveSpeedRange = EditorGUILayout.Vector2Field("Wave Speed Range", controller.waveSpeedRange);
        controller.lightIntensityRange = EditorGUILayout.Vector2Field("Light Intensity Range", controller.lightIntensityRange);
        controller.glitchRange = EditorGUILayout.Vector2Field("Glitch Range", controller.glitchRange);
        
        EditorGUILayout.Space();
        
        // 環境応答
        EditorGUILayout.LabelField("環境応答", EditorStyles.boldLabel);
        controller.respondToHeartCount = EditorGUILayout.Toggle("Respond To Heart Count", controller.respondToHeartCount);
        controller.respondToHeartActivity = EditorGUILayout.Toggle("Respond To Heart Activity", controller.respondToHeartActivity);
        
        // デバッグ情報
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("デバッグ情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"マテリアル: {(controller.backgroundMaterial != null ? controller.backgroundMaterial.name : "未設定")}");
            
            if (GUILayout.Button("背景フラッシュテスト"))
            {
                controller.TriggerBackgroundFlash(Color.red, 1f);
            }
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(controller);
        }
    }
    
    /// <summary>
    /// デフォルト値に設定
    /// </summary>
    void SetDefaultValues(BackgroundController controller)
    {
        if (controller.backgroundMaterial == null) return;
        
        // 基本色
        controller.backgroundMaterial.SetColor("_MainColor", new Color(0.05f, 0.4f, 0.7f, 1.0f));
        controller.backgroundMaterial.SetColor("_SecondColor", new Color(0.02f, 0.15f, 0.3f, 1.0f));
        controller.backgroundMaterial.SetColor("_AccentColor", new Color(0.3f, 0.9f, 1.0f, 1.0f));
        
        // 波とノイズ
        controller.backgroundMaterial.SetFloat("_WaveSpeed", 0.8f);
        controller.backgroundMaterial.SetFloat("_WaveScale", 5.0f);
        controller.backgroundMaterial.SetFloat("_NoiseScale", 2.5f);
        
        // 深度と立体感
        controller.backgroundMaterial.SetFloat("_DepthFalloff", 3.0f);
        controller.backgroundMaterial.SetFloat("_DepthLayers", 4.0f);
        controller.backgroundMaterial.SetFloat("_DepthIntensity", 1.5f);
        controller.backgroundMaterial.SetFloat("_ParallaxStrength", 0.3f);
        
        // 光とエフェクト
        controller.backgroundMaterial.SetFloat("_LightIntensity", 1.5f);
        controller.backgroundMaterial.SetFloat("_CausticsIntensity", 1.0f);
        controller.backgroundMaterial.SetFloat("_DistortionStrength", 0.1f);
        controller.backgroundMaterial.SetFloat("_FresnelPower", 2.0f);
        
        // 液体パラメータ
        controller.backgroundMaterial.SetFloat("_LiquidIntensity", 1.0f);
        controller.backgroundMaterial.SetFloat("_LiquidSpeed", 1.2f);
        controller.backgroundMaterial.SetFloat("_LiquidComplexity", 4.0f);
        controller.backgroundMaterial.SetFloat("_LiquidViscosity", 0.8f);
    }
    
    /// <summary>
    /// 液体強調プリセット
    /// </summary>
    void SetLiquidPreset(BackgroundController controller)
    {
        if (controller.backgroundMaterial == null) return;
        
        // 液体パラメータを強調
        controller.backgroundMaterial.SetFloat("_LiquidIntensity", 1.5f);
        controller.backgroundMaterial.SetFloat("_LiquidSpeed", 1.8f);
        controller.backgroundMaterial.SetFloat("_LiquidComplexity", 6.0f);
        controller.backgroundMaterial.SetFloat("_LiquidViscosity", 0.5f);
        
        // 歪みを強化
        controller.backgroundMaterial.SetFloat("_DistortionStrength", 0.2f);
        controller.backgroundMaterial.SetFloat("_WaveScale", 8.0f);
    }
    
    /// <summary>
    /// 立体感強調プリセット
    /// </summary>
    void SetDepthPreset(BackgroundController controller)
    {
        if (controller.backgroundMaterial == null) return;
        
        // 深度パラメータを強調
        controller.backgroundMaterial.SetFloat("_DepthLayers", 6.0f);
        controller.backgroundMaterial.SetFloat("_DepthIntensity", 2.5f);
        controller.backgroundMaterial.SetFloat("_ParallaxStrength", 0.6f);
        controller.backgroundMaterial.SetFloat("_DepthFalloff", 4.0f);
        
        // フレネル効果を強化
        controller.backgroundMaterial.SetFloat("_FresnelPower", 3.5f);
        controller.backgroundMaterial.SetFloat("_LightIntensity", 2.0f);
    }
}

