using UnityEngine;

public class FlowEmitter2D : MonoBehaviour
{
    public enum Mode { Radial, Directional }
    public Mode mode = Mode.Radial;

    [Tooltip("Radial時 +1=押し出し, -1=吸い込み")]
    public float sign = +1f;

    public float strength = 3f;
    public float radius = 6f;
    public AnimationCurve falloff = AnimationCurve.EaseInOut(0,1, 1,0);

    [Header("Directional")]
    public Vector2 direction = Vector2.right;

    public Vector2 DirectionWorld =>
        (direction.sqrMagnitude < 1e-6f) ? (Vector2)transform.right : direction.normalized;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f,1f,0.4f, .35f);
        Gizmos.DrawWireSphere(transform.position, radius);
        if (mode == Mode.Directional)
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)DirectionWorld * Mathf.Min(radius, 1.5f));
    }
}
