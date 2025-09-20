using UnityEngine;

/// <summary>
/// 任意のオブジェクトを一定速度で回転させる補助コンポーネント。
/// </summary>
public class SlowRotator : MonoBehaviour
{
    [Tooltip("1 秒あたりに回転させる角度（度）。正で反時計回り。")]
    public float degreesPerSecond = 10f;

    [Tooltip("回転軸（ローカル or ワールド空間）。長さ 0 の場合は Z 軸を使用。")]
    public Vector3 axis = Vector3.forward;

    [Tooltip("回転軸をローカル空間にするかワールド空間にするか。")]
    public Space space = Space.Self;

    void Update()
    {
        if (Mathf.Abs(degreesPerSecond) <= Mathf.Epsilon) return;

        Vector3 effectiveAxis = axis;
        if (effectiveAxis.sqrMagnitude <= 1e-6f)
        {
            effectiveAxis = Vector3.forward;
        }

        transform.Rotate(
            effectiveAxis.normalized,
            degreesPerSecond * Time.deltaTime,
            space
        );
    }
}
