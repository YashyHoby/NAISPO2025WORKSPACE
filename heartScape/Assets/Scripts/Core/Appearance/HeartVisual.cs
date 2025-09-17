using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer))]
public class HeartVisual : MonoBehaviour
{
    [Header("Visual")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    [Tooltip("�����ځE�����蔻��̊���a�BHeartGrowth����X�V����܂��B")]
    public float     radius    = 0.6f;

    [Header("Refs (optional)")]
    [Tooltip("�S���Ȃǂ̃p�����[�^�����v���t�@�C���B���ݒ�Ȃ�70bpm ���g�p")]
    public HeartProfile profile;
    [Tooltip("���� GameObject �ɂ���ꍇ�ɎQ�Ƃ��܂��B�C�ӁI")]
    public HeartAgent agent;

    [Header("Material")]
    [Tooltip("�x�[�X�ɂ���}�e���A���B�C���X�^���X�����Ďg�p���܂�")]
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
        float bpm    = (profile != null) ? profile.hr : 70f;
        float bpm01  = Mathf.InverseLerp(50f, 120f, bpm);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse  = (Mathf.Sin(Time.time * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        if (mat != null)
        {
            mat.SetFloat("_Pulse",     pulse);
            mat.SetFloat("_Radius",    radius);
            mat.SetColor("_Tint",      color);
            mat.SetFloat("_ShapeType", (float)shapeType);
        }
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

    /// <summary>�O���iGrowth�Ȃǁj���甼�a�X�V</summary>
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