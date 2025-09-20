using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeartPhysics : MonoBehaviour
{
    [Tooltip("FlowManager2D への参照。未設定ならシーン上から自動取得します。")]
    public FlowManager2D flow;

    [Header("参照（任意）")]
    [Tooltip("任意の HeartProfile。HeartVisual に設定されていればそこから取得します。")]
    public HeartProfile profile;
    [Tooltip("プロファイルが無い場合に使う基準 BPM 値です。")]
    public float defaultBpm = 80f;

    [Header("流れへの沿い力（B案）")]
    [Tooltip("流れの速度ベクトルに沿って押し出す力の係数です。")]
    public float alignK = 6f;

    [Header("二次抗力（任意）")]
    [Tooltip("速度の二乗に比例する抗力係数です。0 にすると無効化します。")]
    public float quadDragK = 0.35f;

    [Header("実効重力")]
    [Tooltip("Y 軸方向に働く擬似重力の強さです。負にすると下向きに作用します。")]
    public float effectiveGravity = -3f;

    [Header("ノイズ（任意）")]
    [Tooltip("ランダムノイズの力の大きさです。0 にすると無効化します。")]
    public float noiseForce   = 0.2f;
    [Tooltip("ノイズ力を更新する時間方向の速さです。値が大きいほど変化が速くなります。")]
    public float noiseFreq    = 0.25f;
    [Tooltip("ノイズをサンプリングする際の空間スケールです。大きいほど滑らかに変化します。")]
    public float noiseSpatial = 1.7f;

    [Header("衝突反発")]
    [Tooltip("衝突時にスクリプトの反発処理を有効にします。")]
    public bool  collisionBounceEnabled   = true;
    [Tooltip("反発係数です。0 で吸収、1 で弾性、1 を超えると強く跳ね返ります。")]
    [Range(0f, 2f)] public float collisionRestitution = 0.8f;
    [Tooltip("反発処理を適用するための最小法線速度です。")]
    public float collisionImpactThreshold = 0.2f;

    [Header("衝突ビジュアル反発")]
    [Tooltip("衝突時にスクワッシュ＆ストレッチ演出を再生します。")]
    public bool  collisionVisualEnabled = true;
    [Tooltip("最大の潰れ量になるとみなす衝突速度です。")]
    public float collisionImpactForMaxVisual = 4f;
    [Tooltip("ビジュアルに適用される最大潰れ量です。0 で変形を行いません。")]
    [Range(0f, 0.8f)] public float collisionVisualMaxSquash = 0.2f;
    [Tooltip("スクワッシュ＆ストレッチ演出が収束するまでの時間（秒）です。")]
    public float collisionVisualDuration = 0.45f;
    [Tooltip("スクワッシュ＆ストレッチ演出の振動数です。")]
    public float collisionVisualFrequency = 6f;
    [Tooltip("スクワッシュ＆ストレッチ演出の減衰係数です。大きいほど早く収束します。")]
    public float collisionVisualDamping = 4f;

    [Header("速度上限")]
    [Tooltip("リジッドボディの速度を最大値で制限します。")]
    public bool  clampSpeed = true;
    [Tooltip("速度制限が有効なときの最大速度です。")]
    public float maxSpeed   = 10f;

    [Header("最低速度（停止防止）")]
    [Tooltip("最低速度を維持して完全停止を防ぎます。")]
    public bool  enforceMinSpeed = true;
    [Tooltip("維持したい最低速度です。この値を下回ると補正されます。")]
    public float minSpeed        = 0.5f;
    [Tooltip("オンの場合は速度が遅すぎると即座に最低速度まで引き上げます。オフの場合は力を加えて補います。")]
    public bool  setVelocityHard = true;
    [Tooltip("力で最低速度まで戻す際に使う加速度係数です。")]
    public float keepAliveAccel  = 8f;

    [Header("パルス推進（クラゲ風）")]
    [Tooltip("クラゲのようなパルス推進を有効化します。")]
    public bool   pulseSwimEnabled = true;
    [Tooltip("パルス 1 回で与える推進力です。")]
    public float  pulseForce = 12f;
    [Tooltip("連続力モードでパルス推進を適用し続ける時間（秒）です。")]
    public float  pulseDuration = 0.12f;
    [Tooltip("パルスの進行方向を流れ方向と現在速度のどちらへ寄せるかの係数です（0=流れ、1=現在速度）。")]
    [Range(0f,1f)] public float pulseDirVelBias = 0.6f;
    [Tooltip("オンでインパルスとして瞬間的に適用します。オフの場合は duration の間に分散させます。")]
    public bool   pulseAsImpulse = false;
    [Tooltip("パルスの BPM をこの範囲へ制限します。")]
    public Vector2 pulseBpmRange = new Vector2(50f, 120f);

    [Header("パルス演出")]
    [Range(0.5f, 1.2f)] public float pulseVisualMinScale = 0.85f;
    [Range(0.8f, 1.5f)] public float pulseVisualMaxScale = 1f;
    [Tooltip("見た目の潰れが元の形へ戻る速さです。")] public float pulseVisualReturnSpeed = 4f;
    [Tooltip("見た目の潰れ量を補間するスピードです。")] public float pulseVisualSmoothSpeed = 8f;

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

    // Pulse state
    float pulsePhase;   // 0..1 cycle
    float pulseTimer;   // Remaining duration

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

        // Disable built-in gravity; apply effective gravity via script.
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
        Vector2 v   = rb.linearVelocity;  // Older Unity versions used rb.velocity
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

        // --- Flow push (B-plan) ---
        Vector2 F_align = u * alignK;

        // --- Quadratic drag (set quadDragK=0 to disable) ---
        Vector2 F_dragQ = (v.sqrMagnitude > 1e-8f) ? (-v * v.magnitude * quadDragK) : Vector2.zero;

        // --- Effective gravity (downward) ---
        Vector2 F_grav = new Vector2(0f, effectiveGravity) * rb.mass;

        // --- Light noise ---
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

        // --- Clamp maximum speed ---
        if (clampSpeed)
        {
            float sp = rb.linearVelocity.magnitude;
            if (sp > maxSpeed) rb.linearVelocity = rb.linearVelocity * (maxSpeed / sp);
        }

        // --- Maintain minimum speed (keep moving) ---
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
                    rb.linearVelocity = dir * minSpeed;  // Older Unity versions used rb.velocity
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
        bool skipBounce = false;

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
                if (velocity.sqrMagnitude > 1e-6f)
                {
                    float alignment = Vector2.Dot(rel.normalized, velocity.normalized);
                    if (alignment > 0.9f && Vector2.Dot(rel, velocity) > 0f)
                    {
                        skipBounce = true;
                    }
                }

                bestNormal = -rel.normalized;
                strongestDot = Vector2.Dot(velocity, bestNormal);
            }
        }

        if (bestNormal == Vector2.zero || strongestDot >= 0f) return;

        float impactSpeed = -strongestDot;

        if (!skipBounce && collisionBounceEnabled && impactSpeed >= collisionImpactThreshold)
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


    /// <summary>外部からの衝突による演出を発生させたいときに呼び出します。direction は移動方向を指定します。
    /// impactSpeed は擬似的な衝突速度を表す値です。
    public void NotifyExternalBounce(Vector2 direction, float impactSpeed)
    {
        if (impactSpeed <= 0f) return;
        Vector2 dir = direction;
        if (dir.sqrMagnitude <= 1e-6f)
        {
            dir = (rb != null && rb.linearVelocity.sqrMagnitude > 1e-6f) ? rb.linearVelocity.normalized : RandomDir();
        }
        if (collisionVisualEnabled)
        {
            TriggerVisualBounce(dir.normalized, impactSpeed);
        }
    }

    Vector2 RandomDir()
    {
        float nx = Mathf.PerlinNoise(seed,         Time.time * 0.7f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed + 77.3f, Time.time * 0.7f) * 2f - 1f;
        Vector2 d = new Vector2(nx, ny);
        return (d.sqrMagnitude > 1e-6f) ? d.normalized : Vector2.right;
    }

    /// <summary>Set the profile supplied from outside.</summary>
    public void SetProfile(HeartProfile p) => profile = p;
}



