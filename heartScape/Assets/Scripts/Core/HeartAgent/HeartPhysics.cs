using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlowBody2D : MonoBehaviour
{
    public FlowManager2D flow;

    [Header("Forces")]
    [Tooltip("u を押す力の係数（大きいほど強く押す）")]
    public float alignK = 6f;
    [Tooltip("2次抗力係数（速いほど強く減速）")]
    public float quadDragK = 0.35f;
    [Tooltip("実効重力（下向き）。小さめにして“ゆっくり沈む”を表現")]
    public float effectiveGravity = -3f;

    [Header("Noise (optional)")]
    public float noiseForce = 0.2f;
    public float noiseFreq  = 0.25f;
    public float noiseSpatial = 1.7f;

    [Header("Clamp (optional)")]
    public bool  clampSpeed = true;
    public float maxSpeed   = 10f;

    [Header("Min Speed (keep alive)")]
    public bool  enforceMinSpeed = true;
    public float minSpeed        = 0.5f;   // これ未満には落とさない
    public bool  setVelocityHard = true;   // true: 直に速度を底上げ / false: AddForceでじわっと
    public float keepAliveAccel  = 8f;     // setVelocityHard=false のときの加速強さ

    Rigidbody2D rb;
    float seed;

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        seed = Random.Range(0f, 10000f);

        // “重力”はスクリプトで与えるので 2D物理の重力は0に
        rb.gravityScale = 0f;

        // Inspectorの Linear Damping を使いたいのでここでは上書きしない
        // rb.linearDamping = 0f;

        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // FlowManager の自動取得（未アサイン時）
        if (!flow) flow = FindFirstObjectByType<FlowManager2D>(FindObjectsInactive.Exclude);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!flow) flow = FindFirstObjectByType<FlowManager2D>(FindObjectsInactive.Exclude);
    }
#endif

    void FixedUpdate()
    {
        if (!flow) return;

        Vector2 pos = rb.position;
        Vector2 v   = rb.linearVelocity;          // 旧版Unityなら rb.velocity
        Vector2 u   = flow.SampleVelocity(pos);

        // 1) 流れを“押す力”（B案）
        Vector2 F_align = u * alignK;

        // 2) 2次抗力（-k * v * |v|）※使わないなら quadDragK=0 に
        Vector2 F_dragQ = (v.sqrMagnitude > 1e-8f) ? (-v * v.magnitude * quadDragK) : Vector2.zero;

        // 3) 実効重力（下向き）
        Vector2 F_grav = new Vector2(0f, effectiveGravity) * rb.mass;

        // 4) 軽いノイズで水中っぽさ
        Vector2 F_noise = Vector2.zero;
        if (noiseForce > 0f)
        {
            float t  = Time.time * noiseFreq;
            float nx = Mathf.PerlinNoise(seed + pos.x * noiseSpatial, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(seed + pos.y * noiseSpatial, t + 37.1f) * 2f - 1f;
            Vector2 n = new Vector2(nx, ny).normalized;
            F_noise = n * noiseForce;
        }

        rb.AddForce(F_align + F_dragQ + F_grav + F_noise, ForceMode2D.Force);

        // 最高速度クランプ
        if (clampSpeed)
        {
            float m = rb.linearVelocity.magnitude;
            if (m > maxSpeed) rb.linearVelocity = rb.linearVelocity * (maxSpeed / m);
        }

        // 最低速度キープ（常に少しは動かす）
        if (enforceMinSpeed)
        {
            float m = rb.linearVelocity.magnitude;
            if (m < minSpeed)
            {
                // 進行方向の決定：今の速度 → 流れu → ランダムの順で採用
                Vector2 dir =
                    (m > 1e-6f)           ? rb.linearVelocity.normalized :
                    (u.sqrMagnitude > 0f) ? u.normalized :
                    new Vector2(
                        Mathf.PerlinNoise(seed,           Time.time * 0.7f) * 2f - 1f,
                        Mathf.PerlinNoise(seed + 37.1f,   Time.time * 0.7f) * 2f - 1f
                    ).normalized;

                if (setVelocityHard)
                {
                    // ① 速度を直接“底上げ”
                    rb.linearVelocity = dir * minSpeed;      // 旧版Unityなら rb.velocity
                }
                else
                {
                    // ② 力でじわっと底上げ
                    float need  = (minSpeed - m);
                    float accel = keepAliveAccel * need;
                    rb.AddForce(dir * accel * rb.mass, ForceMode2D.Force);
                }
            }
        }
    }
}
