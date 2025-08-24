using UnityEngine;

public abstract class EventZone : MonoBehaviour
{
    public float radius = 1.0f;

    public bool Contains(Vector2 p)
        => (p - (Vector2)transform.position).sqrMagnitude < radius * radius;

    public abstract void Apply(HeartAgent a);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
