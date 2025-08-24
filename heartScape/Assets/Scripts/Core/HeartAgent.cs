using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

public class HeartAgent : MonoBehaviour
{
    public int id;
    public ShapeType shapeType;
    public float radius;              // 表示・当たり用の現在半径
    public Vector2 vel;
    public Color color;
    public HeartProfile profile;
    public Material mat;

    // --- FOOD 成長設定（インスペクタ調整） ---
    [Header("Growth (FOOD)")]
    [Tooltip("成長段階ごとのスケール。5要素（0〜4段階）。5段階目の取得で分裂します。")]
    public float[] stageScales = new float[5] { 0.45f, 0.60f, 0.75f, 0.90f, 1.05f };

    [Tooltip("基準半径（stageScalesの係数）")]
    public float baseRadius = 0.6f;

    [Tooltip("1段階成長に必要なFOOD量")]
    public float foodPerStage = 1.0f;

    [Tooltip("FOOD取得量の全体倍率")]
    public float foodGainMul = 1.0f;

    [HideInInspector] public int  growthStage = 0;   // 0〜4（5段階目取得で分裂）
    float foodAccum = 0f;

    // --- バブル捕獲状態 ---
    [HideInInspector] public bool       isCaptured = false;
    [HideInInspector] public BubbleZone capturedBy = null;

    Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (mat != null) { mat = new Material(mat); _renderer.material = mat; }
        else if (_renderer != null) { mat = _renderer.material; }

        ApplyStageScale();
    }

    void Update()
    {
        // 捕獲時は移動をBubbleZone側の制御に任せる
        if (!isCaptured)
        {
            transform.position += (Vector3)(vel * Time.deltaTime);

            // 壁バウンス（±X:8, ±Y:4.5）
            var p = transform.position;
            if (Mathf.Abs(p.x) > 8f) { vel.x *= -1; p.x = Mathf.Sign(p.x) * 8f; }
            if (Mathf.Abs(p.y) > 4.5f) { vel.y *= -1; p.y = Mathf.Sign(p.y) * 4.5f; }
            transform.position = p;
        }

        // 拍動（見た目）
        float bpm01  = Mathf.InverseLerp(50f, 120f, profile.hr);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse  = (Mathf.Sin(Time.time * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        // シェーダへ
        mat.SetFloat("_Pulse",     pulse);
        mat.SetFloat("_Radius",    radius);
        mat.SetColor("_Tint",      color);
        mat.SetFloat("_ShapeType", (float)shapeType);
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
    }

    void SplitFromGrowth()
    {
        if (radius < 0.35f) return;

        float childR = radius * 0.7f;
        radius       = childR;          // 親も縮めて2分裂感
        ApplyStageScale();              // 親のstage=0反映（半径は下で維持される）

        // 子をSpawn（Manager経由）
        var hp  = profile;
        var pos = (Vector2)transform.position;
        var dir = Random.insideUnitCircle.normalized;

        var mgr   = FindAnyObjectByType<HeartManager>();
        var child = mgr != null ? mgr.Spawn(hp, pos + dir * childR * 0.6f, vel + dir * 1.0f) : null;

        if (child != null)
        {
            child.growthStage = 0;
            // child.radius は Spawn 内で初期化されるので、こちらでスケール設定を反映
            child.baseRadius  = this.baseRadius;
            child.stageScales = (float[])this.stageScales.Clone();
            child.ApplyStageScale();
        }
    }
}
