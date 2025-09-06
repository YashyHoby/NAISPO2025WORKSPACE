using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DriftBody2D : MonoBehaviour
{
    public FlowField2D flowField;   // ← ここに参照を入れる（Scene上のFlowField2D）
    public float flowForceScale = 1f;

    [Header("Water-like Motion")]
    public float buoyancy = 0.5f;
    public float noiseForce = 0.6f;
    public float noiseFrequency = 0.2f;
    public float noiseSpatialScale = 1.7f;

    [Header("Speed Clamp")]
    public float maxSpeed = 3.0f;
    public float minSpeed = 0.15f;

    Rigidbody2D rb;
    float noiseSeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noiseSeed = Random.Range(0f, 10000f);
        rb.gravityScale = 0.2f;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;

        // 1) 力場からサンプルして加算
        if (flowField)
        {
            Vector2 f = flowField.GetForceAt(pos) * flowForceScale;
            if (f.sqrMagnitude > 0f) rb.AddForce(f, ForceMode2D.Force);
        }

        // 2) 浮力
        if (buoyancy != 0f) rb.AddForce(Vector2.up * buoyancy, ForceMode2D.Force);

        // 3) ゆらぎ
        if (noiseForce > 0f)
        {
            float t = Time.time * noiseFrequency;
            float s = noiseSpatialScale;
            float nx = Mathf.PerlinNoise(noiseSeed + pos.x * s, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(noiseSeed + pos.y * s, t + 37.1f) * 2f - 1f;
            rb.AddForce(new Vector2(nx, ny).normalized * noiseForce, ForceMode2D.Force);
        }

        // 4) 速度クランプ
        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd > maxSpeed) rb.linearVelocity = v * (maxSpeed / spd);
        else if (spd < minSpeed)
        {
            if (spd < 1e-4f) rb.linearVelocity = Random.insideUnitCircle.normalized * (minSpeed * 0.5f);
            else rb.linearVelocity = v * (minSpeed / Mathf.Max(spd, 1e-4f));
        }
    }
}
