using UnityEngine;

[ExecuteAlways]
public class FlowRadialSource2D : MonoBehaviour, IFlowSource2D
{
    [Header("プロファイル設定")]
    [Tooltip("中心から外向きに押し出す力の最大値です。")]
    public float strength = 5f;

    [Tooltip("フローの影響が及ぶ外側の半径（ワールド単位）です。")]
    public float radius = 8f;

    [Tooltip("中心付近で流れを発生させない内側の半径（ワールド単位）です。")]
    public float innerRadius = 0f;

    [Tooltip("距離に応じて強度を変化させるプロファイル（0 = 内側端、1 = 外側端）です。")]
    public AnimationCurve radialProfile = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("フォールオフ設定")]
    [Tooltip("外側半径を超えた際に緩やかに減衰させる幅です。0 にすると減衰しません。")]
    public float edgeFalloff = 0.5f;

    public Vector2 SampleVelocity(Vector2 worldPos)
    {
        Vector2 offset = worldPos - (Vector2)transform.position;
        float dist = offset.magnitude;

        float outer = Mathf.Max(0.0001f, radius);
        float inner = Mathf.Clamp(innerRadius, 0f, outer);

        if (dist <= inner)
            return Vector2.zero;

        float range = Mathf.Max(0.0001f, outer - inner);
        float t = Mathf.Clamp01((dist - inner) / range);
        float profile = Mathf.Clamp01(radialProfile.Evaluate(t));

        float falloffFactor = 1f;
        if (edgeFalloff > 0f && dist > outer)
        {
            float beyond = dist - outer;
            float width = Mathf.Max(0.0001f, edgeFalloff);
            falloffFactor = Mathf.Clamp01(1f - beyond / width);
        }
        else if (dist > outer)
        {
            return Vector2.zero;
        }

        if (profile <= 0f || falloffFactor <= 0f)
            return Vector2.zero;

        Vector2 dir = offset / dist;
        return dir * (strength * profile * falloffFactor);
    }

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position;

        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(origin, radius);

        if (innerRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(origin, Mathf.Clamp(innerRadius, 0f, radius));
        }

        if (edgeFalloff > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.05f, 0.1f);
            Gizmos.DrawWireSphere(origin, radius + Mathf.Max(0f, edgeFalloff));
        }

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Vector3 right = transform.right;
        Gizmos.DrawLine(origin, origin + right * Mathf.Min(radius, 2f));
    }
}