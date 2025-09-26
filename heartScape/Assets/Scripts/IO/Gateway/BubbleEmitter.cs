using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeartScape.IO.Gateway
{
    [DefaultExecutionOrder(100)]
    public class BubbleEmitter : MonoBehaviour
    {
        [SerializeField] Settings settings = Settings.CreateDefault();
        [SerializeField] bool showGizmo = false;

        readonly List<BubbleData> activeBubbles = new();
        float emissionTimer;

        Vector3 lastForward = Vector3.right;
        Vector3 lastPerp = Vector3.up;

        void OnEnable()
        {
            emissionTimer = 0f;
        }

        void Update()
        {
            UpdateDirectionVectors();

            if (settings.enableEmission)
            {
                EmitIfNeeded();
            }

            UpdateBubbles();
        }

        void OnDisable()
        {
            ClearBubbles();
        }

        void OnDestroy()
        {
            ClearBubbles(true);
        }

        void EmitIfNeeded()
        {
            if (settings.emissionRate <= 0f)
            {
                return;
            }

            emissionTimer += Time.deltaTime;
            float interval = 1f / settings.emissionRate;

            while (emissionTimer >= interval)
            {
                emissionTimer -= interval;
                SpawnBubble();
            }
        }

        void SpawnBubble()
        {
            var bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(transform, false);

            var sr = bubbleObj.AddComponent<SpriteRenderer>();
            sr.sprite = settings.bubbleSprite != null ? settings.bubbleSprite : GetDefaultSprite();
            sr.material = settings.material;
            sr.sortingOrder = settings.sortingOrder;

            float sizeFactor = UnityEngine.Random.Range(settings.sizeRange.x, settings.sizeRange.y);
            float alphaFactor = UnityEngine.Random.Range(settings.alphaRange.x, settings.alphaRange.y);

            bubbleObj.transform.localScale = Vector3.one * sizeFactor;

            Color baseColor = settings.baseColor;
            baseColor.a *= alphaFactor;
            sr.color = baseColor;

            float travelT = UnityEngine.Random.value;
            float lateralWidth = Mathf.Lerp(settings.startWidth, settings.endWidth, travelT);
            float lateralOffset = (UnityEngine.Random.value - 0.5f) * lateralWidth;
            float distance = settings.travelDistance * travelT;
            float startOffset = settings.startOffset;

            Vector3 worldPos = transform.position + lastForward * (startOffset + distance) + lastPerp * lateralOffset;
            bubbleObj.transform.position = worldPos;

            float speed = UnityEngine.Random.Range(settings.speedRange.x, settings.speedRange.y);
            float lifetime = Mathf.Max(0.01f, settings.lifetime);

            var data = new BubbleData
            {
                transform = bubbleObj.transform,
                renderer = sr,
                baseColor = baseColor,
                startScale = bubbleObj.transform.localScale,
                speed = speed,
                direction = lastForward,
                startPosition = bubbleObj.transform.position,
                startOffset = startOffset,
                travelDistance = settings.travelDistance,
                lifetime = lifetime,
                age = 0f
            };

            activeBubbles.Add(data);
        }

        void UpdateBubbles()
        {
            if (activeBubbles.Count == 0) return;

            float dt = Time.deltaTime;

            for (int i = activeBubbles.Count - 1; i >= 0; i--)
            {
                var data = activeBubbles[i];
                if (data.transform == null || data.renderer == null)
                {
                    activeBubbles.RemoveAt(i);
                    continue;
                }

                data.age += dt;
                float t = Mathf.Clamp01(data.age / data.lifetime);

                // position along direction
                float distance = data.speed * dt;
                data.transform.position += data.direction * distance;

                // size over lifetime
                if (settings.sizeOverLifetime != null && settings.sizeOverLifetime.keys.Length > 0)
                {
                    float scaleMul = settings.sizeOverLifetime.Evaluate(t);
                    data.transform.localScale = data.startScale * scaleMul;
                }

            
                // fade
                if (settings.alphaOverLifetime != null && settings.alphaOverLifetime.keys.Length > 0)
                {
                    float alphaMul = settings.alphaOverLifetime.Evaluate(t);
                    Color c = data.baseColor;
                    c.a *= alphaMul;
                    data.renderer.color = c;
                }

                if (data.age >= data.lifetime)
                {
                    Destroy(data.transform.gameObject);
                    activeBubbles.RemoveAt(i);
                }
            }
        }

        void ClearBubbles(bool immediate = false)
        {
            foreach (var bubble in activeBubbles)
            {
                if (bubble.transform != null)
                {
                    if (immediate)
                    {
                        DestroyImmediate(bubble.transform.gameObject);
                    }
                    else
                    {
                        Destroy(bubble.transform.gameObject);
                    }
                }
            }
            activeBubbles.Clear();
        }

        void UpdateDirectionVectors()
        {
            Vector3 dir = settings.alignToEmitter ? transform.right : Vector3.right;
            if (dir.sqrMagnitude < 1e-6f)
            {
                dir = Vector3.right;
            }
            dir.Normalize();
            lastForward = dir;
            lastPerp = new Vector3(-dir.y, dir.x, 0f);
        }

        Sprite GetDefaultSprite()
        {
            if (Settings.cachedSprite == null)
            {
                Settings.cachedSprite = Settings.CreateFallbackSprite();
            }
            return Settings.cachedSprite;
        }

        void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;
            UpdateDirectionVectors();

            Vector3 origin = transform.position + lastForward * settings.startOffset;
            Vector3 end = origin + lastForward * settings.travelDistance;
            Vector3 halfStart = lastPerp * (settings.startWidth * 0.5f);
            Vector3 halfEnd = lastPerp * (settings.endWidth * 0.5f);

            Gizmos.color = settings.baseColor;
            Gizmos.DrawLine(origin - halfStart, end - halfEnd);
            Gizmos.DrawLine(origin + halfStart, end + halfEnd);
            Gizmos.DrawLine(origin - halfStart, origin + halfStart);
            Gizmos.DrawLine(end - halfEnd, end + halfEnd);
        }

        public void ApplySettings(Settings newSettings)
        {
            settings = newSettings != null ? newSettings.Clone() : Settings.CreateDefault();
            ClearBubbles();
            emissionTimer = 0f;
        }

        class BubbleData
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 startPosition;
            public Vector3 direction;
            public float speed;
            public float startOffset;
            public float travelDistance;
            public float lifetime;
            public float age;
            public Vector3 startScale;
            public Color baseColor;
        }

        [Serializable]
        public class Settings
        {
            public bool enableEmission = true;
            public float emissionRate = 4f;
            public float lifetime = 1.2f;
            public Vector2 sizeRange = new Vector2(0.08f, 0.14f);
            public Vector2 speedRange = new Vector2(1f, 1.6f);
            public Color baseColor = new Color(0.85f, 0.9f, 1f, 0.75f);
            public Vector2 alphaRange = new Vector2(0.5f, 0.9f);
            public Sprite bubbleSprite;
            public Material material;
            public int sortingOrder = 0;
            public bool alignToEmitter = true;
            public float startOffset = 0.05f;
            public float travelDistance = 1.2f;
            public float startWidth = 0.05f;
            public float endWidth = 0.2f;
            public AnimationCurve alphaOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            public AnimationCurve sizeOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

            internal static Sprite cachedSprite;

            public Settings Clone()
            {
                return new Settings
                {
                    enableEmission = enableEmission,
                    emissionRate = emissionRate,
                    lifetime = lifetime,
                    sizeRange = sizeRange,
                    speedRange = speedRange,
                    baseColor = baseColor,
                    alphaRange = alphaRange,
                    bubbleSprite = bubbleSprite,
                    material = material,
                    sortingOrder = sortingOrder,
                    alignToEmitter = alignToEmitter,
                    startOffset = startOffset,
                    travelDistance = travelDistance,
                    startWidth = startWidth,
                    endWidth = endWidth,
                    alphaOverLifetime = alphaOverLifetime != null ? new AnimationCurve(alphaOverLifetime.keys) : AnimationCurve.Linear(0f, 1f, 1f, 0f),
                    sizeOverLifetime = sizeOverLifetime != null ? new AnimationCurve(sizeOverLifetime.keys) : AnimationCurve.Linear(0f, 1f, 1f, 1f)
                };
            }

            public static Settings CreateDefault()
            {
                return new Settings();
            }

            internal static Sprite CreateFallbackSprite()
            {
                const int size = 32;
                Texture2D texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                Color[] pixels = new Color[size * size];
                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = (size * 0.5f) - 1f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Vector2 pos = new Vector2(x, y);
                        float distance = Vector2.Distance(pos, center);
                        if (distance <= radius)
                        {
                            float alpha = Mathf.Lerp(1f, 0.4f, distance / radius);
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                        else
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
