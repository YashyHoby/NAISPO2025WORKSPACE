using UnityEngine;

[ExecuteAlways]
public class FlowRadialSource2D : MonoBehaviour, IFlowSource2D
{
    [Header("Profile")] 
    [Tooltip("Maximum outward strength applied from the centre.")]
    public float strength = 5f;

    [Tooltip("Radius of the area influenced by the flow (world units).")]
    public float radius = 8f;

    [Tooltip("Strength profile sampled with distance (0 = centre, 1 = radius).")]
    public AnimationCurve radialProfile = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Falloff")] 
    [Tooltip("Soft falloff width beyond the radius (0 disables the falloff).")]
    public float edgeFalloff = 0.5f;

    public Vector2 SampleVelocity(Vector2 worldPos)
    {
        Vector2 offset = worldPos - (Vector2)transform.position;
        float dist = offset.magnitude;

        if (dist <= 1e-5f)
            return Vector2.zero;

        float maxR = Mathf.Max(0.0001f, radius);
        float t = Mathf.Clamp01(dist / maxR);
        float profile = Mathf.Clamp01(radialProfile.Evaluate(t));

        float falloffFactor = 1f;
        if (edgeFalloff > 0f && dist > maxR)
        {
            float beyond = dist - maxR;
            float width = Mathf.Max(0.0001f, edgeFalloff);
            falloffFactor = Mathf.Clamp01(1f - beyond / width);
        }
        else if (dist > maxR)
        {
            return Vector2.zero;
        }

        Vector2 dir = offset / dist;
        return dir * (strength * profile * falloffFactor);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radius);

        if (edgeFalloff > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.05f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, radius + edgeFalloff);
        }

        Vector3 origin = transform.position;
        Vector3 right = transform.right;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawLine(origin, origin + right * Mathf.Min(radius, 2f));
    }
}
