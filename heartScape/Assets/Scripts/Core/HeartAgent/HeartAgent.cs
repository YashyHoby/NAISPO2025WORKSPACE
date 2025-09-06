using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))] // できれば CircleCollider2D を推奨
public class HeartAgent : MonoBehaviour
{
    public int id;
    public ShapeType shapeType;

    [Header("Visual / Physics Radius")]
    [Tooltip("表示＆当たり判定の基準半径（stageScales と乗算）")]
    public float baseRadius = 0.6f;

    [Tooltip("初期速度（任意）。StartでRigidbody2D.velocityに反映")]
    public Vector2 initialVelocity = Vector2.zero;

    [HideInInspector] public float radius; // 実効半径（表示＋当たり）

    public Color color = Color.white;
    public HeartProfile profile;     // hr等を持つ前提
    public Material mat;             // 表示用マテリアル（インスタンス化）

    // --- FOOD 成長設定 ---
    [Header("Growth (FOOD)")]
    [Tooltip("成長段階ごとのスケール。5要素（0〜4段階）。5段階目の取得で分裂します。")]
    public float[] stageScales = new float[5] { 0.45f, 0.60f, 0.75f, 0.90f, 1.05f };

    [Tooltip("1段階成長に必要なFOOD量")]
    public float foodPerStage = 1.0f;

    [Tooltip("FOOD取得量の全体倍率")]
    public float foodGainMul = 1.0f;

    [HideInInspector] public int  growthStage = 0; // 0〜4
    float foodAccum = 0f;

    // --- バブル捕獲状態 ---
    [HideInInspector] public bool       isCaptured = false;
    [HideInInspector] public BubbleZone capturedBy = null;

    // cached
    Renderer _renderer;
    Rigidbody2D _rb;
    CircleCollider2D _circle;    // 推奨コライダー

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _rb       = GetComponent<Rigidbody2D>();
        _circle   = GetComponent<CircleCollider2D>(); // 無ければ null（Polygon等でも動作はする）

        // マテリアルをインスタンス化
        if (mat != null) { mat = new Material(mat); _renderer.material = mat; }
        else if (_renderer != null) { mat = _renderer.material; }

        ApplyStageScale(); // radius を決定＆Colliderへ反映
    }

    void Start()
    {
        // 初期速度をRigidbodyへ
        if (_rb) _rb.linearVelocity = initialVelocity;
    }

    void Update()
    {
        // 捕獲時は移動をBubbleZone側の制御に任せる
        // ※位置更新は物理が担当するため、ここでは何もしない

        // 拍動（見た目）
        float hr      = (profile != null) ? profile.hr : 70f;
        float bpm01   = Mathf.InverseLerp(50f, 120f, hr);
        float beatHz  = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse   = (Mathf.Sin(Time.time * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        // シェーダへ
        if (mat != null)
        {
            mat.SetFloat("_Pulse",     pulse);
            mat.SetFloat("_Radius",    radius);
            mat.SetColor("_Tint",      color);
            mat.SetFloat("_ShapeType", (float)shapeType);
        }
    }

    // --- FOOD 取得 ---
    public void Feed(float amount)
    {
        foodAccum += amount * foodGainMul;
        while (foodAccum >= foodPerStage)
        {
            foodAccum -= foodPerStage;

            if (growthStage < 4)
            {
                growthStage++;
                ApplyStageScale();
            }
            else
            {
                // 5段階目の取得 → 分裂
                SplitFromGrowth();
                // 親をリセット
                growthStage = 0;
                foodAccum   = 0f;
                ApplyStageScale();
            }
        }
    }

    public void ApplyStageScale()
    {
        int idx = Mathf.Clamp(growthStage, 0, stageScales.Length - 1);
        radius  = baseRadius * stageScales[idx];

        // 当たり判定も半径に同期（CircleCollider2D推奨）
        if (_circle != null)
        {
            // Transformのスケールを考慮して物理半径を設定
            float uniform = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            _circle.radius = radius / Mathf.Max(uniform, 0.0001f);
        }
        // 他のCollider2Dを使う場合はここでサイズを合わせる処理を追加
    }

    void SplitFromGrowth()
    {
        if (radius < 0.35f) return;

        float childR = radius * 0.7f;
        radius       = childR;   // 親も縮める
        ApplyStageScale();

        var hp  = profile;
        var pos = (Vector2)transform.position;
        var dir = Random.insideUnitCircle.normalized;

        var mgr   = FindAnyObjectByType<HeartManager>();
        var child = mgr != null ? mgr.Spawn(hp, pos + dir * childR * 0.6f, Vector2.zero) : null;

        if (child != null)
        {
            child.growthStage = 0;
            child.baseRadius  = this.baseRadius;
            child.stageScales = (float[])this.stageScales.Clone();
            child.ApplyStageScale();

            // 物理速度を引き継ぎ＋発散しない程度のキック
            var childRb = child.GetComponent<Rigidbody2D>();
            if (childRb != null)
            {
                Vector2 parentV = _rb != null ? _rb.linearVelocity : Vector2.zero;
                childRb.linearVelocity = parentV + dir * 1.0f;
            }
        }
    }
}
