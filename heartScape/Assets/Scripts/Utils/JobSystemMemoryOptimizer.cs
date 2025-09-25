using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

public class JobSystemMemoryOptimizer : MonoBehaviour
{
    [Header("Job System メモリ最適化")]
    [Tooltip("メモリ最適化を有効にする")]
    public bool enableOptimization = true;
    
    [Tooltip("最適化間隔（フレーム）")]
    public int optimizationInterval = 60;
    
    [Tooltip("最大メモリ使用量（MB）")]
    public float maxMemoryUsage = 200f;
    
    private int frameCount;
    private List<JobHandle> activeJobs = new List<JobHandle>();
    
    void Update()
    {
        if (!enableOptimization) return;
        
        frameCount++;
        
        if (frameCount % optimizationInterval == 0)
        {
            OptimizeMemoryUsage();
        }
    }
    
    void OptimizeMemoryUsage()
    {
        // アクティブなJobを完了させる
        CompleteActiveJobs();
        
        // メモリ使用量をチェック
        float currentMemory = GetMemoryUsage();
        if (currentMemory > maxMemoryUsage)
        {
            Debug.LogWarning($"[JobSystemMemoryOptimizer] メモリ使用量が閾値を超えました: {currentMemory:F2}MB");
            
            // 強制的にガベージコレクションを実行
            ForceGarbageCollection();
        }
    }
    
    void CompleteActiveJobs()
    {
        // アクティブなJobを完了させる
        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            if (activeJobs[i].IsCompleted)
            {
                activeJobs[i].Complete();
                activeJobs.RemoveAt(i);
            }
        }
    }
    
    float GetMemoryUsage()
    {
        return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    
    void ForceGarbageCollection()
    {
        // すべてのアクティブなJobを完了させる
        foreach (var job in activeJobs)
        {
            if (!job.IsCompleted)
            {
                job.Complete();
            }
        }
        activeJobs.Clear();
        
        // ガベージコレクションを強制実行
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        
        Debug.Log("[JobSystemMemoryOptimizer] 強制ガベージコレクションを実行しました");
    }
    
    /// <summary>
    /// Jobを登録してメモリ使用量を監視
    /// </summary>
    public void RegisterJob(JobHandle job)
    {
        if (enableOptimization)
        {
            activeJobs.Add(job);
        }
    }
    
    /// <summary>
    /// 完了したJobを登録から削除
    /// </summary>
    public void UnregisterJob(JobHandle job)
    {
        activeJobs.Remove(job);
    }
    
    void OnDestroy()
    {
        // すべてのアクティブなJobを完了させる
        foreach (var job in activeJobs)
        {
            if (!job.IsCompleted)
            {
                job.Complete();
            }
        }
        activeJobs.Clear();
    }
}
