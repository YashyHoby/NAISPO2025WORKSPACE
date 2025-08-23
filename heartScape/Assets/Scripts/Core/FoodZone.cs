using UnityEngine;
public class FoodZone : EventZone
{
    public float grow = 0.04f;
    public float rMax = 1.2f;
    public override void Apply(HeartAgent a)
    {
        a.radius = Mathf.Min(a.radius + grow * Time.deltaTime, rMax);
    }
}