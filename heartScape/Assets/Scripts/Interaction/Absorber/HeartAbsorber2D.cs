using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartAbsorber2D : MonoBehaviour
{
    [Header("吸収設定")]
    [Tooltip("この数だけハートを取り込むと爆発します。")]
    public int requiredCaptureCount = 5;

    [Tooltip("吸い込みに要する時間（秒）。")]
    public float absorbDuration = 0.35f;

    [Tooltip("吸い込み中にハートを縮小させる倍率です。")]
    public float absorbScaleFactor = 0.6f;

    [Tooltip("吸収演出の補間カーブです（0→1）。")]
    public AnimationCurve absorbEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("色の混ざり方")]
    [Tooltip("新しい色を取り込んだ際に現在の色へどれだけ寄せるか。0～1。")]
    [Range(0f, 1f)] public float mixFactor = 0.35f;

    [Tooltip("取り込むたびに増える透明度のステップです。")]
    public float alphaStep = 0.12f;

    [Header("ビジュアル参照")]
    [Tooltip("本体のスプライト。未指定の場合は子から検索します。")]
    public SpriteRenderer bodyRenderer;

    [Tooltip("内部でゆらめくパーティクル（任意）。開始色が更新されます。")]
    public ParticleSystem auraParticle;

    [Tooltip("吸収時に再生するパーティクル（任意）。")] public ParticleSystem absorbBurstPrefab;
    [Tooltip("爆発時に再生するパーティクル（任意）。")] public ParticleSystem explosionPrefab;

    [Header("再出現")]
    [Tooltip("爆発後に再出現するまでの遅延（秒）。")] public float respawnDelay = 3f;

    [Tooltip("爆発後に自動的に再出現するか。")] public bool autoRespawn = true;

    Collider2D absorberCollider;
    readonly HashSet<int> absorbingIds = new();
    readonly List<Color> capturedColors = new();
    Color baseColor = new Color(1f, 1f, 1f, 0f);
    int capturedCount = 0;
    bool isInactive = false;

    void Awake()
    {
        absorberCollider = GetComponent<Collider2D>();
        absorberCollider.isTrigger = true;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer != null)
        {
            baseColor = bodyRenderer.color;
            baseColor.a = 0f;
            bodyRenderer.color = baseColor;
        }
    }

    void OnEnable()
    {
        if (bodyRenderer != null)
            bodyRenderer.color = baseColor;
        capturedColors.Clear();
        capturedCount = 0;
        isInactive = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isInactive) return;

        var heart = other.GetComponentInParent<HeartPhysics>();
        if (heart == null) return;
        if (!absorbingIds.Add(heart.GetInstanceID())) return;

        StartCoroutine(AbsorbHeartRoutine(heart));
    }

    IEnumerator AbsorbHeartRoutine(HeartPhysics heart)
    {
        var heartTransform = heart.transform;
        var rb = heart.GetComponent<Rigidbody2D>();
        var heartVisual = heart.GetComponent<HeartVisual>();
        Color heartColor = heartVisual != null ? heartVisual.color : Color.white;

        if (rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
        }

        Vector3 startPos = heartTransform.position;
        Vector3 startScale = heartTransform.localScale;
        Vector3 endPos = transform.position;
        Vector3 endScale = startScale * Mathf.Max(0.01f, absorbScaleFactor);

        float timer = 0f;
        float duration = Mathf.Max(0.01f, absorbDuration);

        while (timer < duration)
        {
            float t = timer / duration;
            float eased = absorbEase != null ? absorbEase.Evaluate(t) : t;
            heartTransform.position = Vector3.Lerp(startPos, endPos, eased);
            heartTransform.localScale = Vector3.Lerp(startScale, endScale, eased);

            timer += Time.deltaTime;
            yield return null;
        }

        heartTransform.position = endPos;

        Destroy(heart.gameObject);
        absorbingIds.Remove(heart.GetInstanceID());

        capturedCount++;
        capturedColors.Add(heartColor);
        UpdateVisualFromCapture(heartColor);
        SpawnAbsorbEffect(heartColor);

        if (capturedCount >= requiredCaptureCount)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    void UpdateVisualFromCapture(Color newColor)
    {
        if (bodyRenderer == null) return;

        Color current = bodyRenderer.color.a <= 0.001f ? new Color(newColor.r, newColor.g, newColor.b, 0f) : bodyRenderer.color;
        Color mixed = Color.Lerp(current, newColor, Mathf.Clamp01(mixFactor));
        float targetAlpha = Mathf.Clamp01(current.a + alphaStep);
        mixed.a = targetAlpha;
        bodyRenderer.color = mixed;

        if (auraParticle != null)
        {
            var main = auraParticle.main;
            main.startColor = new ParticleSystem.MinMaxGradient(mixed);
        }
    }

    void SpawnAbsorbEffect(Color color)
    {
        if (absorbBurstPrefab == null) return;
        var fx = Instantiate(absorbBurstPrefab, transform.position, Quaternion.identity);
        var main = fx.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color);
    }

    IEnumerator ExplodeRoutine()
    {
        isInactive = true;
        absorberCollider.enabled = false;

        Color explosionColor = GetExplosionColor();

        if (explosionPrefab != null)
        {
            var fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            var main = fx.main;
            main.startColor = new ParticleSystem.MinMaxGradient(explosionColor);
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }

        capturedColors.Clear();
        capturedCount = 0;

        if (autoRespawn)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));
            Respawn();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void Respawn()
    {
        isInactive = false;
        absorberCollider.enabled = true;
        if (bodyRenderer != null)
            bodyRenderer.color = baseColor;
    }

    Color GetExplosionColor()
    {
        if (capturedColors.Count == 0)
            return Color.white;

        Color result = Color.black;
        for (int i = 0; i < capturedColors.Count; i++)
        {
            float weight = 1f / (i + 1);
            result = Color.Lerp(result, capturedColors[i], weight);
        }
        result.a = 1f;
        return result;
    }
}
