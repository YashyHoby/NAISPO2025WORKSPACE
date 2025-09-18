using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeartPhysics : MonoBehaviour
{
    [Tooltip("流体シミュレーションを提供する FlowManager2D への参照です。未設定の場合はシーンから自動取得します。")]
    public FlowManager2D flow;

    [Header("Refs (optional)")]
    [Tooltip("心拍や見た目のパラメータを持つ HeartProfile です。未設定の場合は HeartVisual から取得し、それもなければ defaultBpm を使います。")]
    public HeartProfile profile;
    [Tooltip("プロファイルがない場合に使う基準 BPM です。")]
    public float defaultBpm = 80f;

    [Header("Flow Push (B-plan)")]
    [Tooltip("流れの速度ベクトルを押し出す力として加える係数です。大きいほど強く流れに沿って押し出します。")]
    public float alignK = 6f;

    [Header("Quadratic Drag (optional)")]
    [Tooltip("速度の二乗に比例する抵抗の係数です。0 にすると無効になります。")]
    public float quadDragK = 0.35f;

    [Header("Effective Gravity")]
    [Tooltip("Y 軸下方向に働く疑似重力の強さです。小さいほどゆっくり沈みます。")]
    public float effectiveGravity = -3f;

    [Header("Noise (optional)")]
    [Tooltip("ランダムな揺らぎの力の大きさです。0 にするとノイズを無効にします。")]
    public float noiseForce   = 0.2f;
    [Tooltip("ノイズ力を更新する時間の速さです。大きいほど変化が速くなります。")]
    public float noiseFreq    = 0.25f;
    [Tooltip("ノイズの空間スケールです。大きいほど広い範囲でゆるやかに変化します。")]
    public float noiseSpatial = 1.7f;

    [Header("Collision Bounce")]
    [Tooltip("衝突時にスクリプトで反発を計算するかどうかです。")]
    public bool  collisionBounceEnabled   = true;
    [Tooltip("反発係数です。0 で吸収し、1 で弾性、2 以上で強い跳ね返りになります。")]
    [Range(0f, 2f)] public float collisionRestitution = 0.8f;
    [Tooltip("反発を適用するために必要な最小の法線速度です。")]
    public float collisionImpactThreshold = 0.2f;

    [Header("Collision Visual Bounce")]
    [Tooltip("衝突時に見た目だけを潰して伸ばすアニメーションを再生するかどうかです。")]
    public bool  collisionVisualEnabled = true;
    [Tooltip("最大の潰れ量に達するとみなす衝突速度です。")]
    public float collisionImpactForMaxVisual = 4f;
    [Tooltip("見た目に適用する最大の潰れ量です。0 にすると変形しません。")]
    [Range(0f, 0.8f)] public float collisionVisualMaxSquash = 0.2f;
    [Tooltip("見た目の弾むアニメーションが収束するまでの時間です（秒）。")]
    public float collisionVisualDuration = 0.45f;
    [Tooltip("潰れ伸びアニメーションの振動数です（Hz）。")]
    public float collisionVisualFrequency = 6f;
    [Tooltip("潰れ伸びアニメーションの減衰係数です。大きいほど早く収束します。")]
    public float collisionVisualDamping = 4f;

    [Header("Clamp")]
    [Tooltip("速度の上限を適用するかどうかです。")]
    public bool  clampSpeed = true;
    [Tooltip("上限として使用する最大速度です。clampSpeed が有効なときに適用されます。")]
    public float maxSpeed   = 10f;

    [Header("Min Speed (keep alive)")]
    [Tooltip("最低速度を維持して動きを止めないようにするかどうかです。")]
    public bool  enforceMinSpeed = true;
    [Tooltip("維持したい最低速度です。これ未満になると補正します。")]
    public float minSpeed        = 0.5f;
    [Tooltip("最低速度を下回ったときに速度を直接書き換えるかどうかです。false の場合は力で加速します。")]
    public bool  setVelocityHard = true;
    [Tooltip("力で最低速度を補うときに使う加速度係数です。setVelocityHard が false のときに使用します。")]
    public float keepAliveAccel  = 8f;

    [Header("Pulse Swim (jellyfish)")]
    [Tooltip("拍動による推進を有効にするかどうかです。")]
    public bool   pulseSwimEnabled = true;
    [Tooltip("拍動 1 回あたりの推進力です。インパルスモードでは瞬間的な衝撃量になります。")]
    public float  pulseForce = 12f;
    [Tooltip("連続力モードで力を加え続ける時間です（秒）。インパルスモードでは使用しません。")]
    public float  pulseDuration = 0.12f;
    [Tooltip("拍動の進行方向を流れベクトルと現在速度のどちらに寄せるかを決める係数です。0 で流れ、1 で現在速度です。")]
    [Range(0f,1f)] public float pulseDirVelBias = 0.6f;
    [Tooltip("拍動をインパルスとして適用するかどうかです。true で瞬間的に加速します。")]
    public bool   pulseAsImpulse = false;
    [Tooltip("拍動の BPM をこの範囲に収めます。")]
    public Vector2 pulseBpmRange = new Vector2(50f, 120f);

    [Header("Pulse Visual")]
    [Range(0.5f, 1.2f)] public float pulseVisualMinScale = 0.85f;
    [Range(0.8f, 1.5f)] public float pulseVisualMaxScale = 1f;
    [Tooltip("見た目の収縮バランスが元に戻る速さです。")] public float pulseVisualReturnSpeed = 4f;
    [Tooltip("収縮スケールの補間速度です。大きいほど素早く追従します。")] public float pulseVisualSmoothSpeed = 8f;

    Rigidbody2D rb;
    float seed;

    MeshFilter meshFilter;
    Mesh        visualMesh;
    Vector3[]   baseVertices;
    Vector3[]   workingVertices;
    Coroutine   collisionVisualRoutine;
    Vector2     lastBounceAxis = Vector2.right;

    HeartVisual visual;
    float       pulseVisualValue;
    float       pulseVisualScale = 1f;
    float       pulseVisualTargetScale = 1f;
    float       currentSquashAmount = 0f;

    // Pulse 内部状態
    float pulsePhase;   // 0..1 周回
    float pulseTimer;   // 残り時間

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        seed = Random.Range(0f, 10000f);

        SetupVisualMesh();

        visual = GetComponent<HeartVisual>();
        if (profile == null && visual != null)
        {
            profile = visual.profile;
        }

        // 物理重力は0（実効重力をスクリプトで与える）
        rb.gravityScale = 0f;

        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (flow == null)
            flow = FindFirstObjectByType<FlowManager2D>(FindObjectsInactive.Exclude);
    }

    void OnEnable()
    {
        if (visualMesh == null)
            SetupVisualMesh();
        ResetVisualDeform();
    }

    void OnDisable()
    {
        if (collisionVisualRoutine != null)
        {
            StopCoroutine(collisionVisualRoutine);
            collisionVisualRoutine = null;
        }
        ResetVisualDeform();
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

        // --- 拍動推進
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

                pulseVisualValue = 1f;
            }

            if (!pulseAsImpulse && pulseTimer > 0f)
            {
                Vector2 dir = ComputePulseDir(v, u);
                rb.AddForce(dir * pulseForce, ForceMode2D.Force);
                pulseTimer -= Time.fixedDeltaTime;

                float strength = Mathf.Sin(Mathf.Clamp01(pulsePhase) * Mathf.PI);
                pulseVisualValue = Mathf.Max(pulseVisualValue, strength);
            }
        }
        else
        {
            pulseTimer = 0f;
            pulsePhase = 0f;
        }

        pulseVisualValue = Mathf.Clamp01(pulseVisualValue);
        float visualReturn = Mathf.Max(0f, pulseVisualReturnSpeed);
        if (visualReturn > 0f)
        {
            pulseVisualValue = Mathf.MoveTowards(pulseVisualValue, 0f, visualReturn * Time.fixedDeltaTime);
        }
        else
        {
            pulseVisualValue = 0f;
        }

        float minScale = Mathf.Clamp(pulseVisualMinScale, 0.1f, pulseVisualMaxScale);
        float maxScale = Mathf.Max(minScale, pulseVisualMaxScale);
        float smooth = Mathf.Max(0f, pulseVisualSmoothSpeed);

        pulseVisualTargetScale = Mathf.Lerp(maxScale, minScale, pulseVisualValue);

        if (smooth > 0f)
        {
            pulseVisualScale = Mathf.MoveTowards(pulseVisualScale, pulseVisualTargetScale, smooth * Time.fixedDeltaTime);
        }
        else
        {
            pulseVisualScale = pulseVisualTargetScale;
        }

        ApplyVisualSquash(lastBounceAxis, currentSquashAmount);

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

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollisionBounce(collision);
    }

    void HandleCollisionBounce(Collision2D collision)
    {
        if ((collisionBounceEnabled || collisionVisualEnabled) == false || rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        Vector2 bestNormal = Vector2.zero;
        float strongestDot = 0f;

        int contactCount = collision.contactCount;
        for (int i = 0; i < contactCount; i++)
        {
            var contact = collision.GetContact(i);
            float dot = Vector2.Dot(velocity, contact.normal);
            if (dot < strongestDot)
            {
                strongestDot = dot;
                bestNormal = contact.normal;
            }
        }

        if (bestNormal == Vector2.zero)
        {
            Vector2 rel = collision.relativeVelocity;
            if (rel.sqrMagnitude > 1e-6f)
            {
                bestNormal = -rel.normalized;
                strongestDot = Vector2.Dot(velocity, bestNormal);
            }
        }

        if (bestNormal == Vector2.zero || strongestDot >= 0f) return;

        float impactSpeed = -strongestDot;

        if (collisionBounceEnabled && impactSpeed >= collisionImpactThreshold)
        {
            Vector2 newVel = velocity - (1f + collisionRestitution) * strongestDot * bestNormal;
            rb.linearVelocity = newVel;
            velocity = newVel;
        }

        if (collisionVisualEnabled && impactSpeed > 0f)
        {
            Vector2 incomingDir = (velocity.sqrMagnitude > 1e-6f) ? velocity.normalized : -bestNormal;
            TriggerVisualBounce(incomingDir, impactSpeed);
        }
    }

    void SetupVisualMesh()
    {
        if (EnsureVisualMeshData(true))
        {
            lastBounceAxis = Vector2.right;
            currentSquashAmount = 0f;
            pulseVisualValue = 0f;
            pulseVisualTargetScale = 1f;
            pulseVisualScale = 1f;
            ApplyVisualSquash(lastBounceAxis, currentSquashAmount);
        }
    }

    bool EnsureVisualMeshData(bool forceRefresh = false)
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            visualMesh = null;
            baseVertices = null;
            workingVertices = null;
            return false;
        }

        Mesh currentMesh = meshFilter.sharedMesh;
        if (currentMesh == null)
            currentMesh = meshFilter.mesh;

        if (currentMesh == null)
        {
            visualMesh = null;
            baseVertices = null;
            workingVertices = null;
            return false;
        }

        bool meshChanged = visualMesh != currentMesh;
        if (meshChanged)
        {
            visualMesh = currentMesh;
            visualMesh.MarkDynamic();
        }

        int vertexCount = visualMesh.vertexCount;
        if (vertexCount <= 0)
        {
            baseVertices = null;
            workingVertices = null;
            return false;
        }

        if (forceRefresh || meshChanged || baseVertices == null || baseVertices.Length != vertexCount)
        {
            var verts = visualMesh.vertices;
            if (verts == null || verts.Length != vertexCount)
            {
                return false;
            }

            baseVertices = new Vector3[vertexCount];
            workingVertices = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                baseVertices[i] = verts[i];
                workingVertices[i] = verts[i];
            }
        }

        return true;
    }

    void ResetVisualDeform()
    {
        if (!EnsureVisualMeshData()) return;

        visualMesh.vertices = baseVertices;
        visualMesh.RecalculateBounds();

        if (workingVertices != null)
        {
            for (int i = 0; i < workingVertices.Length; i++)
                workingVertices[i] = baseVertices[i];
        }

        lastBounceAxis = Vector2.right;
        currentSquashAmount = 0f;
        pulseVisualValue = 0f;
        pulseVisualTargetScale = 1f;
        pulseVisualScale = 1f;
    }

    void TriggerVisualBounce(Vector2 axis, float impactSpeed)
    {
        if (!collisionVisualEnabled || collisionVisualMaxSquash <= 0f) return;
        if (!EnsureVisualMeshData()) return;
        if (impactSpeed <= 1e-4f) return;

        float normalised = (collisionImpactForMaxVisual > 0f) ? Mathf.Clamp01(impactSpeed / collisionImpactForMaxVisual) : 1f;
        float amplitude = collisionVisualMaxSquash * normalised;
        if (amplitude <= 1e-4f) return;

        Vector2 axisNorm = axis.sqrMagnitude > 1e-6f ? axis.normalized : lastBounceAxis;
        if (axisNorm == Vector2.zero) axisNorm = Vector2.right;

        if (collisionVisualRoutine != null)
        {
            StopCoroutine(collisionVisualRoutine);
        }
        collisionVisualRoutine = StartCoroutine(VisualBounceRoutine(axisNorm, amplitude));
    }

    IEnumerator VisualBounceRoutine(Vector2 axis, float amplitude)
    {
        float timer = 0f;
        float duration = Mathf.Max(0.001f, collisionVisualDuration);
        float frequency = Mathf.Max(0.01f, collisionVisualFrequency);

        while (timer < duration)
        {
            float t = timer / duration;
            float damping = Mathf.Exp(-collisionVisualDamping * t);
            float oscillation = Mathf.Cos(frequency * timer * Mathf.PI * 2f);
            ApplyVisualSquash(axis, amplitude * damping * oscillation);
            timer += Time.deltaTime;
            yield return null;
        }

        ApplyVisualSquash(axis, 0f);
        collisionVisualRoutine = null;
    }

    void ApplyVisualSquash(Vector2 axis, float amount)
    {
        if (!EnsureVisualMeshData()) return;

        Vector2 norm = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector2.right;
        Vector2 perp = new Vector2(-norm.y, norm.x);

        float clamped = Mathf.Clamp(amount, -collisionVisualMaxSquash, collisionVisualMaxSquash);
        float alongScale = Mathf.Clamp(1f - clamped, 0.25f, 2.5f);
        float perpScale  = Mathf.Clamp(1f + clamped, 0.25f, 2.5f);
        currentSquashAmount = clamped;

        float pulseScale = Mathf.Max(0.0001f, pulseVisualScale);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 baseV = baseVertices[i];
            Vector2 plane = new Vector2(baseV.x, baseV.y);
            float along = Vector2.Dot(plane, norm);
            float side = Vector2.Dot(plane, perp);
            Vector2 scaled = norm * (along * alongScale) + perp * (side * perpScale);
            scaled *= pulseScale;
            workingVertices[i] = new Vector3(scaled.x, scaled.y, baseV.z);
        }

        visualMesh.vertices = workingVertices;
        visualMesh.RecalculateBounds();
        lastBounceAxis = norm;
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


