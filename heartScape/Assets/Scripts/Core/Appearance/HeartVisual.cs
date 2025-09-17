using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer))]
public class HeartVisual : MonoBehaviour
{
    [Header("Visual")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    [Tooltip("・ｽ・ｽ・ｽ・ｽ・ｽﾚ・・ｽ・ｽ・ｽ・ｽ・ｽ阡ｻ・ｽ・ｽﾌ基準・ｽ・ｽ・ｽa・ｽBHeartGrowth・ｽ・ｽ・ｽ・ｽX・ｽV・ｽ・ｽ・ｽ・ｽﾜゑｿｽ・ｽB")]
    public float     radius    = 0.6f;

    [Header("Refs (optional)")]
    [Tooltip("・ｽS・ｽ・ｽ・ｽﾈどのパ・ｽ・ｽ・ｽ・ｽ・ｽ[・ｽ^・ｽ・ｽ・ｽ・ｽ・ｽﾂプ・ｽ・ｽ・ｽt・ｽ@・ｽC・ｽ・ｽ・ｽB・ｽ・ｽ・ｽﾝ抵ｿｽﾈゑｿｽ70bpm ・ｽ・ｽ・ｽg・ｽp")]
    public HeartProfile profile;
    [Tooltip("・ｽ・ｽ・ｽ・ｽ GameObject ・ｽﾉゑｿｽ・ｽ・ｽ鼾・ｿｽﾉ参・ｽﾆゑｿｽ・ｽﾜゑｿｽ・ｽB・ｽC・ｽﾓ！")]
    public HeartAgent agent;

    [Header("Material")]
    [Tooltip("・ｽx・ｽ[・ｽX・ｽﾉゑｿｽ・ｽ・ｽ}・ｽe・ｽ・ｽ・ｽA・ｽ・ｽ・ｽB・ｽC・ｽ・ｽ・ｽX・ｽ^・ｽ・ｽ・ｽX・ｽ・ｽ・ｽ・ｽ・ｽﾄ使・ｽp・ｽ・ｽ・ｽﾜゑｿｽ")]
    public Material baseMaterial;

    Material        mat;
    Renderer        rend;
    CircleCollider2D circleCollider;

    Vector3 initialScale;
    float   initialColliderRadius;
    float   initialWorldRadius;
    bool    initialized;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        Initialize();
        ApplyRadiusScale(radius);
    }

    void Initialize()
    {
        if (initialized) return;

        if (agent == null) agent = GetComponent<HeartAgent>();
        rend           = GetComponent<Renderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        initialScale = transform.localScale;

        const float minRadius = 0.0001f;
        float colliderLocalRadius = minRadius;
        if (circleCollider != null && circleCollider.radius > minRadius)
        {
            colliderLocalRadius = circleCollider.radius;
        }
        else
        {
            colliderLocalRadius = Mathf.Max(radius, minRadius);
        }

        initialColliderRadius = colliderLocalRadius;

        float baseScale = Mathf.Max(initialScale.x, minRadius);
        float worldRadius = colliderLocalRadius * baseScale;
        if (worldRadius <= minRadius)
        {
            worldRadius = Mathf.Max(radius, 0.1f);
        }
        initialWorldRadius = worldRadius;

        var src = baseMaterial != null ? baseMaterial : (rend != null ? rend.sharedMaterial : null);
        if (src != null)
        {
            mat = new Material(src);
            if (rend != null) rend.material = mat;
        }
        else
        {
            Debug.LogWarning("[HeartVisual] Source material not found.", this);
        }

        initialized = true;
    }

    void Update()
    {
        RefreshMaterial(Time.time);
    }

    public void RefreshMaterial()
    {
        RefreshMaterial(Time.time);
    }

    public void RefreshMaterial(float timeSeconds)
    {
        ApplyMaterialState(timeSeconds);
    }

    void ApplyMaterialState(float timeSeconds)
    {
        Initialize();

        float bpm    = (profile != null) ? profile.hr : 70f;
        float bpm01  = Mathf.InverseLerp(50f, 120f, bpm);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse  = (Mathf.Sin(timeSeconds * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        if (mat == null) return;

        mat.SetFloat("_Pulse",     pulse);
        mat.SetFloat("_Radius",    radius);
        mat.SetColor("_Tint",      color);
        mat.SetFloat("_ShapeType", (float)shapeType);
    }

    void OnDestroy()
    {
        if (mat != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(mat);
#else
            Destroy(mat);
#endif
        }
    }

    /// <summary>・ｽO・ｽ・ｽ・ｽiGrowth・ｽﾈど）・ｽ・ｽ・ｽ逕ｼ・ｽa・ｽX・ｽV</summary>
    public void SetRadius(float r)
    {
        ApplyRadiusScale(Mathf.Max(0f, r));
    }

    void ApplyRadiusScale(float targetRadius)
    {
        Initialize();

        radius = targetRadius;

        const float minRadius = 0.0001f;
        float worldRadius = Mathf.Max(targetRadius, minRadius);
        float baseWorld = Mathf.Max(initialWorldRadius, minRadius);
        float scaleFactor = worldRadius / baseWorld;

        if (!float.IsFinite(scaleFactor))
        {
            scaleFactor = 1f;
        }

        transform.localScale = initialScale * scaleFactor;

        if (circleCollider != null)
        {
            circleCollider.radius = initialColliderRadius;
        }
    }

    public Material MaterialInstance => mat;
}
