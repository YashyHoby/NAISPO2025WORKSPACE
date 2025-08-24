using UnityEngine;

public class FoodZone : EventZone
{
    [Tooltip("秒あたりのFOOD供給量（1.0で約1段/秒；HeartAgent.foodPerStageと掛け合わせで調整）")]
    public float feedPerSecond = 1.0f;

    [Tooltip("半径上限（セーフティ）")]
    public float rMax = 1.3f;

    public override void Apply(HeartAgent a)
    {
        a.Feed(feedPerSecond * Time.deltaTime);
        a.radius = Mathf.Min(a.radius, rMax);
    }
}
