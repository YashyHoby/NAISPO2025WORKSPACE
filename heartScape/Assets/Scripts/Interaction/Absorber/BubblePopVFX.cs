using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// シンプルな花火エフェクトシステム
/// liquidオブジェクトの色に基づく美しい花火を生成
/// </summary>
public class BubblePopVFX : MonoBehaviour
{
    [Tooltip("吸収泡の液体サイト。カラー抽出に使用します。")]
    public BubbleLiquidSites2D liquidSites;

    [Tooltip("互換用：旧 BubbleVisual2D を参照する場合に設定します。")]
    public BubbleVisual2D legacyBubbleVisual;

    [Header("シンプル花火システム")]
    [Tooltip("シンプル花火システム")]
    public Component fireworkSystem;

    readonly List<Color> paletteBuffer = new();

    void Awake()
    {
        // シンプル花火システムを自動取得
        if (fireworkSystem == null)
        {
            fireworkSystem = GetComponent("SimpleFireworkSystem") as Component;
            if (fireworkSystem == null)
            {
                // より確実な方法で型を取得
                System.Type fireworkType = null;
                string[] typeNames = {
                    "SimpleFireworkSystem, Assembly-CSharp",
                    "SimpleFireworkSystem, Assembly-CSharp-firstpass",
                    "SimpleFireworkSystem"
                };
                
                foreach (string typeName in typeNames)
                {
                    fireworkType = System.Type.GetType(typeName);
                    if (fireworkType != null) break;
                }
                
                if (fireworkType != null)
                {
                    fireworkSystem = gameObject.AddComponent(fireworkType) as Component;
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // 花火システムをクリーンアップ
        if (fireworkSystem != null)
        {
            fireworkSystem.SendMessage("StopFirework", null, SendMessageOptions.DontRequireReceiver);
        }
    }


    void GatherPalette()
    {
        paletteBuffer.Clear();

        if (liquidSites != null)
            liquidSites.GetPalette(paletteBuffer);

        if (paletteBuffer.Count == 0 && legacyBubbleVisual != null)
            paletteBuffer.AddRange(legacyBubbleVisual.GetPalette());

        if (paletteBuffer.Count == 0)
            paletteBuffer.Add(Color.white);
    }

    public void PlayPop()
    {
        GatherPalette();
        
        // シンプル花火システムを開始
        if (fireworkSystem != null)
        {
            fireworkSystem.SendMessage("StartFirework", paletteBuffer, SendMessageOptions.DontRequireReceiver);
        }
    }
}
