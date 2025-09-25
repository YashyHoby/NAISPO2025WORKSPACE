using UnityEngine;
using System.Collections.Generic;

public class MemoryLeakManager : MonoBehaviour
{
    [Header("メモリリーク管理")]
    [Tooltip("メモリリーク管理を有効にする")]
    public bool enableManagement = true;
    
    [Tooltip("管理間隔（フレーム）")]
    public int managementInterval = 300; // 5秒ごと（軽量化）
    
    [Tooltip("緊急クリーンアップの閾値（MB）")]
    public float emergencyThreshold = 200f;
    
    private List<MemoryLeakDetector> detectors = new List<MemoryLeakDetector>();
    private List<JobSystemMemoryOptimizer> optimizers = new List<JobSystemMemoryOptimizer>();
    private List<AdvancedMemoryLeakFixer> fixers = new List<AdvancedMemoryLeakFixer>();
    
    private int frameCount;
    private float lastMemoryUsage;
    
    void Start()
    {
        // 既存のメモリリーク対策コンポーネントを検索
        FindMemoryLeakComponents();
    }
    
    void Update()
    {
        if (!enableManagement) return;
        
        frameCount++;
        
        if (frameCount % managementInterval == 0)
        {
            ManageMemoryLeaks();
        }
    }
    
    void FindMemoryLeakComponents()
    {
        // メモリリーク検出コンポーネントを検索
        detectors.AddRange(FindObjectsByType<MemoryLeakDetector>(FindObjectsSortMode.None));
        
        // Job System最適化コンポーネントを検索
        optimizers.AddRange(FindObjectsByType<JobSystemMemoryOptimizer>(FindObjectsSortMode.None));
        
        // 高度なメモリリーク修正コンポーネントを検索
        fixers.AddRange(FindObjectsByType<AdvancedMemoryLeakFixer>(FindObjectsSortMode.None));
        
        Debug.Log($"[MemoryLeakManager] 検出されたコンポーネント: 検出器={detectors.Count}, 最適化器={optimizers.Count}, 修正器={fixers.Count}");
    }
    
    void ManageMemoryLeaks()
    {
        float currentMemory = GetMemoryUsage();
        
        // 緊急クリーンアップの実行
        if (currentMemory > emergencyThreshold)
        {
            Debug.LogWarning($"[MemoryLeakManager] 緊急メモリクリーンアップを実行: {currentMemory:F2}MB");
            PerformEmergencyCleanup();
        }
        
        // メモリ使用量の監視
        if (currentMemory > lastMemoryUsage + 10f) // 10MB以上増加
        {
            Debug.LogWarning($"[MemoryLeakManager] メモリ使用量が急激に増加: {currentMemory:F2}MB (+{currentMemory - lastMemoryUsage:F2}MB)");
        }
        
        lastMemoryUsage = currentMemory;
    }
    
    void PerformEmergencyCleanup()
    {
        // すべてのパーティクルシステムを停止
        var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop();
                ps.Clear();
            }
        }
        
        // 未使用のアセットをアンロード
        Resources.UnloadUnusedAssets();
        
        // 強制ガベージコレクション
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        Debug.Log($"[MemoryLeakManager] 緊急クリーンアップ完了: {GetMemoryUsage():F2}MB");
    }
    
    float GetMemoryUsage()
    {
        return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    
    void OnGUI()
    {
        if (!enableManagement) return;
        
        float currentMemory = GetMemoryUsage();
        GUI.Box(new Rect(10, 300, 300, 100), 
            $"メモリリーク管理\n" +
            $"メモリ使用量: {currentMemory:F2}MB\n" +
            $"フレーム: {frameCount}\n" +
            $"検出器: {detectors.Count}\n" +
            $"最適化器: {optimizers.Count}\n" +
            $"修正器: {fixers.Count}");
        
        if (GUI.Button(new Rect(10, 410, 120, 30), "緊急クリーンアップ"))
        {
            PerformEmergencyCleanup();
        }
        
        if (GUI.Button(new Rect(140, 410, 120, 30), "コンポーネント再検索"))
        {
            FindMemoryLeakComponents();
        }
    }
}
