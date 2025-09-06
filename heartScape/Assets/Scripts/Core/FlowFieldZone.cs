using UnityEngine;

public class FlowFieldZone : EventZone
{
    public float strength = 2f;

    public override void Apply(HeartAgent a)
    {
        var rb = a.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // 簡易的な回転流（中心からのベクトルを90度回転）
        var p = (Vector2)(a.transform.position - transform.position);
        var v = new Vector2(-p.y, p.x).normalized;

        // 力として加算（Time.deltaTime は ForceMode2D.Forceで不要）
        rb.AddForce(v * strength, ForceMode2D.Force);
    }
}
