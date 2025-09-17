using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer))]
public class HeartVisual : MonoBehaviour
{
    [Header("Visual")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    [Tooltip("見た目・当たり判定の基準半径。HeartGrowthから更新されます。")]
    public float     radius    = 0.6f;

    [Header("Refs (optional)")]
    [Tooltip("心拍などのパラメータを持つプロファイル。未設定なら70bpm を使用")]
    public HeartProfile profile;
    [Tooltip("同じ GameObject にある場合に参照します。任意！")]
    public HeartAgent agent;

    [Header("Material")]
    [Tooltip("ベースにするマテリアル。インスタンス化して使用します")]
    public Material baseMaterial;

    Material        mat;
    Renderer        rend;
    CircleCollider2D circleCollider;
    Vector3          baseScale;
    float            referenceRadiusForScale = 1f;
    bool             scaleBaselineInitialized;

    void OnEnable()
    {
        ApplyRadiusToScale();
    }

    void Awake()
    {
        if (agent == null) agent = GetComponent<HeartAgent>();
        rend           = GetComponent<Renderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        CacheScaleBaseline();
        ApplyRadiusToScale();

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
    }

    void CacheScaleBaseline()
    {
        if (scaleBaselineInitialized) return;

        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();

        baseScale = transform.localScale;

        const float minRadius = 0.0001f;
        if (circleCollider != null && circleCollider.radius > minRadius)
        {
            referenceRadiusForScale = circleCollider.radius;
        }
        else
        {
            referenceRadiusForScale = Mathf.Max(radius, minRadius);
        }

        scaleBaselineInitialized = true;
    }

    void ApplyRadiusToScale()
    {
        CacheScaleBaseline();

        float baseRadius   = Mathf.Max(referenceRadiusForScale, 0.0001f);
        float targetRadius = Mathf.Max(0f, radius);
        float scaleFactor  = targetRadius / baseRadius;

        transform.localScale = baseScale * scaleFactor;

        if (circleCollider != null)
        {
            circleCollider.radius = baseRadius;
        }
    }

    void Update()
    {
        // 脈動→hr→周波数→sin パルス
        float bpm    = (profile != null) ? profile.hr : 70f;
        float bpm01  = Mathf.InverseLerp(50f, 120f, bpm);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse  = (Mathf.Sin(Time.time * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        // シェーダへ
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

    /// <summary>外部（Growthなど）から半径更新</summary>
    public void SetRadius(float r)
    {
        radius = Mathf.Max(0f, r);
        ApplyRadiusToScale();
    }

    public Material MaterialInstance => mat;
}
