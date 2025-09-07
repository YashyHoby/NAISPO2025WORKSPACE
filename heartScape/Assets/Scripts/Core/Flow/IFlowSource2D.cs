using UnityEngine;

public interface IFlowSource2D
{
    /// <summary>ワールド座標 position での流速 u(x) を返す</summary>
    Vector2 SampleVelocity(Vector2 position);
}
