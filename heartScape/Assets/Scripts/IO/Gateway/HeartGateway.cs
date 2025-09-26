using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeartScape.IO.Gateway

{
    public class HeartGateway : MonoBehaviour
    {
        [Header("References")]
        public HeartManager manager;

        [Header("Emitters (A/B/C/D)")]
        [Tooltip("Spawn positions for switches A/B/C/D. Transform.right is treated as launch direction.")]
        public Transform[] emitters = new Transform[4];

        [Header("Launch Parameters")]
        public float baseSpeed = 3f;
        public float speedJitter = 0.5f;
        public float spreadDeg = 5f;

        [Header("Data Source")]
        [Tooltip("When true and UID is supplied, prefer profiles stored in HeartDB.")]
        public bool preferDbIfUid = true;

        [Header("Press Time Size Mapping")]
        public bool applyPressTimeScaling = true;
        public Vector2 pressTimeRange = new Vector2(0f, 2f);
        public Vector2 pressTimeScaleRange = new Vector2(0.8f, 1.3f);
        public AnimationCurve pressTimeScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Emitter Visual Settings")]
        public BubbleEmitter.Settings defaultEmitterVisual = BubbleEmitter.Settings.CreateDefault();
        public bool usePerEmitterVisuals = false;
        public List<PerEmitterVisual> perEmitterVisualSettings = new();
#if UNITY_EDITOR
        [NonSerialized] bool editorRefreshScheduled;
#endif

        [Header("Auto Spawn Settings")]
        public bool enableAutoSpawn = true;
        public int minAgentsBeforeAutoSpawn = 10;
        public float autoSpawnInterval = 3f;
        [Tooltip("Emitter index used for automatic spawns (-1 = random).")]
        public int autoSpawnEmitterIndex = -1;

        readonly ConcurrentQueue<HeartInput> queue = new();
        float autoSpawnTimer;

        [Serializable]
        public class HeartInput
        {
            public string uid;
            public int switchNo;
            public float hr;
            public float cv;
            public float range;
            public float mean;
            public float pressTime;
            public float dynRange;
            public float hue;
            public bool hasHue;
        }

        [Serializable]
        public class PerEmitterVisual
        {
            public int emitterIndex;
            public bool overrideSettings = false;
            public BubbleEmitter.Settings settings = BubbleEmitter.Settings.CreateDefault();
        }

        void Awake()
        {
            ApplyEmitterVisualsImmediate();
        }

        void Start()
        {
            ApplyEmitterVisualsImmediate();
        }

        void Update()
        {
            while (queue.TryDequeue(out var msg))
            {
                SpawnFromMessage(msg);
            }

            HandleAutoSpawn(Time.deltaTime);
        }
#if UNITY_EDITOR
        void OnValidate()
        {
            if (pressTimeRange.y < pressTimeRange.x)
            {
                pressTimeRange.y = pressTimeRange.x + 0.01f;
            }

            if (pressTimeScaleRange.y < pressTimeScaleRange.x)
            {
                pressTimeScaleRange.y = pressTimeScaleRange.x;
            }

            SyncPerEmitterVisuals();

            if (UnityEditor.EditorApplication.isPlaying)
            {
                ApplyEmitterVisualsImmediate();
            }
            else
            {
                ScheduleEditorVisualRefresh();
            }
        }

        void ScheduleEditorVisualRefresh()
        {
            if (editorRefreshScheduled) return;
            editorRefreshScheduled = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                editorRefreshScheduled = false;
                if (this == null) return;
                ApplyEmitterVisualsImmediate();
            };
        }
#endif



        public void Inject(HeartInput msg)
        {
            if (msg != null)
            {
                queue.Enqueue(msg);
            }
        }

        void SpawnFromMessage(HeartInput m)
        {
            if (manager == null || emitters == null || emitters.Length == 0)
            {
                Debug.LogWarning("[HeartGateway] Missing manager or emitters.");
                return;
            }

            int idx = Mathf.Clamp(m.switchNo - 1, 0, emitters.Length - 1);
            var emitter = emitters[idx];
            if (emitter == null)
            {
                Debug.LogWarning($"[HeartGateway] Emitter for switch {m.switchNo} missing.");
                return;
            }

            HeartProfile hp = ResolveProfile(m);
            Vector2 pos = emitter.position;
            Vector2 dir = emitter.right;
            if (spreadDeg > 0f)
            {
                float ang = UnityEngine.Random.Range(-spreadDeg, spreadDeg);
                dir = (Vector2)(Quaternion.Euler(0, 0, ang) * dir);
            }
            float spd = Mathf.Max(0f, baseSpeed + UnityEngine.Random.Range(-speedJitter, speedJitter));
            Vector2 vel = dir.normalized * spd;

            var agent = manager.Spawn(hp, pos, vel);
            if (agent != null)
            {
                ApplyInputOverrides(m, agent);
            }
        }

        HeartProfile ResolveProfile(HeartInput m)
        {
            HeartProfile hp = null;
            bool hasMsgValues = !(Mathf.Approximately(m.hr, 0f) && Mathf.Approximately(m.cv, 0f)
                                  && Mathf.Approximately(m.range, 0f) && Mathf.Approximately(m.mean, 0f));

            if (preferDbIfUid && !string.IsNullOrEmpty(m.uid) && !hasMsgValues)
            {
                hp = HeartDB.Get(m.uid);
            }
            else if (hasMsgValues)
            {
                string resolvedUid = !string.IsNullOrEmpty(m.uid)
                    ? m.uid
                    : $"MSG_{Guid.NewGuid():N}".Substring(0, 8);

                hp = new HeartProfile
                {
                    uid   = resolvedUid,
                    hr    = m.hr,
                    cv    = m.cv,
                    range = m.range,
                    mean  = m.mean
                };
            }
            else if (!string.IsNullOrEmpty(m.uid))
            {
                hp = HeartDB.Get(m.uid);
            }
            else
            {
                hp = HeartDB.Get(null);
            }

            return hp;
        }

        public void ApplyInputOverrides(HeartInput input, HeartAgent agent)
        {
            if (input == null || agent == null) return;

            var visual = agent.GetComponent<HeartVisual>();

            if (input.hasHue && visual != null)
            {
                float _, s, v;
                Color.RGBToHSV(visual.color, out _, out s, out v);
                float hue = Mathf.Repeat(input.hue, 1f);
                visual.color = Color.HSVToRGB(hue, s, v);
                visual.RefreshMaterial();
            }

            if (applyPressTimeScaling && visual != null)
            {
                float minTime = Mathf.Min(pressTimeRange.x, pressTimeRange.y);
                float maxTime = Mathf.Max(pressTimeRange.x, pressTimeRange.y);
                float clampedTime = Mathf.Clamp(input.pressTime, minTime, maxTime);
                float normalized = (Mathf.Abs(maxTime - minTime) < Mathf.Epsilon)
                    ? 0.5f
                    : Mathf.InverseLerp(minTime, maxTime, clampedTime);
                float curveValue = pressTimeScaleCurve != null ? pressTimeScaleCurve.Evaluate(normalized) : normalized;
                float multiplier = Mathf.Lerp(pressTimeScaleRange.x, pressTimeScaleRange.y, Mathf.Clamp01(curveValue));
                float baseRadius = visual.radius;
                float targetRadius = Mathf.Max(0.01f, baseRadius * multiplier);
                visual.SetRadius(targetRadius);

                var growth = agent.GetComponent<HeartGrowth>();
                if (growth != null)
                {
                    growth.baseRadius = targetRadius;
                }
            }
        }

        void HandleAutoSpawn(float deltaTime)
        {
            if (!enableAutoSpawn || manager == null) return;

            if (manager.agents.Count >= minAgentsBeforeAutoSpawn)
            {
                autoSpawnTimer = 0f;
                return;
            }

            autoSpawnTimer += deltaTime;
            if (autoSpawnTimer < autoSpawnInterval) return;
            autoSpawnTimer = 0f;

            SpawnAutoHeart();
        }

        void SpawnAutoHeart()
        {
            if (emitters == null || emitters.Length == 0) return;
            if (manager == null || manager.agentPrefab == null) return;

            int index = autoSpawnEmitterIndex;
            if (index < 0 || index >= emitters.Length || emitters[index] == null)
            {
                index = UnityEngine.Random.Range(0, emitters.Length);
            }

            var emitter = emitters[index];
            if (emitter == null) return;

            var profile = GenerateRandomProfile();
            if (profile == null) return;

            Vector2 pos = emitter.position;
            Vector2 dir = emitter.right;
            float speed = Mathf.Max(0f, baseSpeed + UnityEngine.Random.Range(-speedJitter, speedJitter));
            Vector2 vel = dir.normalized * speed;

            var agent = manager.Spawn(profile, pos, vel);
            if (agent != null)
            {
                var autoInput = new HeartInput
                {
                    uid = profile.uid,
                    switchNo = index + 1,
                    hr = profile.hr,
                    cv = profile.cv,
                    range = profile.range,
                    mean = profile.mean,
                    pressTime = UnityEngine.Random.Range(pressTimeRange.x, pressTimeRange.y),
                    dynRange = profile.range,
                    hue = 0f,
                    hasHue = false
                };
                ApplyInputOverrides(autoInput, agent);
            }
        }

        HeartProfile GenerateRandomProfile()
        {
            var mapper = manager != null ? manager.appearance : null;
            Vector2 hrRange = mapper != null ? mapper.hrRange : new Vector2(60f, 100f);
            Vector2 cvRange = mapper != null ? mapper.cvRange : new Vector2(0.05f, 0.2f);
            Vector2 rngRange = mapper != null ? mapper.rngRange : new Vector2(10f, 35f);
            Vector2 meanRange = mapper != null ? mapper.meanRange : new Vector2(65f, 95f);

            return new HeartProfile
            {
                uid = $"AUTO_{Guid.NewGuid():N}".Substring(0, 8),
                hr = UnityEngine.Random.Range(hrRange.x, hrRange.y),
                cv = UnityEngine.Random.Range(cvRange.x, cvRange.y),
                range = UnityEngine.Random.Range(rngRange.x, rngRange.y),
                mean = UnityEngine.Random.Range(meanRange.x, meanRange.y)
            };
        }

        void ApplyEmitterVisualsImmediate()
        {
            if (emitters == null) return;
            SyncPerEmitterVisuals();

            for (int i = 0; i < emitters.Length; i++)
            {
                var emitter = emitters[i];
                if (emitter == null) continue;

                var bubble = emitter.GetComponent<BubbleEmitter>();
                if (bubble == null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        bubble = UnityEditor.Undo.AddComponent<BubbleEmitter>(emitter.gameObject);
                    }
                    else
#endif
                    {
                        bubble = emitter.gameObject.AddComponent<BubbleEmitter>();
                    }
                }

                var settings = defaultEmitterVisual.Clone();
                if (usePerEmitterVisuals)
                {
                    var per = perEmitterVisualSettings.Find(p => p.emitterIndex == i && p.overrideSettings);
                    if (per != null)
                    {
                        settings = per.settings.Clone();
                    }
                }

                bubble.ApplySettings(settings);
            }
        }

        void SyncPerEmitterVisuals()
        {
            if (emitters == null) return;

            for (int i = 0; i < emitters.Length; i++)
            {
                if (perEmitterVisualSettings.Exists(p => p.emitterIndex == i)) continue;
                perEmitterVisualSettings.Add(new PerEmitterVisual { emitterIndex = i });
            }

            perEmitterVisualSettings.RemoveAll(p => p.emitterIndex < 0 || p.emitterIndex >= emitters.Length);
        }
    }
}
