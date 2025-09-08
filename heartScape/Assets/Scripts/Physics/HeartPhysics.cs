using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeartPhysics : MonoBehaviour
{
    public FlowManager2D flow;

    [Header("Refs (optional)")]
    [Tooltip("心拍などのパラメータを持つプロファイル。未設定なら HeartVisual から自動取得、さらに無ければ defaultBpm を使用")]
    public HeartProfile profile;
    [Tooltip("プロファイル未設定時に使う BPM")]
    public float defaultBpm = 80f;

    [Header("Flow Push (B-plan)")]
    [Tooltip("流速uを“押す力”として与える係数（大きいほど強く押す）")]
    public float alignK = 6f;

    [Header("Quadratic Drag (optional)")]
    [Tooltip("2次抗力係数（速いほど強く減速）。使わないなら0")]
    public float quadDragK = 0.35f;

    [Header("Effective Gravity")]
    [Tooltip("下向きの実効重力（小さめで“ゆっくり沈む”）")]
    public float effectiveGravity = -3f;

    [Header("Noise (optional)")]
    public float noiseForce   = 0.2f;
    public float noiseFreq    = 0.25f;
    public float noiseSpatial = 1.7f;

    [Header("Clamp")]
    public bool  clampSpeed = true;
    public float maxSpeed   = 10f;

    [Header("Min Speed (keep alive)")]
    public bool  enforceMinSpeed = true;
    public float minSpeed        = 0.5f;   // これ未満にはしない
    public bool  setVelocityHard = true;   // true: 直に底上げ / false: 力で底上げ
    public float keepAliveAccel  = 8f;     // false時の加速強さ

    [Header("Pulse Swim (jellyfish)")]
    public bool   pulseSwimEnabled = true;
    [Tooltip("拍動の加速強さ（連続力モード時は力、インパルス時は衝撃量）")]
    public float  pulseForce = 12f;          // 8〜18 目安
    [Tooltip("拍動で押し続ける時間（秒）。インパルス時は無視")]
    public float  pulseDuration = 0.12f;     // 0.08〜0.16 目安
    [Range(0f,1f)] public float pulseDirVelBias = 0.6f; // 0=流れ,1=現在速度
    public bool   pulseAsImpulse = false;
    public Vector2 pulseBpmRange = new Vector2(50f, 120f);

    Rigidbody2D rb;
    float seed;

    // Pulse 内部状態
    float pulsePhase;   // 0..1 周回
    float pulseTimer;   // 残り時間

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        seed = Random.Range(0f, 10000f);

        // 可能なら HeartVisual から profile を拝借
        if (profile == null)
        {
            var hv = GetComponent<HeartVisual>();
            if (hv != null && hv != null && hv.GetType() != null) // NRE保険
                profile = hv.GetType().GetField("profile") != null ? hv.profile : hv.GetComponent<HeartVisual>()?.profile;
            // ↑ 通常は hv.profile で十分。リフレクション保険は消してもOK
        }

        // 物理重力は0（実効重力をスクリプトで与える）
        rb.gravityScale = 0f;

        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (flow == null)
            flow = FindFirstObjectByType<FlowManager2D>(FindObjectsInactive.Exclude);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (flow == null)
            flow = FindFirstObjectByType<FlowManager2D>(FindObjectsInactive.Exclude);
    }
#endif

    void FixedUpdate()
    {
        if (flow == null) return;

        Vector2 pos = rb.position;
        Vector2 v   = rb.linearVelocity;  // 旧版Unityなら rb.velocity
        Vector2 u   = flow.SampleVelocity(pos);

        // --- 拍動推進（クラゲ） ---
        if (pulseSwimEnabled)
        {
            float bpm = (profile != null) ? profile.hr : defaultBpm;
            bpm = Mathf.Clamp(bpm, pulseBpmRange.x, pulseBpmRange.y);

            float beatHz = Mathf.Lerp(1.0f, 2.4f,
                Mathf.InverseLerp(pulseBpmRange.x, pulseBpmRange.y, bpm));

            pulsePhase += beatHz * Time.fixedDeltaTime;
            if (pulsePhase >= 1f)
            {
                pulsePhase -= 1f;
                pulseTimer   = pulseDuration;

                if (pulseAsImpulse)
                {
                    Vector2 dir = ComputePulseDir(v, u);
                    rb.AddForce(dir * pulseForce * rb.mass, ForceMode2D.Impulse);
                }
            }
            if (!pulseAsImpulse && pulseTimer > 0f)
            {
                Vector2 dir = ComputePulseDir(v, u);
                rb.AddForce(dir * pulseForce, ForceMode2D.Force);
                pulseTimer -= Time.fixedDeltaTime;
            }
        }

        // --- 流れに“押される”力（B案） ---
        Vector2 F_align = u * alignK;

        // --- 2次抗力（使わないなら quadDragK=0） ---
        Vector2 F_dragQ = (v.sqrMagnitude > 1e-8f) ? (-v * v.magnitude * quadDragK) : Vector2.zero;

        // --- 実効重力（下向き） ---
        Vector2 F_grav = new Vector2(0f, effectiveGravity) * rb.mass;

        // --- 軽いノイズ ---
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

        // --- 最高速度クランプ ---
        if (clampSpeed)
        {
            float sp = rb.linearVelocity.magnitude;
            if (sp > maxSpeed) rb.linearVelocity = rb.linearVelocity * (maxSpeed / sp);
        }

        // --- 最低速度キープ（常に少し動かす） ---
        if (enforceMinSpeed)
        {
            float m = rb.linearVelocity.magnitude;
            if (m < minSpeed)
            {
                Vector2 dir =
                    (m > 1e-6f)            ? rb.linearVelocity.normalized :
                    (u.sqrMagnitude > 0f)  ? u.normalized :
                    RandomDir();

                if (setVelocityHard)
                {
                    rb.linearVelocity = dir * minSpeed;  // 旧版Unityなら rb.velocity
                }
                else
                {
                    float need  = (minSpeed - m);
                    float accel = keepAliveAccel * need;
                    rb.AddForce(dir * accel * rb.mass, ForceMode2D.Force);
                }
            }
        }
    }

    Vector2 ComputePulseDir(Vector2 v, Vector2 u)
    {
        Vector2 a = (v.sqrMagnitude > 1e-8f) ? v.normalized : Vector2.zero;
        Vector2 b = (u.sqrMagnitude > 1e-8f) ? u.normalized : Vector2.zero;

        if (a == Vector2.zero && b == Vector2.zero) return RandomDir();
        return Vector2.Lerp(b, a, pulseDirVelBias).normalized;
    }

    Vector2 RandomDir()
    {
        float nx = Mathf.PerlinNoise(seed,         Time.time * 0.7f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed + 77.3f, Time.time * 0.7f) * 2f - 1f;
        Vector2 d = new Vector2(nx, ny);
        return (d.sqrMagnitude > 1e-6f) ? d.normalized : Vector2.right;
    }

    /// <summary>外部からプロファイルを差し替え</summary>
    public void SetProfile(HeartProfile p) => profile = p;
}
