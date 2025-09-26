using UnityEngine;
using System.Collections;

public class PerformanceOptimizer : MonoBehaviour
{
    [Header("パフォーマンス最適化")]
    [Tooltip("パフォーマンス最適化を有効にする")]
    public bool enableOptimization = true;
    
    [Tooltip("フレームレートの目標値")]
    public int targetFrameRate = 60;
    
    [Tooltip("品質設定の自動調整")]
    public bool autoQualityAdjustment = true;
    
    [Tooltip("低フレームレート時の品質低下")]
    public bool enableQualityDegradation = true;
    
    [Tooltip("品質低下の閾値（FPS）")]
    public float lowFpsThreshold = 45f;
    
    private float currentFps;
    private int frameCount;
    private float deltaTime;
    private int qualityLevel = 0;
    
    void Start()
    {
        if (enableOptimization)
        {
            Application.targetFrameRate = targetFrameRate;
            StartCoroutine(MonitorPerformance());
        }
    }
    
    IEnumerator MonitorPerformance()
    {
        while (enableOptimization)
        {
            yield return new WaitForSeconds(1f); // 1秒ごとに監視
            
            // フレームレートを計算
            currentFps = 1f / deltaTime;
            
            // 品質の自動調整
            if (autoQualityAdjustment)
            {
                AdjustQuality();
            }
        }
    }
    
    void Update()
    {
        frameCount++;
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f; // スムージング
        
        // 低フレームレート時の緊急対策
        if (enableQualityDegradation && currentFps < lowFpsThreshold)
        {
            ApplyEmergencyOptimizations();
        }
    }
    
    void AdjustQuality()
    {
        if (currentFps < 30f && qualityLevel < 2)
        {
            // 品質を下げる
            qualityLevel++;
            ApplyQualitySettings();
            Debug.Log($"[PerformanceOptimizer] 品質を下げました: Level {qualityLevel}");
        }
        else if (currentFps > 55f && qualityLevel > 0)
        {
            // 品質を上げる
            qualityLevel--;
            ApplyQualitySettings();
            Debug.Log($"[PerformanceOptimizer] 品質を上げました: Level {qualityLevel}");
        }
    }
    
    void ApplyQualitySettings()
    {
        switch (qualityLevel)
        {
            case 0: // 高品質
                QualitySettings.SetQualityLevel(2);
                break;
            case 1: // 中品質
                QualitySettings.SetQualityLevel(1);
                break;
            case 2: // 低品質
                QualitySettings.SetQualityLevel(0);
                break;
        }
    }
    
    void ApplyEmergencyOptimizations()
    {
        // パーティクルシステムの制限
        var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 100); // 最大100パーティクルに制限
            }
        }
        
        // メモリクリーンアップの頻度を上げる
        System.GC.Collect();
    }
    
    void OnGUI()
    {
        if (!enableOptimization) return;
        
        GUI.Box(new Rect(10, 500, 200, 80), 
            $"パフォーマンス最適化\n" +
            $"FPS: {currentFps:F1}\n" +
            $"品質レベル: {qualityLevel}\n" +
            $"フレーム: {frameCount}");
        
        if (GUI.Button(new Rect(10, 590, 100, 30), "緊急最適化"))
        {
            ApplyEmergencyOptimizations();
        }
    }
}


