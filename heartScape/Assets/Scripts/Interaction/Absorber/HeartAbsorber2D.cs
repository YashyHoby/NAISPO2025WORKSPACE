using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartAbsorber2D : MonoBehaviour
{
    [Header("吸収設定")]
    [Tooltip("この数だけハートを取り込むと爆発します。")]
    public int requiredCaptureCount = 5;

    [Tooltip("吸い込みにかかる時間（秒）です。")]
    public float absorbDuration = 0.35f;

    [Tooltip("吸い込み中にハートを縮小させる倍率です。")]
    public float absorbScaleFactor = 0.6f;

    [Tooltip("吸収演出の補間カーブ（0～1）です。")]
    public AnimationCurve absorbEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("色の混ざり方")]
    [Tooltip("新しい色を取り込んだときに現在の色へどれだけ寄せるか（0～1）。")]
    [Range(0f, 1f)] public float mixFactor = 0.35f;

    [Tooltip("取り込むたびに増える透明度のステップです。")]
    public float alphaStep = 0.12f;

    [Header("ビジュアル参照")]
    [Tooltip("本体のスプライト。未指定の場合は子要素から検索します。")]
    public SpriteRenderer bodyRenderer;

    [Tooltip("内部のゆらめきを表現するパーティクル（任意）。色が更新されます。")]
    public ParticleSystem auraParticle;

    [Tooltip("吸収時に再生するパーティクル（任意）。")] public ParticleSystem absorbBurstPrefab;
    [Tooltip("爆発時に再生する花火パーティクル（任意）。")] public ParticleSystem explosionPrefab;

    [Header("再出現演出")]
    [Tooltip("爆発後に自動で再出現するかどうか。")]
    public bool autoRespawn = true;

    [Tooltip("再出現までの遅延（秒）です。")]
    public float respawnDelay = 3f;

    [Tooltip("フェードインにかける時間（秒）です。0 にすると即座に表示されます。")]
    public float fadeInDuration = 0.6f;

    [Tooltip("フェードインに使用する補間カーブです。")]
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("待機時の基本透明度です。")]
    [Range(0f, 1f)] public float idleAlpha = 0.2f;

    Collider2D absorberCollider;
    readonly HashSet<int> absorbingIds = new();
    readonly List<Color> capturedColors = new();
    Color baseColor = Color.white;
    Color idleColor = new Color(1f, 1f, 1f, 0f);
    int capturedCount = 0;
    bool isInactive = false;
    Coroutine fadeRoutine;

    void Awake()
    {
        absorberCollider = GetComponent<Collider2D>();
        absorberCollider.isTrigger = true;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer != null)
        {
            baseColor = bodyRenderer.color;
            idleColor = new Color(baseColor.r, baseColor.g, baseColor.b, idleAlpha);
            bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }
    }

    void OnEnable()
    {
        ResetState();
    }

    void ResetState()
    {
        absorbingIds.Clear();
        capturedColors.Clear();
        capturedCount = 0;
        isInactive = false;

        if (absorberCollider != null)
            absorberCollider.enabled = true;

        if (bodyRenderer != null)
        {
            bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            StartFadeIn();
        }

        UpdateAuraGradient();
    }

    void StartFadeIn()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        float timer = 0f;
        float duration = Mathf.Max(0f, fadeInDuration);

        if (duration <= Mathf.Epsilon)
        {
            if (bodyRenderer != null)
                bodyRenderer.color = idleColor;
            fadeRoutine = null;
            yield break;
        }

        while (timer < duration)
        {
            float t = timer / duration;
            float eased = fadeInCurve != null ? fadeInCurve.Evaluate(t) : t;
            if (bodyRenderer != null)
            {
                Color c = Color.Lerp(new Color(baseColor.r, baseColor.g, baseColor.b, 0f), idleColor, eased);
                bodyRenderer.color = c;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (bodyRenderer != null)
            bodyRenderer.color = idleColor;
        fadeRoutine = null;
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
            StartCoroutine(ExplodeRoutine());
    }

    void UpdateVisualFromCapture(Color newColor)
    {
        if (bodyRenderer != null)
        {
            Color current = bodyRenderer.color.a <= 0.001f ? idleColor : bodyRenderer.color;
            Color mixed = Color.Lerp(current, newColor, Mathf.Clamp01(mixFactor));
            float targetAlpha = Mathf.Clamp01(Mathf.Max(idleAlpha, current.a + alphaStep));
            mixed.a = targetAlpha;
            bodyRenderer.color = mixed;
        }

        UpdateAuraGradient();
    }

    void UpdateAuraGradient()
    {
        if (auraParticle == null)
            return;

        var main = auraParticle.main;
        var colorModule = auraParticle.colorOverLifetime;
        colorModule.enabled = true;

        if (capturedColors.Count == 0)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(idleColor, 0f), new GradientColorKey(idleColor, 1f) },
                new[] { new GradientAlphaKey(idleAlpha, 0f), new GradientAlphaKey(idleAlpha, 1f) }
            );
            var gradient = new ParticleSystem.MinMaxGradient(g);
            main.startColor = gradient;
            colorModule.color = gradient;
            return;
        }

        List<GradientColorKey> cKeys = new();
        List<GradientAlphaKey> aKeys = new();
        float step = capturedColors.Count > 1 ? 1f / (capturedColors.Count - 1) : 1f;
        for (int i = 0; i < capturedColors.Count; i++)
        {
            float t = capturedColors.Count > 1 ? Mathf.Clamp01(i * step) : 0.5f;
            Color c = capturedColors[i];
            cKeys.Add(new GradientColorKey(c, t));
            aKeys.Add(new GradientAlphaKey(Mathf.Clamp01(c.a), t));
        }
        Gradient gradientData = new Gradient();
        gradientData.SetKeys(cKeys.ToArray(), aKeys.ToArray());
        var minMax = new ParticleSystem.MinMaxGradient(gradientData);
        main.startColor = minMax;
        colorModule.color = minMax;
        if (!auraParticle.isPlaying)
            auraParticle.Play(true);
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
        if (absorberCollider != null)
            absorberCollider.enabled = false;

        Color explosionColor = GetExplosionColor();

        if (explosionPrefab != null)
        {
            var fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            var main = fx.main;
            main.startColor = new ParticleSystem.MinMaxGradient(explosionColor);
        }

        if (bodyRenderer != null)
            bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

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
        if (absorberCollider != null)
            absorberCollider.enabled = true;
        if (bodyRenderer != null)
        {
            bodyRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            StartFadeIn();
        }
        UpdateAuraGradient();
    }

    Color GetExplosionColor()
    {
        if (capturedColors.Count == 0)
            return Color.white;

        Color result = capturedColors[0];
        for (int i = 1; i < capturedColors.Count; i++)
        {
            float weight = 1f / (i + 1f);
            result = Color.Lerp(result, capturedColors[i], weight);
        }
        result.a = 1f;
        return result;
    }
}