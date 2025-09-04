using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DriftBody2D : MonoBehaviour
{
    [Header("Water-like Motion")]
    [Tooltip("水中の浮力っぽい常時上向きの力")]
    public float buoyancy = 0.5f;
    [Tooltip("ゆらぎ（パーリンノイズ）によるランダム力の強さ")]
    public float noiseForce = 0.6f;
    [Tooltip("ノイズの時間スピード")]
    public float noiseFrequency = 0.2f;
    [Tooltip("ノイズの空間スケール（個体差）")]
    public float noiseSpatialScale = 1.7f;

    [Header("Flow Sources")]
    [Tooltip("FlowSource の力に掛ける全体スケール")]
    public float flowForceScale = 1.0f;

    [Header("Speed Clamp")]
    [Tooltip("速度の上限（これ以上は速くならない）")]
    public float maxSpeed = 3.0f;
    [Tooltip("速度の下限（完全停止しないように）")]
    public float minSpeed = 0.15f;

    Rigidbody2D rb;
    float noiseSeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        noiseSeed = Random.Range(0f, 10000f);

        // 水中っぽい推奨初期値（必要に応じてインスペクタで調整）
        rb.gravityScale = 0.2f;   // 重力を弱める
        rb.drag = 1.5f;           // 線形ドラッグで減速
        rb.angularDrag = 5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;

        // 1) FlowSource からの力（距離減衰つき）を加算
        Vector2 flow = Vector2.zero;
        foreach (var src in FlowSource2D.All)
            flow += src.ForceAt(pos);
        if (flow.sqrMagnitude > 0f)
            rb.AddForce(flow * flowForceScale, ForceMode2D.Force);

        // 2) 浮力（常に少し上へ）
        if (buoyancy != 0f)
            rb.AddForce(Vector2.up * buoyancy, ForceMode2D.Force);

        // 3) ゆらぎ：Perlin ノイズで緩やかなランダムドリフト
        if (noiseForce > 0f)
        {
            float t = Time.time * noiseFrequency;
            float s = noiseSpatialScale;
            float nx = Mathf.PerlinNoise(noiseSeed + pos.x * s, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(noiseSeed + pos.y * s, t + 37.1f) * 2f - 1f;
            Vector2 n = new Vector2(nx, ny).normalized * noiseForce;
            rb.AddForce(n, ForceMode2D.Force);
        }

        // 4) 速度クランプ（収束/発散防止）
        Vector2 v = rb.velocity;
        float spd = v.magnitude;

        if (spd > maxSpeed)
        {
            rb.velocity = v * (maxSpeed / spd);
        }
        else if (spd < minSpeed)
        {
            if (spd < 1e-4f)
            {
                // 完全停止しているときは微小なランダムキック
                Vector2 kick = Random.insideUnitCircle.normalized * minSpeed * 0.5f;
                rb.velocity = kick;
            }
            else
            {
                rb.velocity = v * (minSpeed / Mathf.Max(spd, 1e-4f));
            }
        }
    }
}
