using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeartAbsorber2D : MonoBehaviour
{
    [Header("吸収設定")]
    [Tooltip("この数だけハートを取り込むと爆発します。")]
    public int requiredCaptureCount = 10;

    [Tooltip("吸い込みにかかる時間（秒）です。")]
    public float absorbDuration = 0.45f;

    [Tooltip("吸い込み中にハートを縮小させる倍率です。")]
    public float absorbScaleFactor = 0.55f;

    [Tooltip("接触点からバブル中心へ寄せる比率（0 = 接点, 1 = 中央までの割合）です。")]
    [Range(0f, 1f)] public float absorbAnchorInward = 0.2f;
    [Tooltip("吸収演出の補間カーブ（0～1）です。")]
    public AnimationCurve absorbEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("接触した地点から吸い込みエフェクトを起こすか。オフにすると中心から吸い込みます。")]
    public bool injectAtContactPoint = true;

    [Header("参照コンポーネント")]
    public BubbleLiquidSites2D bubbleLiquid;
    public BubbleShellDeformer2D shellDeformer;
    public BubblePopVFX bubblePopVFX;
    public SpriteRenderer bodyRenderer;
    public ParticleSystem absorbBurstPrefab;
    public Collider2D absorberCollider;
    public Transform visualRoot;
    public Transform scaleRoot;

    [Header("再出現演出")]
    [Tooltip("爆発後に自動で再出現するかどうか。")]
    public bool autoRespawn = true;

    [Tooltip("再出現までの遅延（秒）です。")]
    public float respawnDelay = 3f;

    [Tooltip("フェードインにかける時間（秒）です。0 にすると即座に表示します。")]
    public float fadeInDuration = 0.6f;

    [Tooltip("フェードインに使用する補間カーブです。")]
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("待機時の基本透明度です。")]
    [Range(0f, 1f)] public float idleAlpha = 0.2f;

    [Tooltip("再出現時に少し膨らむオーバーシュート倍率です。")]
    public float respawnOvershoot = 1.08f;

    [Tooltip("爆発前に縮む倍率です。")]
    public float collapseScaleFactor = 0.7f;

    readonly HashSet<int> absorbingIds = new();
    readonly List<Color> capturedColors = new();

    Coroutine fadeRoutine;
    Coroutine balloonRoutine;

    Vector3 originalScale = Vector3.one;
    bool isInactive;

    void Awake()
    {
        if (!absorberCollider)
            absorberCollider = GetComponent<Collider2D>();
        if (absorberCollider != null)
            absorberCollider.isTrigger = true;

        if (visualRoot == null)
            visualRoot = transform;
        if (scaleRoot == null)
            scaleRoot = visualRoot;

        originalScale = scaleRoot.localScale;

        if (bodyRenderer == null)
            bodyRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();

        if (bubblePopVFX != null)
        {
            if (bubbleLiquid != null)
                bubblePopVFX.liquidSites = bubbleLiquid;
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
        isInactive = false;

        if (bubbleLiquid != null)
            bubbleLiquid.ClearAll();
        if (shellDeformer != null)
            shellDeformer.ClearImpulses();

        if (visualRoot != null)
            visualRoot.gameObject.SetActive(true);

        if (scaleRoot != null)
            scaleRoot.localScale = originalScale * 0.2f;

        if (bodyRenderer != null)
        {
            var c = bodyRenderer.color;
            c.a = 0f;
            bodyRenderer.color = c;
            bodyRenderer.enabled = true;
        }

        if (absorberCollider != null)
            absorberCollider.enabled = true;

        StartAppearanceSequence();
    }

    void StartAppearanceSequence()
    {
        if (balloonRoutine != null)
            StopCoroutine(balloonRoutine);
        balloonRoutine = StartCoroutine(BalloonRoutine());

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        if (bodyRenderer == null)
            yield break;

        float timer = 0f;
        float duration = Mathf.Max(0f, fadeInDuration);
        while (timer < duration)
        {
            float t = duration <= Mathf.Epsilon ? 1f : timer / duration;
            float eased = fadeInCurve != null ? fadeInCurve.Evaluate(t) : t;
            var col = bodyRenderer.color;
            col.a = Mathf.Lerp(0f, idleAlpha, eased);
            bodyRenderer.color = col;
            timer += Time.deltaTime;
            yield return null;
        }
        var finalCol = bodyRenderer.color;
        finalCol.a = idleAlpha;
        bodyRenderer.color = finalCol;
        fadeRoutine = null;
    }

    IEnumerator BalloonRoutine()
    {
        if (scaleRoot == null)
            yield break;

        Vector3 start = originalScale * 0.2f;
        Vector3 overshoot = originalScale * respawnOvershoot;
        float timer = 0f;
        const float growDuration = 0.5f;
        while (timer < growDuration)
        {
            float t = timer / growDuration;
            float eased = Mathf.Sin(t * 1.570796f); // easeOutSine
            scaleRoot.localScale = Vector3.Lerp(start, overshoot, eased);
            timer += Time.deltaTime;
            yield return null;
        }
        float settleTime = 0f;
        const float settleDuration = 0.2f;
        while (settleTime < settleDuration)
        {
            float t = settleTime / settleDuration;
            scaleRoot.localScale = Vector3.Lerp(scaleRoot.localScale, originalScale, t);
            settleTime += Time.deltaTime;
            yield return null;
        }
        scaleRoot.localScale = originalScale;
        balloonRoutine = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isInactive)
            return;

        var heart = other.GetComponentInParent<HeartPhysics>();
        if (heart == null)
            return;
        if (!absorbingIds.Add(heart.GetInstanceID()))
            return;

        StartCoroutine(AbsorbHeartRoutine(heart, other));
    }

    IEnumerator AbsorbHeartRoutine(HeartPhysics heart, Collider2D otherCollider)
    {
		var heartTransform = heart != null ? heart.transform : null;
        var rb = heart.GetComponent<Rigidbody2D>();
        var heartVisual = heart.GetComponent<HeartVisual>();
        Color heartColor = heartVisual != null ? heartVisual.color : Color.white;

        Vector2 contactPoint = injectAtContactPoint && otherCollider != null
            ? otherCollider.ClosestPoint(transform.position)
            : (Vector2)heartTransform.position;

        if (bubbleLiquid != null)
            bubbleLiquid.AddFromWorld(contactPoint, heartColor, absorbDuration);
        if (shellDeformer != null)
            shellDeformer.AddImpulseWorld(contactPoint);
        if (absorbBurstPrefab != null)
        {
            var fx = Instantiate(absorbBurstPrefab, contactPoint, Quaternion.identity);
            var main = fx.main;
            main.startColor = heartColor;
        }

        if (rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
        }

		if (heartTransform == null)
		{
			yield break;
		}

		Vector3 startPos = heartTransform.position;
		Vector3 anchor = new Vector3(contactPoint.x, contactPoint.y, heartTransform.position.z);
		Vector3 center = new Vector3(transform.position.x, transform.position.y, heartTransform.position.z);
        Vector3 targetPos = Vector3.Lerp(anchor, center, Mathf.Clamp01(absorbAnchorInward));
        Vector3 startScale = heartTransform.localScale;
        Vector3 targetScale = startScale * Mathf.Max(0.01f, absorbScaleFactor);

        float timer = 0f;
        float duration = Mathf.Max(0.01f, absorbDuration);
		while (timer < duration)
        {
			if (heart == null || heartTransform == null)
			{
				yield break;
			}
            float t = timer / duration;
            float eased = absorbEase != null ? absorbEase.Evaluate(t) : t;
			if (heartTransform != null)
			{
				heartTransform.position = Vector3.Lerp(startPos, targetPos, eased);
				heartTransform.localScale = Vector3.Lerp(startScale, targetScale, eased);
			}
            timer += Time.deltaTime;
            yield return null;
        }

		if (heart != null)
		{
			Destroy(heart.gameObject);
			absorbingIds.Remove(heart.GetInstanceID());
		}

        HeartSoundManager.Instance?.PlayAbsorb(transform.position);

        capturedColors.Add(heartColor);

        if (capturedColors.Count >= requiredCaptureCount)
        {
            StartCoroutine(ExplodeAndRespawn());
        }
    }

    IEnumerator ExplodeAndRespawn()
    {
        if (isInactive)
            yield break;

        isInactive = true;
        if (absorberCollider != null)
            absorberCollider.enabled = false;

        if (scaleRoot != null)
        {
            Vector3 start = scaleRoot.localScale;
            Vector3 end = originalScale * Mathf.Clamp(collapseScaleFactor, 0.2f, 1f);
            float timer = 0f;
            const float collapseDuration = 0.12f;
            while (timer < collapseDuration)
            {
                float t = timer / collapseDuration;
                scaleRoot.localScale = Vector3.Lerp(start, end, Mathf.Sin(t * 1.570796f));
                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (bubblePopVFX != null)
            bubblePopVFX.PlayPop();

        if (bubbleLiquid != null)
            bubbleLiquid.ClearAll();
        if (shellDeformer != null)
            shellDeformer.ClearImpulses();

        if (bodyRenderer != null)
        {
            var col = bodyRenderer.color;
            col.a = 0f;
            bodyRenderer.color = col;
        }

        if (visualRoot != null)
            visualRoot.gameObject.SetActive(false);

        if (!autoRespawn)
            yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));

        capturedColors.Clear();
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(true);

        if (scaleRoot != null)
            scaleRoot.localScale = originalScale * 0.2f;

        if (bodyRenderer != null)
        {
            var col = bodyRenderer.color;
            col.a = 0f;
            bodyRenderer.color = col;
            bodyRenderer.enabled = true;
        }

        if (bubbleLiquid != null)
            bubbleLiquid.ClearAll();
        if (shellDeformer != null)
            shellDeformer.ClearImpulses();

        if (absorberCollider != null)
            absorberCollider.enabled = true;

        isInactive = false;
        StartAppearanceSequence();
    }
}

