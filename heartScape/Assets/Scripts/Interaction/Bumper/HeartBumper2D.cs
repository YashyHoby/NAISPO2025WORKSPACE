using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartBumper2D : MonoBehaviour
{
    [Header("バウンド設定")]
    [Tooltip("衝突したハートに与える速度（m/s）。")]
    public float bounceSpeed = 12f;

    [Tooltip("既存の速度を上書きするかどうか。オフの場合は速度に加算します。")]
    public bool overrideVelocity = true;

    [Tooltip("押し出す方向に追加する上向きバイアスです（0 で無効）。")]
    public float upwardBias = 0f;

    [Header("ビジュアル演出")]
    [Tooltip("拡大／振動演出を適用する対象。未設定時はこのオブジェクトを使用します。")]
    public Transform visualTarget;

    [Tooltip("衝突時の最大拡大倍率です。1 を超える値を指定してください。")]
    public float visualScaleMultiplier = 1.25f;

    [Tooltip("振動演出が収束するまでの時間（秒）。")]
    public float visualDuration = 0.35f;

    [Tooltip("振動演出の減衰係数です。大きいほど早く収束します。")]
    public float visualShakeDamping = 6f;

    [Tooltip("振動演出の振動数（Hz）です。")]
    public float visualShakeFrequency = 10f;

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

        Vector2 impulse = dir * bounceSpeed;

        if (overrideVelocity)
            rb.linearVelocity = impulse;
        else
            rb.linearVelocity += impulse;

        heart.NotifyExternalBounce(rb.linearVelocity.normalized, bounceSpeed);

        StartVisualShake();
    }

    void StartVisualShake()
    {
        if (visualTarget == null || visualDuration <= 0f)
            return;

        if (visualRoutine != null)
            StopCoroutine(visualRoutine);

        visualRoutine = StartCoroutine(VisualShakeRoutine());
    }

    IEnumerator VisualShakeRoutine()
    {
        float timer = 0f;
        float amplitude = Mathf.Max(0f, visualScaleMultiplier - 1f);
        float omega = Mathf.Max(0.01f, visualShakeFrequency) * Mathf.PI * 2f;
        float damping = Mathf.Max(0f, visualShakeDamping);

        while (timer < visualDuration)
        {
            float envelope = Mathf.Exp(-damping * timer);
            float oscillation = Mathf.Cos(omega * timer);
            float scaleFactor = 1f + amplitude * envelope * oscillation;
            float minScale = Mathf.Max(0.2f, 1f - amplitude);
            scaleFactor = Mathf.Clamp(scaleFactor, minScale, 1f + amplitude);
            visualTarget.localScale = baseScale * scaleFactor;

            timer += Time.deltaTime;
            yield return null;
        }

        visualTarget.localScale = baseScale;
        visualRoutine = null;
    }
}