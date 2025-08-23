using UnityEngine;
public class FlowFieldZone : EventZone
{
    public float strength = 2f;
    public override void Apply(HeartAgent a)
    {
        var p = a.transform.position - transform.position;
        Vector2 v = new Vector2(-p.y, p.x).normalized; // ŠÈˆÕ‰ñ“]—¬
        a.vel += v * strength * Time.deltaTime;
    }
}