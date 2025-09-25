using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MemoryLeakDetector : MonoBehaviour
{
    [Header("メモリリーク検出設定")]
    [Tooltip("メモリリーク検出を有効にする")]
    public bool enableDetection = true;
    
    [Tooltip("検出間隔（フレーム）")]
    public int detectionInterval = 60;
    
    [Tooltip("メモリ使用量の閾値（MB）")]
    public float memoryThreshold = 100f;
    
    private float lastMemoryUsage;
    private int frameCount;
    private List<float> memoryHistory = new List<float>();
    
    void Update()
    {
        if (!enableDetection) return;
        
        frameCount++;
        
        if (frameCount % detectionInterval == 0)
        {
            CheckMemoryUsage();
        }
    }
    
    void CheckMemoryUsage()
    {
        float currentMemory = GetMemoryUsage();
        memoryHistory.Add(currentMemory);
        
        // 履歴を保持（最新10回分）
        if (memoryHistory.Count > 10)
        {
            memoryHistory.RemoveAt(0);
        }
        
        // メモリ使用量が閾値を超えた場合
        if (currentMemory > memoryThreshold)
        {
            Debug.LogWarning($"[MemoryLeakDetector] メモリ使用量が閾値を超えました: {currentMemory:F2}MB");
            
            // メモリ使用量が増加傾向にある場合
            if (memoryHistory.Count >= 3)
            {
                float averageIncrease = 0f;
                for (int i = 1; i < memoryHistory.Count; i++)
                {
                    averageIncrease += memoryHistory[i] - memoryHistory[i - 1];
                }
                averageIncrease /= (memoryHistory.Count - 1);
                
                if (averageIncrease > 1f) // 1MB以上増加
                {
                    Debug.LogError($"[MemoryLeakDetector] メモリリークの可能性があります。平均増加量: {averageIncrease:F2}MB");
                    LogMemoryUsageDetails();
                }
            }
        }
        
        lastMemoryUsage = currentMemory;
    }
    
    float GetMemoryUsage()
    {
        return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    
    void LogMemoryUsageDetails()
    {
        Debug.Log($"[MemoryLeakDetector] メモリ使用量詳細:");
        Debug.Log($"- 総メモリ使用量: {GetMemoryUsage():F2}MB");
        Debug.Log($"- フレーム数: {frameCount}");
        Debug.Log($"- 履歴: {string.Join(", ", memoryHistory.Select(m => $"{m:F1}MB"))}");
        
        // パーティクルシステムの情報
        var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        Debug.Log($"- アクティブなパーティクルシステム数: {particleSystems.Length}");
        
        foreach (var ps in particleSystems)
        {
            if (ps.isPlaying)
            {
                var main = ps.main;
                Debug.Log($"  - {ps.name}: 最大パーティクル数={main.maxParticles}, 現在のパーティクル数={ps.particleCount}");
            }
        }
    }
    
    void OnGUI()
    {
        if (!enableDetection) return;
        
        GUI.Box(new Rect(10, 10, 200, 100), $"メモリ使用量: {GetMemoryUsage():F2}MB\nフレーム: {frameCount}");
        
        if (GUI.Button(new Rect(10, 120, 100, 30), "詳細ログ"))
        {
            LogMemoryUsageDetails();
        }
    }
}
