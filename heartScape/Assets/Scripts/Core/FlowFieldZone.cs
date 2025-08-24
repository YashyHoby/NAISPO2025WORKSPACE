using UnityEngine;

public class FlowFieldZone : EventZone
{
    public float strength = 2f;

    public override void Apply(HeartAgent a)
    {
        var p  = a.transform.position - transform.position;
        var v  = new Vector2(-p.y, p.x).normalized;  // 簡易回転流
        a.vel += v * strength * Time.deltaTime;
    }
}
