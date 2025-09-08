using UnityEngine;

public class FoodZone : EventZone
{
    public float foodAmount = 0.25f;

    public override void Apply(HeartAgent a)
    {
        // 成長コンポーネントへ
        var growth = a.GetComponent<HeartGrowth>();
        if (growth) growth.Feed(foodAmount);

        // 半径を使う処理がある場合は Visual から取得
        var visual = a.GetComponent<HeartVisual>();
        if (visual)
        {
            float r = visual.radius;
            // 例）Gizmoやエフェクト計算など、必要なら r を使用
        }
    }
}
