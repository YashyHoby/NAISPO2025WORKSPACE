using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeartPhysics : MonoBehaviour
{
    [Tooltip("Flow manager reference; auto-locates from the scene when unset.")]
    public FlowManager2D flow;

    [Header("Refs (optional)")]
    [Tooltip("Optional heart profile; fetched from HeartVisual when available.")]
    public HeartProfile profile;
    [Tooltip("Fallback BPM used when no profile is assigned.")]
    public float defaultBpm = 80f;

    [Header("Flow Push (B-plan)")]
    [Tooltip("Coefficient for pushing along the sampled flow velocity.")]
    public float alignK = 6f;

    [Header("Quadratic Drag (optional)")]
    [Tooltip("Quadratic drag coefficient; set to 0 to disable.")]
    public float quadDragK = 0.35f;

    [Header("Effective Gravity")]
    [Tooltip("Pseudo gravity strength applied on the Y axis (negative pulls downward).")]
    public float effectiveGravity = -3f;

    [Header("Noise (optional)")]
    [Tooltip("Magnitude of random noise force; set to 0 to disable.")]
    public float noiseForce   = 0.2f;
    [Tooltip("Temporal frequency for updating the noise force.")]
    public float noiseFreq    = 0.25f;
    [Tooltip("Spatial scale used when sampling the noise field.")]
    public float noiseSpatial = 1.7f;

    [Header("Collision Bounce")]
    [Tooltip("Enable scripted bounce response during collisions.")]
    public bool  collisionBounceEnabled   = true;
    [Tooltip("Restitution coefficient: 0 absorbs, 1 is elastic, >1 amplifies rebound.")]
    [Range(0f, 2f)] public float collisionRestitution = 0.8f;
    [Tooltip("Minimum normal speed required to trigger bounce processing.")]
    public float collisionImpactThreshold = 0.2f;

    [Header("Collision Visual Bounce")]
    [Tooltip("Enable squash-and-stretch animation on collision.")]
    public bool  collisionVisualEnabled = true;
    [Tooltip("Impact speed considered to produce the maximum squash deformation.")]
    public float collisionImpactForMaxVisual = 4f;
    [Tooltip("Maximum squash amount applied to the visual mesh; 0 disables deformation.")]
    [Range(0f, 0.8f)] public float collisionVisualMaxSquash = 0.2f;
    [Tooltip("Duration of the squash-and-stretch animation in seconds.")]
    public float collisionVisualDuration = 0.45f;
    [Tooltip("Oscillation frequency for the squash-and-stretch animation.")]
    public float collisionVisualFrequency = 6f;
    [Tooltip("Damping factor for the squash-and-stretch animation; higher values settle faster.")]
    public float collisionVisualDamping = 4f;

    [Header("Clamp")]
    [Tooltip("Clamp the rigidbody speed to a maximum value.")]
    public bool  clampSpeed = true;
    [Tooltip("Maximum speed enforced when clamping is enabled.")]
    public float maxSpeed   = 10f;

    [Header("Min Speed (keep alive)")]
    [Tooltip("Prevent the heart from fully stopping by enforcing a minimum speed.")]
    public bool  enforceMinSpeed = true;
    [Tooltip("Minimum speed to maintain; values below are boosted.")]
    public float minSpeed        = 0.5f;
    [Tooltip("If true, snap velocity directly to the minimum when too slow; otherwise add force.")]
    public bool  setVelocityHard = true;
    [Tooltip("Acceleration factor used when restoring minimum speed via forces.")]
    public float keepAliveAccel  = 8f;

    [Header("Pulse Swim (jellyfish)")]
    [Tooltip("Enable jellyfish-like pulse swimming behavior.")]
    public bool   pulseSwimEnabled = true;
    [Tooltip("Propulsive force contributed by a single pulse.")]
    public float  pulseForce = 12f;
    [Tooltip("Duration that continuous-force mode applies pulse propulsion.")]
    public float  pulseDuration = 0.12f;
    [Tooltip("Blend between flow direction and current velocity for pulse steering (0 = flow, 1 = velocity).")]
    [Range(0f,1f)] public float pulseDirVelBias = 0.6f;
    [Tooltip("Apply the pulse as an impulse. When false, the force is distributed over the duration.")]
    public bool   pulseAsImpulse = false;
    [Tooltip("Clamp pulse BPM into this range.")]
    public Vector2 pulseBpmRange = new Vector2(50f, 120f);

    [Header("Pulse Visual")]
    [Range(0.5f, 1.2f)] public float pulseVisualMinScale = 0.85f;
    [Range(0.8f, 1.5f)] public float pulseVisualMaxScale = 1f;
    [Tooltip("Rate at which the visual squash returns to neutral.")] public float pulseVisualReturnSpeed = 4f;
    [Tooltip("Speed for interpolating the visual squash amount.")] public float pulseVisualSmoothSpeed = 8f;

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
                    if (alignment > 0.7f && Vector2.Dot(rel, velocity) > 0f)
                    {
                        return;
                    }
                }

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

    /// <summary>Set the profile supplied from outside.</summary>
    public void SetProfile(HeartProfile p) => profile = p;
}



