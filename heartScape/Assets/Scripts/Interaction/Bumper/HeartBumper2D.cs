using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartBumper2D : MonoBehaviour
{
    [Header("バウンド設定")]
    [Tooltip("衝突したハートに与える速度（m/s）。")]
    public float bounceSpeed = 12f;

    [Tooltip("既存の速度を上書きするかどうか。オフの場合は速度へ加算します。")]
    public bool overrideVelocity = true;

    [Tooltip("押し出し方向に追加する上向きバイアス（0 で無効）。")]
    public float upwardBias = 0f;

    [Header("ビジュアル演出")]
    [Tooltip("拡大演出を適用する対象。未設定の場合は自分自身を使用します。")]
    public Transform visualTarget;

    [Tooltip("衝突時に一時的に拡大する倍率です。")]
    public float visualScaleMultiplier = 1.25f;

    [Tooltip("拡大から元に戻るまでの時間（秒）。")]
    public float visualDuration = 0.25f;

    Collider2D bumperCollider;
    Vector3 baseScale = Vector3.one;
    Coroutine visualRoutine;

    void Awake()
    {
        bumperCollider = GetComponent<Collider2D>();
        bumperCollider.isTrigger = false;

        if (visualTarget == null)
            visualTarget = transform;

        if (visualTarget != null)
            baseScale = visualTarget.localScale;
    }

    void OnEnable()
    {
        if (visualTarget != null)
            baseScale = visualTarget.localScale;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider) return;

        var heart = collision.collider.GetComponentInParent<HeartPhysics>();
        if (heart == null) return;

        var rb = heart.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 bumperPos = transform.position;
        Vector2 heartPos = rb.position;
        Vector2 dir = heartPos - bumperPos;

        if (dir.sqrMagnitude <= 1e-6f)
            dir = Random.insideUnitCircle;

        if (Mathf.Abs(upwardBias) > 1e-6f)
            dir += Vector2.up * upwardBias;

        dir = dir.normalized;

        Vector2 newVelocity = dir * bounceSpeed;

        if (overrideVelocity)
        {
            rb.linearVelocity = newVelocity;
        }
        else
        {
            rb.linearVelocity += newVelocity;
        }

        TriggerVisualPop();
    }

    void TriggerVisualPop()
    {
        if (visualTarget == null || visualScaleMultiplier <= 1f || visualDuration <= 0f)
            return;

        if (visualRoutine != null)
            StopCoroutine(visualRoutine);

        visualRoutine = StartCoroutine(VisualPopRoutine());
    }

    IEnumerator VisualPopRoutine()
    {
        float timer = 0f;
        while (timer < visualDuration)
        {
            float t = timer / visualDuration;
            float curve = Mathf.Sin(t * Mathf.PI); // 0→1→0 の滑らかな曲線
            float scale = Mathf.Lerp(1f, visualScaleMultiplier, curve);
            visualTarget.localScale = baseScale * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        visualTarget.localScale = baseScale;
        visualRoutine = null;
    }
}
