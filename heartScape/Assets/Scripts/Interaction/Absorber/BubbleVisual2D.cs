using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class BubbleVisual2D : MonoBehaviour
{
    [Header("Render Targets")]
    public int dyeSize = 256;
    public Material liquidMat;   // Unlit/BubbleLiquid2D を割り当て
    public Material shellMat;    // Unlit/BubbleShell2D   を割り当て（別スプライトでもOK）

    [Header("Dye Params")]
    [Range(0f,1f)] public float injectRadius = 0.08f;
    [Range(0f,1f)] public float injectPower  = 0.9f;
    [Range(0f,1f)] public float decay = 0.995f; // 1で減衰なし
    [Range(0f,1f)] public float blur = 0.25f;

    Texture2D dyeTex; Color[] pixels;
    // Heartごとに割当チャネル（0..3）
    Dictionary<int,int> heartIdToChannel = new();

    void Awake()
    {
        dyeTex = new Texture2D(dyeSize, dyeSize, TextureFormat.RGBAFloat, false, true);
        pixels = new Color[dyeSize*dyeSize];
        ClearDye();
        if (liquidMat) liquidMat.SetTexture("_DyeTex", dyeTex);
    }

    void Update()
    {
        // 徐々に減衰＆軽いボックスブラー
        BoxBlurAndDecay();
        dyeTex.SetPixels(pixels);
        dyeTex.Apply(false, false);
    }

    public void ClearDye()
    {
        for (int i=0; i<pixels.Length; i++) pixels[i] = Color.clear;
        if (dyeTex){ dyeTex.SetPixels(pixels); dyeTex.Apply(false,false); }
    }

    // HeartAbsorber2D から呼ぶ
    public void InjectFromWorld(Vector2 worldPos, int heartId, Color heartColor)
    {
        int ch = GetOrAssignChannel(heartId);
        Vector2 uv = WorldToLocal01(worldPos);
        PaintDisc(uv, ch);
        // パレットも同期（4つまで）
        if (liquidMat){
            string pname = ch switch{ 0 => "_Color0", 1 => "_Color1", 2 => "_Color2", _ => "_Color3"};
            liquidMat.SetColor(pname, heartColor);
        }
    }

    int GetOrAssignChannel(int heartId)
    {
        if (heartIdToChannel.TryGetValue(heartId, out int ch)) return ch;
        ch = Mathf.Clamp(heartIdToChannel.Count, 0, 3); // 4色まで
        heartIdToChannel[heartId] = ch;
        return ch;
    }

    Vector2 WorldToLocal01(Vector2 world)
    {
        Vector2 local = transform.InverseTransformPoint(world);
        // assume sprite quad [-0.5..0.5] x [-0.5..0.5]
        return local + new Vector2(0.5f, 0.5f);
    }

    void PaintDisc(Vector2 uv01, int channel)
    {
        int cx = Mathf.FloorToInt(uv01.x * dyeSize);
        int cy = Mathf.FloorToInt(uv01.y * dyeSize);
        int rad = Mathf.Max(1, Mathf.RoundToInt(injectRadius * dyeSize));
        float r2 = rad*rad;

        for (int y=-rad; y<=rad; y++){
            int yy = cy + y; if (yy<0||yy>=dyeSize) continue;
            for (int x=-rad; x<=rad; x++){
                int xx = cx + x; if (xx<0||xx>=dyeSize) continue;
                if (x*x + y*y > r2) continue;
                int idx = yy*dyeSize + xx;
                Color c = pixels[idx];
                float v = injectPower;
                switch(channel){
                    case 0: c.r = Mathf.Clamp01(c.r + v); break;
                    case 1: c.g = Mathf.Clamp01(c.g + v); break;
                    case 2: c.b = Mathf.Clamp01(c.b + v); break;
                    default: c.a = Mathf.Clamp01(c.a + v); break;
                }
                pixels[idx] = c;
            }
        }
    }

    void BoxBlurAndDecay()
    {
        // 3x3ボックスブラー＋減衰（軽量）
        var src = pixels;
        var dst = new Color[src.Length];
        for (int y=1; y<dyeSize-1; y++){
            for (int x=1; x<dyeSize-1; x++){
                int i = y*dyeSize + x;
                Color sum = Color.black;
                for(int j=-1;j<=1;j++)
                    for(int k=-1;k<=1;k++)
                        sum += src[(y+j)*dyeSize + (x+k)];
                Color v = sum / 9f;
                // 減衰
                v.r *= decay; v.g *= decay; v.b *= decay; v.a *= decay;
                // ブラー強度を線形補間
                dst[i] = Color.Lerp(src[i], v, blur);
            }
        }
        pixels = dst;
    }

    // 破裂時に呼ぶ
    public List<Color> GetPalette()
    {
        var list = new List<Color>();
        if (liquidMat){
            list.Add(liquidMat.GetColor("_Color0"));
            list.Add(liquidMat.GetColor("_Color1"));
            list.Add(liquidMat.GetColor("_Color2"));
            list.Add(liquidMat.GetColor("_Color3"));
        }
        return list;
    }
}
