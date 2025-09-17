using UnityEngine;

/// <summary>
/// 心拍情報(HeartProfile)から 形/色/半径を決定論的に割り当てるマッパー。
/// 値域や分布の偏りに合わせてガンマで補正し、見た目が“そこそこ一様”にばらけるよう調整。
/// </summary>
public class HeartAppearanceMapper : MonoBehaviour
{
    [Header("Expected Ranges (tune to your dataset)")]
    [Tooltip("HR (bpm) の想定下限/上限。正規化に使用")]
    public Vector2 hrRange   = new Vector2(50f, 120f);
    [Tooltip("CV の想定下限/上限。正規化に使用")]
    public Vector2 cvRange   = new Vector2(0.05f, 0.50f);
    [Tooltip("Range の想定下限/上限。正規化に使用")]
    public Vector2 rngRange  = new Vector2(10f, 35f);
    [Tooltip("Mean の想定下限/上限。正規化に使用。半径に使用")]
    public Vector2 meanRange = new Vector2(65f, 95f);

    [Header("Distribution Shaping (gamma) 0.1~3")]
    [Tooltip("HR 正規化値のガンマ。<1 で伸ばし/ >1 でつぶす。")]
    public float hrGamma   = 0.9f;
    [Tooltip("CV 正規化値のガンマ")]
    public float cvGamma   = 0.9f;
    [Tooltip("Range 正規化値のガンマ")]
    public float rngGamma  = 0.9f;
    [Tooltip("Mean 正規化値のガンマ（半径に使用）")]
    public float meanGamma = 0.9f;

    [Header("Color (HSV)")]
    [Tooltip("Sの彩度レンジ: 下限/上限")]
    public Vector2 satRange = new Vector2(0.55f, 0.95f);
    [Tooltip("Vの明度レンジ: 下限/上限")]
    public Vector2 valRange = new Vector2(0.85f, 1.00f);
    [Tooltip("Hue を HR 基準で決める際の重み。cv/range の影響度。")]
    public Vector3 hueWeights = new Vector3(0.65f, 0.25f, 0.10f); // (hr, cv, range)

    [Header("Radius (pixels or world units)")]
    [Tooltip("初期半径の下限/上限（HeartVisual.radius へ適用）")]
    public Vector2 radiusRange = new Vector2(0.15f, 0.45f);
    [Tooltip("Spawn 時に radius を上書きするか。HeartVisual.SetRadius によって Transform スケールも同期。")]
    public bool setRadiusOnSpawn = true;

    [Header("Shape")]
    [Tooltip("シーンに存在する ShapeType の総数。HeartVisualの列挙数に合わせる。")]
    public int shapeCount = 3; // 仮: 0=Circle,1=Triangle,2=Box

    // === 公開API: これを呼べば見た目が決まる ===
    public void Apply(HeartProfile hp, HeartVisual vis)
    {
        if (hp == null || vis == null) return;

        // 1) 正規化 + 分布シェイプ: pow 正規化
        float h = PowNorm(hp.hr,    hrRange,   hrGamma);
        float c = PowNorm(hp.cv,    cvRange,   cvGamma);
        float r = PowNorm(hp.range, rngRange,  rngGamma);
        float m = PowNorm(hp.mean,  meanRange, meanGamma);

        // 2) 形状: 準一様化して 0..1 → 0..shapeCount-1
        //    互いに無相関っぽい係数で線形結合 → frac で丸め、値域偏りでも程よく散る。
        float u = Frac(h * 0.754877666f + c * 0.569840291f + r * 0.438695021f + m * 0.271828182f);
        int idx = Mathf.Clamp(Mathf.FloorToInt(u * shapeCount), 0, shapeCount - 1);
        vis.shapeType = (ShapeType)idx;

        // 3) 色: HSV
        //    Hue: HR主軸 + CV/Rangeで微拡散 → 0..1 wrap
        float hue = Frac(h * hueWeights.x + c * hueWeights.y + r * hueWeights.z);
        float sat = Mathf.Lerp(satRange.x, satRange.y, c); // CV 高いほど鮮やか
        float val = Mathf.Lerp(valRange.x, valRange.y, r); // Range 広いほど明るめ
        vis.color = Color.HSVToRGB(hue, sat, val);

        // 4) 半径: Mean 主軸。pow正規済み m を使用。
        if (setRadiusOnSpawn)
        {
            vis.SetRadius(Mathf.Lerp(radiusRange.x, radiusRange.y, m));
        }
    }

    // --- Helpers ---
    static float Frac(float x) => x - Mathf.Floor(x);

    static float PowNorm(float v, Vector2 minmax, float gamma)
    {
        float t = 0.5f;
        if (minmax.y > minmax.x)
        {
            t = Mathf.InverseLerp(minmax.x, minmax.y, v);
        }
        t = Mathf.Clamp01(t);
        // ガンマ<1: 中央を伸ばし端を引き伸ばす → 均一化効き目
        // ガンマ>1: 中央をつぶして端を強調
        return Mathf.Pow(t, Mathf.Max(0.01f, gamma));
    }
}
