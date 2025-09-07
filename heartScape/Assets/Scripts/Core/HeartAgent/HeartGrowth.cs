using UnityEngine;

public class HeartGrowth : MonoBehaviour
{
    [Header("Growth (FOOD)")]
    [Tooltip("成長段階ごとのスケール。5要素（0〜4段階）。5段階目の取得で分裂します。")]
    public float[] stageScales = new float[5] { 0.45f, 0.60f, 0.75f, 0.90f, 1.05f };

    [Tooltip("基準半径（stageScalesの係数）")]
    public float baseRadius = 0.6f;

    [Tooltip("1段階成長に必要なFOOD量")]
    public float foodPerStage = 1.0f;

    [Tooltip("FOOD取得量の全体倍率")]
    public float foodGainMul = 1.0f;

    [HideInInspector] public int growthStage = 0;   // 0〜4（5段階目取得で分裂）
    float foodAccum = 0f;

    HeartVisual visual;
    HeartAgent  agent;

    void Awake()
    {
        visual = GetComponent<HeartVisual>();
        agent  = GetComponent<HeartAgent>();
        ApplyStageScale();
    }

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
        float r = baseRadius * stageScales[idx];
        if (visual) visual.SetRadius(r);
    }

    void SplitFromGrowth()
    {
        if (!visual) return;
        float r = visual.radius;
        if (r < 0.35f) return;

        // 親子で体積保存っぽく
        float childR = r * 0.7f;
        visual.SetRadius(childR);         // 親も縮めて“分裂感”
        ApplyStageScale();                // stage=0反映（半径は上書き済み）

        // 子をSpawn（HeartManagerに依存）
        var hp  = agent ? agent.profile : null;
        var pos = (Vector2)transform.position;
        var dir = Random.insideUnitCircle.normalized;

        var mgr   = FindAnyObjectByType<HeartManager>();
        var child = mgr ? mgr.Spawn(hp, pos + dir * childR * 0.6f, dir * 1.0f) : null;

        if (child != null)
        {
            // 子側のGrowth/Visualへ設定反映
            var cg = child.GetComponent<HeartGrowth>();
            var cv = child.GetComponent<HeartVisual>();
            if (cg)
            {
                cg.baseRadius  = this.baseRadius;
                cg.stageScales = (float[])this.stageScales.Clone();
                cg.growthStage = 0;
                cg.ApplyStageScale();
            }
            if (cv) cv.SetRadius(childR);
        }
    }
}
