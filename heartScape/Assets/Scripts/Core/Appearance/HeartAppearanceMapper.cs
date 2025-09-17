using UnityEngine;

/// <summary>
/// HeartProfile の数値から HeartVisual の見た目（カラー・サイズ・形状）を算出するマッパー。
/// </summary>
public class HeartAppearanceMapper : MonoBehaviour
{
    [Header("Expected Ranges (tune to your dataset)")]
    [Tooltip("HR (bpm) の想定最小/最大。統計レンジに合わせること")]
    public Vector2 hrRange   = new Vector2(50f, 120f);
    [Tooltip("CV の想定最小/最大。統計レンジに合わせること")]
    public Vector2 cvRange   = new Vector2(0.05f, 0.50f);
    [Tooltip("Range の想定最小/最大。統計レンジに合わせること")]
    public Vector2 rngRange  = new Vector2(10f, 35f);
    [Tooltip("Mean の想定最小/最大。統計レンジに合わせること")]
    public Vector2 meanRange = new Vector2(65f, 95f);

    [Header("Distribution Shaping (gamma) 0.1~3")]
    [Tooltip("HR 正規化時のガンマ。<1: 低値を広げる / >1: 高値を強調")] public float hrGamma   = 0.9f;
    [Tooltip("CV 正規化時のガンマ")]                                     public float cvGamma   = 0.9f;
    [Tooltip("Range 正規化時のガンマ")]                                  public float rngGamma  = 0.9f;
    [Tooltip("Mean 正規化時のガンマ")]                                   public float meanGamma = 0.9f;

    [Header("Color (HSV)")]
    [Tooltip("Saturation のレンジ")] public Vector2 satRange = new Vector2(0.55f, 0.95f);
    [Tooltip("Value のレンジ")]      public Vector2 valRange = new Vector2(0.85f, 1.00f);
    [Tooltip("Hue 決定時の重み (hr, cv, range)")] public Vector3 hueWeights = new Vector3(0.65f, 0.25f, 0.10f);

    [Header("Radius (pixels or world units)")]
    [Tooltip("視覚半径のレンジ。HeartVisual.SetRadius に適用")] public Vector2 radiusRange = new Vector2(0.15f, 0.45f);
    [Tooltip("Spawn 時に radius を自動適用するか。false の場合は外部で制御")] public bool setRadiusOnSpawn = true;

    [Header("Shape (legacy)")]
    [Tooltip("旧システムの ShapeType 数。後方互換のため残置。現在は未使用")] public int shapeCount = 3;

    public void Apply(HeartProfile hp, HeartVisual vis)
    {
        if (hp == null || vis == null) return;

        // 1) 正規化 + ガンマ補正
        float h = PowNorm(hp.hr,    hrRange,   hrGamma);
        float c = PowNorm(hp.cv,    cvRange,   cvGamma);
        float r = PowNorm(hp.range, rngRange,  rngGamma);
        float m = PowNorm(hp.mean,  meanRange, meanGamma);

        // 2) 形状パラメータを算出（0..1 の指標を複合して周期性を持たせる）
        float selector     = Frac(h * 0.754877666f + c * 0.569840291f + r * 0.438695021f + m * 0.271828182f);
        float irregularity = Frac(h * 0.915965594f + c * 0.577215664f + r * 0.618033989f + m * 0.414213562f);
        float orientation  = Frac(h * 0.333333333f + c * 0.211324865f + r * 0.707106781f + m * 0.553574359f);
        float seed         = Frac(h * 0.873662f    + c * 0.632459f    + r * 0.521379f    + m * 0.414494f);

        var shapeParams = new HeartVisual.ProceduralShapeParameters
        {
            normalizedHr    = h,
            normalizedCv    = c,
            normalizedRange = r,
            normalizedMean  = m,
            shapeSelector   = selector,
            irregularity    = irregularity,
            orientation     = orientation,
            seed            = seed
        };

        int vertexCount = vis.ApplyProceduralShape(shapeParams);
        vis.shapeType = vertexCount == 0 ? ShapeType.Circle : ShapeType.Box;

        // 3) 色（HSV）
        float hue = Frac(h * hueWeights.x + c * hueWeights.y + r * hueWeights.z);
        float sat = Mathf.Lerp(satRange.x, satRange.y, c);
        float val = Mathf.Lerp(valRange.x, valRange.y, r);
        vis.color = Color.HSVToRGB(hue, sat, val);

        // 4) サイズ（Transform スケール経由で半径を適用）
        if (setRadiusOnSpawn)
        {
            vis.SetRadius(Mathf.Lerp(radiusRange.x, radiusRange.y, m));
        }
    }

    static float Frac(float x) => x - Mathf.Floor(x);

    static float PowNorm(float v, Vector2 minmax, float gamma)
    {
        float t = 0.5f;
        if (minmax.y > minmax.x)
        {
            t = Mathf.InverseLerp(minmax.x, minmax.y, v);
        }
        t = Mathf.Clamp01(t);
        return Mathf.Pow(t, Mathf.Max(0.01f, gamma));
    }
}
