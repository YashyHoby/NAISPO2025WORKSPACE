using UnityEngine;

[ExecuteAlways]
public class FlowRectSource2D : MonoBehaviour, IFlowSource2D
{
    [Header("Shape / Strength")]
    public float length = 5f;           // 流れ方向(+X)の長さ
    public float width  = 2f;           // 幅（±Y/2）
    public float strength = 4f;         // 基本流速の大きさ

    [Header("Profiles (0..1)")]
    [Tooltip("長手方向プロファイル t=x/L: 0=根元,1=出口")]
    public AnimationCurve lengthProfile = AnimationCurve.Linear(0, 1, 1, 0.5f);
    [Tooltip("幅方向プロファイル s=|2y/W|: 0=中央,1=端")]
    public AnimationCurve widthProfile  = AnimationCurve.Linear(0, 1, 1, 0.6f);

    [Header("Inflow (optional)")]
    public bool   inflowEnabled   = true;
    public float  inflowRadius    = 1.0f;
    public float  inflowStrength  = 2.0f;
    public AnimationCurve inflowProfile = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public Vector2 SampleVelocity(Vector2 worldPos)
    {
        // ローカル座標へ
        Vector2 p = transform.InverseTransformPoint(worldPos);
        float halfW = width * 0.5f;

        Vector2 u = Vector2.zero;

        // 矩形内部の主流（+X方向）
        if (p.x >= 0 && p.x <= length && Mathf.Abs(p.y) <= halfW)
        {
            float t = Mathf.Clamp01(p.x / Mathf.Max(0.0001f, length));   // 0..1
            float s = Mathf.Clamp01(Mathf.Abs(p.y) / Mathf.Max(0.0001f, halfW)); // 0..1
            float m = strength * lengthProfile.Evaluate(t) * widthProfile.Evaluate(s);
            u += (Vector2)transform.right * m; // +Xをワールドへ
        }

        // 根元吸い込み（矩形の外側：根元(-X側)に近い点）
        if (inflowEnabled && inflowRadius > 0f)
        {
            if (p.x < 0 && Mathf.Abs(p.y) <= halfW + inflowRadius)
            {
                float d = Mathf.Min(-p.x, inflowRadius);         // 根元からの奥行き
                float r = Mathf.Clamp01(d / inflowRadius);       // 0..1
                float w = inflowProfile.Evaluate(r);
                u += (Vector2)(-transform.right) * (inflowStrength * w);
            }
        }

        return u;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, .7f, 1f, .25f);
        Vector3 c = transform.TransformPoint(new Vector3(length * .5f, 0, 0));
        Vector3 sx = transform.right * length;
        Vector3 sy = transform.up * width;
        Vector3 p0 = c - sx * .5f - sy * .5f;
        Vector3 p1 = c + sx * .5f - sy * .5f;
        Vector3 p2 = c + sx * .5f + sy * .5f;
        Vector3 p3 = c - sx * .5f + sy * .5f;
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);

        // 方向表示
        Gizmos.color = new Color(0f, .9f, .9f, .8f);
        Vector3 a = transform.position;
        Gizmos.DrawLine(a, a + transform.right * Mathf.Max(1f, length * 0.2f));
    }
}
