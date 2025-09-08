using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer))]
public class HeartVisual : MonoBehaviour
{
    [Header("Visual")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    [Tooltip("見た目・当たりの基準半径（Growthから更新されます）")]
    public float     radius    = 0.6f;

    [Header("Refs (optional)")]
    [Tooltip("心拍などのパラメータを持つプロファイル。未設定なら 70bpm を使用")]
    public HeartProfile profile;
    [Tooltip("同じ GameObject にある場合に参照（任意）")]
    public HeartAgent agent;

    [Header("Material")]
    [Tooltip("ベースにするマテリアル（インスタンス化して使用）")]
    public Material baseMaterial;

    Material  mat;
    Renderer  rend;

    void Awake()
    {
        if (agent == null) agent = GetComponent<HeartAgent>();
        rend = GetComponent<Renderer>();

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

    void Update()
    {
        // 脈動：hr→周波数→sin パルス
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

    /// <summary>外部（Growthなど）から半径を更新</summary>
    public void SetRadius(float r) => radius = Mathf.Max(0f, r);

    public Material MaterialInstance => mat;
}
