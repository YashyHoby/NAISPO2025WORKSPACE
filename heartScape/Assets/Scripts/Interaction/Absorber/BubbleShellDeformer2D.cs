using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BubbleShellDeformer2D : MonoBehaviour
{
    struct Impulse
    {
        public Vector2 uv;
        public float amplitude;
        public float radius;
        public float life;
    }

    [Header("Impulse Parameters")]
    [Tooltip("シェルを変形させるインパルスを何件まで保持するか。")]
    public int maxImpulses = 8;

    [Tooltip("インパルスの初期へこみ量（負で内側に凹む）。")]
    public float impulseAmplitude = 0.06f;

    [Tooltip("インパルスの半径（UV 空間）。")]
    public float impulseRadius = 0.35f;

    [Tooltip("インパルスが消えるまでの寿命（秒）。")]
    public float impulseLifetime = 0.7f;

    [Header("Jiggle Settings")]
    [Tooltip("膜のぷるぷる振動の周波数です。")]
    public float jiggleFrequency = 8f;

    [Tooltip("膜のぷるぷる振動の振幅です。")]
    public float jiggleAmplitude = 0.006f;

    [Tooltip("リム幅の基準値です。")]
    public float rimWidthBase = 0.12f;

    [Tooltip("リム幅の最小値。")]
    public float rimWidthMin = 0.05f;

    [Tooltip("リム幅の最大値。")]
    public float rimWidthMax = 0.2f;

    static readonly int PropImpCount = Shader.PropertyToID("_ImpCount");
    static readonly int PropImpulses = Shader.PropertyToID("_Impulses");
    static readonly int PropRimWidth = Shader.PropertyToID("_RimWidth");
    

    readonly List<Impulse> impulses = new();
    readonly Vector4[] buffer = new Vector4[8];

    SpriteRenderer shellRenderer;
    Material runtimeMaterial;
    float jiggleSeed;

    void Awake()
    {
        shellRenderer = GetComponent<SpriteRenderer>();
        runtimeMaterial = shellRenderer != null ? shellRenderer.material : null;
        jiggleSeed = Random.value * 100f;
    }

    void OnEnable()
    {
        impulses.Clear();
        if (runtimeMaterial != null)
            runtimeMaterial.SetFloat(PropImpCount, 0f);
    }

    public void AddImpulseWorld(Vector2 worldPos)
    {
        AddImpulseUV(WorldToUV(worldPos));
    }

    public void AddImpulseUV(Vector2 uv)
    {
        if (maxImpulses <= 0)
            return;

        if (impulses.Count >= maxImpulses)
            impulses.RemoveAt(0);

        impulses.Add(new Impulse
        {
            uv = uv,
            amplitude = -Mathf.Abs(impulseAmplitude),
            radius = Mathf.Max(0.01f, impulseRadius),
            life = Mathf.Max(0.01f, impulseLifetime)
        });
    }

    public void ClearImpulses()
    {
        impulses.Clear();
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(PropImpCount, 0f);
            runtimeMaterial.SetVectorArray(PropImpulses, buffer);
        }
    }

    Vector2 WorldToUV(Vector2 world)
    {
        Vector2 local = transform.InverseTransformPoint(world);
        return local + new Vector2(0.5f, 0.5f);
    }

    void Update()
    {
        if (runtimeMaterial == null)
            return;

        float rim = Mathf.Clamp(
            rimWidthBase + Mathf.Sin((Time.time + jiggleSeed) * jiggleFrequency) * jiggleAmplitude,
            rimWidthMin, rimWidthMax);
        runtimeMaterial.SetFloat(PropRimWidth, rim);

        float dt = Time.deltaTime;
        for (int i = impulses.Count - 1; i >= 0; --i)
        {
            var imp = impulses[i];
            imp.life -= dt;
            imp.amplitude *= Mathf.Exp(-dt * 4f);
            if (imp.life <= 0f || Mathf.Abs(imp.amplitude) < 1e-4f)
                impulses.RemoveAt(i);
            else
                impulses[i] = imp;
        }

        int count = Mathf.Min(impulses.Count, buffer.Length);
        for (int i = 0; i < count; i++)
        {
            var imp = impulses[i];
            buffer[i] = new Vector4(imp.uv.x, imp.uv.y, imp.amplitude, imp.radius);
        }
        runtimeMaterial.SetFloat(PropImpCount, count);
        runtimeMaterial.SetVectorArray(PropImpulses, buffer);
    }
}
