using UnityEngine;

/// <summary>
/// 任意のオブジェクトを一定速度で回転させる補助コンポーネント。
/// </summary>
public class SlowRotator : MonoBehaviour
{
    [Tooltip("1 秒あたりに回転させる角度（度数法）。正の値で反時計回りです。")]
    public float degreesPerSecond = 10f;

    [Tooltip("回転軸（ローカルまたはワールド空間）。長さ 0 の場合は Z 軸を使用します。")]
    public Vector3 axis = Vector3.forward;

    [Tooltip("回転軸をローカル空間にするか、ワールド空間にするかを指定します。")]
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
