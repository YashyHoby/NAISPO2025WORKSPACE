using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;

public class AdvancedMemoryLeakFixer : MonoBehaviour
{
    [Header("高度なメモリリーク修正")]
    [Tooltip("メモリリーク修正を有効にする")]
    public bool enableAdvancedFix = true;
    
    [Tooltip("修正間隔（フレーム）")]
    public int fixInterval = 180; // 3秒ごと（軽量化）
    
    [Tooltip("強制クリーンアップ間隔（フレーム）")]
    public int forceCleanupInterval = 600; // 10秒ごと（軽量化）
    
    [Tooltip("メモリ使用量の閾値（MB）")]
    public float memoryThreshold = 150f;
    
    private int frameCount;
    private float lastMemoryUsage;
    private int consecutiveHighMemoryFrames;
    
    void Update()
    {
        if (!enableAdvancedFix) return;
        
        frameCount++;
        
        // 定期的なメモリリーク修正
        if (frameCount % fixInterval == 0)
        {
            PerformMemoryLeakFix();
        }
        
        // 強制クリーンアップ
        if (frameCount % forceCleanupInterval == 0)
        {
            ForceMemoryCleanup();
        }
        
        // 連続的な高メモリ使用量の監視
        MonitorMemoryUsage();
    }
    
    void PerformMemoryLeakFix()
    {
        // パーティクルシステムのクリーンアップ
        CleanupParticleSystems();
        
        // テクスチャのクリーンアップ
        CleanupTextures();
        
        // マテリアルのクリーンアップ
        CleanupMaterials();
        
        // ガベージコレクションの実行
        System.GC.Collect();
    }
    
    void ForceMemoryCleanup()
    {
        Debug.Log("[AdvancedMemoryLeakFixer] 強制メモリクリーンアップを実行");
        
        // すべてのパーティクルシステムを停止・クリア
        var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
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
        
        // メモリ使用量をログ出力
        float currentMemory = GetMemoryUsage();
        Debug.Log($"[AdvancedMemoryLeakFixer] クリーンアップ後メモリ使用量: {currentMemory:F2}MB");
    }
    
    void MonitorMemoryUsage()
    {
        float currentMemory = GetMemoryUsage();
        
        if (currentMemory > memoryThreshold)
        {
            consecutiveHighMemoryFrames++;
            
            if (consecutiveHighMemoryFrames >= 5) // 5フレーム連続で高メモリ使用量
            {
                Debug.LogWarning($"[AdvancedMemoryLeakFixer] 連続的な高メモリ使用量を検出: {currentMemory:F2}MB");
                ForceMemoryCleanup();
                consecutiveHighMemoryFrames = 0;
            }
        }
        else
        {
            consecutiveHighMemoryFrames = 0;
        }
        
        lastMemoryUsage = currentMemory;
    }
    
    void CleanupParticleSystems()
    {
        // パーティクルシステムの検索を軽量化（キャッシュを使用）
        if (Time.frameCount % 300 == 0) // 5秒ごとに検索
        {
            var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            foreach (var ps in particleSystems)
            {
                if (ps != null && ps.isPlaying)
                {
                    // 長時間再生されているパーティクルシステムをクリア
                    if (ps.time > 15f) // 15秒以上再生（閾値を上げる）
                    {
                        ps.Clear();
                    }
                }
            }
        }
    }
    
    void CleanupTextures()
    {
        // テクスチャのクリーンアップを軽量化（頻度を下げる）
        if (Time.frameCount % 600 == 0) // 10秒ごと
        {
            var textures = FindObjectsByType<Texture2D>(FindObjectsSortMode.None);
            foreach (var texture in textures)
            {
                if (texture != null && texture.name.Contains("WhiteTexture"))
                {
                    // 一時的なテクスチャをクリーンアップ
                    if (texture.hideFlags == HideFlags.DontSaveInEditor)
                    {
                        DestroyImmediate(texture);
                    }
                }
            }
        }
    }
    
    void CleanupMaterials()
    {
        // マテリアルのクリーンアップを軽量化（頻度を下げる）
        if (Time.frameCount % 600 == 0) // 10秒ごと
        {
            var materials = FindObjectsByType<Material>(FindObjectsSortMode.None);
            foreach (var material in materials)
            {
                if (material != null && material.name.Contains("SlimeMaterial"))
                {
                    // 一時的なマテリアルをクリーンアップ
                    if (material.hideFlags == HideFlags.DontSaveInEditor)
                    {
                        DestroyImmediate(material);
                    }
                }
            }
        }
    }
    
    float GetMemoryUsage()
    {
        return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    
    void OnGUI()
    {
        if (!enableAdvancedFix) return;
        
        float currentMemory = GetMemoryUsage();
        GUI.Box(new Rect(10, 120, 250, 80), 
            $"高度なメモリ修正\n" +
            $"メモリ使用量: {currentMemory:F2}MB\n" +
            $"フレーム: {frameCount}\n" +
            $"連続高メモリ: {consecutiveHighMemoryFrames}");
        
        if (GUI.Button(new Rect(10, 210, 100, 30), "強制クリーンアップ"))
        {
            ForceMemoryCleanup();
        }
    }
}
