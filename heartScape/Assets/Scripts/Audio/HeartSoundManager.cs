using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-200)]
public class HeartSoundManager : MonoBehaviour
{
    public static HeartSoundManager Instance { get; private set; }

    [Header("Audio Source Pool")]
    [SerializeField, Min(1)] int poolSize = 8;
    [SerializeField] AudioMixerGroup outputMixer;
    [SerializeField, Range(0f, 1f)] float spatialBlend = 0f;
    [SerializeField] bool persistent = false;

    [Header("Background Music")]
    [SerializeField] bool playBgmOnStart = false;
    [SerializeField] bool loopBgmPlaylist = true;
    [SerializeField, Min(0f)] float defaultBgmGapSeconds = 1f;
    [SerializeField] List<BgmTrack> bgmTracks = new();

    [Header("Wall Detection")]
    [SerializeField] LayerMask wallLayers = 0;
    [SerializeField] string wallTag = "Wall";
    [SerializeField] string[] wallNameKeywords = new[] { "Wall" };

    [Header("Agent Collision")]
    [SerializeField] AudioClip agentCollisionClip;
    [SerializeField] Vector2 combinedRadiusRange = new Vector2(0.8f, 2.4f);
    [SerializeField] Vector2 collisionPitchRange = new Vector2(1.1f, 0.7f);
    [SerializeField] Vector2 collisionVolumeRange = new Vector2(0.25f, 1f);
    [SerializeField] float collisionImpactForMaxVolume = 4f;

    [Header("Wall Collision")]
    [SerializeField] AudioClip wallCollisionClip;
    [SerializeField] Vector2 wallCollisionVolumeRange = new Vector2(0.3f, 0.9f);
    [SerializeField] float wallImpactForMaxVolume = 3f;

    [Header("Spawn / Absorb")]
    [SerializeField] AudioClip spawnClip;
    [SerializeField, Range(0f, 2f)] float spawnVolume = 0.8f;
    [SerializeField] AudioClip absorbClip;
    [SerializeField, Range(0f, 2f)] float absorbVolume = 1f;
    [SerializeField] AudioClip absorberBurstClip;
    [SerializeField, Range(0f, 2f)] float absorberBurstVolume = 1f;
    [SerializeField, Range(0f, 4f)] float absorberBurstDelay = 0f;

    [Header("Bumper Clips")]
    [SerializeField] List<BumperClip> bumperClips = new();

    [Header("Misc Clips")]
    [SerializeField] AudioClip bubbleBurstClip;
    [SerializeField, Range(0f, 2f)] float bubbleBurstVolume = 0.8f;
    [SerializeField] List<MaterialClip> materialClips = new();

    readonly List<AudioSource> sources = new();
    readonly Dictionary<string, BumperClip> bumperMap = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, MaterialClip> materialMap = new(StringComparer.OrdinalIgnoreCase);
    int nextSource;

    AudioSource bgmSource;
    Coroutine bgmRoutine;
    int bgmIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[HeartSoundManager] Duplicate instance detected. Removing new instance.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (persistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        SetupSources();
        EnsureBgmSource();
        RebuildLookups();

        if (playBgmOnStart && bgmTracks.Count > 0)
        {
            StartBgmPlaylist();
        }
    }

    void OnEnable()
    {
        if (playBgmOnStart && bgmTracks.Count > 0 && bgmRoutine == null)
        {
            StartBgmPlaylist();
        }
    }

    void OnDisable()
    {
        StopBgmPlaylist();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnValidate()
    {
        poolSize = Mathf.Max(1, poolSize);
        SetupSources();
        RebuildLookups();

        if (Application.isPlaying)
        {
            EnsureBgmSource();
        }
        else
        {
            var existing = transform.Find("HeartSoundManager_BGM");
            if (existing != null)
            {
                bgmSource = existing.GetComponent<AudioSource>();
            }
        }

        ConfigureBgmSource(bgmSource);
    }

    void SetupSources()
    {
        if (sources.Count > 0 && sources.Count >= poolSize && Application.isPlaying)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                ConfigureSource(sources[i]);
            }
            return;
        }

        sources.Clear();
        var existing = GetComponents<AudioSource>();
        for (int i = 0; i < existing.Length && sources.Count < poolSize; i++)
        {
            ConfigureSource(existing[i]);
            sources.Add(existing[i]);
        }

        while (sources.Count < poolSize)
        {
            var src = gameObject.AddComponent<AudioSource>();
            ConfigureSource(src);
            sources.Add(src);
        }

        nextSource = 0;
    }

    void EnsureBgmSource()
    {
        if (bgmSource != null) return;

        Transform found = transform.Find("HeartSoundManager_BGM");
        if (found != null)
        {
            bgmSource = found.GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = found.gameObject.AddComponent<AudioSource>();
            }
        }
        else
        {
            var go = new GameObject("HeartSoundManager_BGM");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            bgmSource = go.AddComponent<AudioSource>();
        }

        ConfigureBgmSource(bgmSource);
    }

    void ConfigureSource(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.loop = false;
        src.outputAudioMixerGroup = outputMixer;
        src.spatialBlend = spatialBlend;
    }

    void ConfigureBgmSource(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.loop = false;
        src.outputAudioMixerGroup = outputMixer;
        src.spatialBlend = spatialBlend;
    }

    void RebuildLookups()
    {
        bumperMap.Clear();
        if (bumperClips != null)
        {
            foreach (var entry in bumperClips)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null) continue;
                bumperMap[entry.key.Trim()] = entry;
            }
        }

        materialMap.Clear();
        if (materialClips != null)
        {
            foreach (var entry in materialClips)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null) continue;
                materialMap[entry.key.Trim()] = entry;
            }
        }
    }

    AudioSource GetSource()
    {
        if (sources.Count == 0)
        {
            var src = gameObject.AddComponent<AudioSource>();
            ConfigureSource(src);
            sources.Add(src);
        }

        var source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Count;
        return source;
    }

    float RemapVolume(float value, float maxValue, Vector2 range)
    {
        float t = maxValue > 0f ? Mathf.Clamp01(value / maxValue) : 1f;
        return Mathf.Lerp(range.x, range.y, t);
    }

    public void StartBgmPlaylist()
    {
        if (bgmTracks == null || bgmTracks.Count == 0)
        {
            Debug.LogWarning("[HeartSoundManager] BGM playlist is empty.");
            return;
        }

        EnsureBgmSource();

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        bgmIndex = Mathf.Clamp(bgmIndex, 0, Mathf.Max(0, bgmTracks.Count - 1));
        bgmRoutine = StartCoroutine(BgmPlaylistRoutine());
    }

    public void StopBgmPlaylist()
    {
        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }

        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    IEnumerator BgmPlaylistRoutine()
    {
        if (bgmTracks == null || bgmTracks.Count == 0)
        {
            yield break;
        }

        EnsureBgmSource();

        while (true)
        {
            if (bgmIndex < 0 || bgmIndex >= bgmTracks.Count)
            {
                if (loopBgmPlaylist)
                {
                    bgmIndex = 0;
                }
                else
                {
                    bgmRoutine = null;
                    yield break;
                }
            }

            var entry = bgmTracks[bgmIndex];
            if (entry != null && entry.clip != null && bgmSource != null)
            {
                bgmSource.clip = entry.clip;
                bgmSource.volume = Mathf.Clamp01(entry.volume);
                bgmSource.Play();

                while (bgmSource.isPlaying)
                {
                    yield return null;
                }

                float gap = entry.gapSeconds >= 0f ? entry.gapSeconds : defaultBgmGapSeconds;
                if (gap > 0f)
                {
                    yield return new WaitForSeconds(gap);
                }
            }
            else
            {
                yield return null;
            }

            bgmIndex++;
            if (bgmIndex >= bgmTracks.Count && !loopBgmPlaylist)
            {
                break;
            }
        }

        bgmRoutine = null;
    }

    public void RequestBgmRestart(int startIndex = 0)
    {
        bgmIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, (bgmTracks?.Count ?? 1) - 1));
        StartBgmPlaylist();
    }

    public void OnHeartCollision(HeartPhysics self, Collision2D collision, float impactSpeed)
    {
        if (self == null || collision == null || impactSpeed <= 0f) return;

        var otherRb = collision.rigidbody;
        var otherHeart = otherRb ? otherRb.GetComponent<HeartPhysics>() : null;

        if (otherHeart != null)
        {
            if (self.GetInstanceID() > otherHeart.GetInstanceID()) return;

            float radiusA = self.CurrentRadius;
            float radiusB = otherHeart.CurrentRadius;
            float combined = radiusA + radiusB;

            float pitch;
            if (combinedRadiusRange.y > combinedRadiusRange.x)
            {
                float t = Mathf.InverseLerp(combinedRadiusRange.x, combinedRadiusRange.y, combined);
                pitch = Mathf.Lerp(collisionPitchRange.x, collisionPitchRange.y, t);
            }
            else
            {
                pitch = collisionPitchRange.y;
            }

            float volume = RemapVolume(impactSpeed, collisionImpactForMaxVolume, collisionVolumeRange);
            PlayClip(agentCollisionClip, volume, pitch);
            return;
        }

        var otherCollider = collision.collider;
        if (otherCollider == null) return;

        if (IsBumperCollider(otherCollider)) return;

        if (IsWallCollider(otherCollider))
        {
            float volume = RemapVolume(impactSpeed, wallImpactForMaxVolume, wallCollisionVolumeRange);
            PlayClip(wallCollisionClip, volume);
        }
    }

    public void PlaySpawn(Vector2 position)
    {
        PlayClip(spawnClip, spawnVolume);
    }

    public void PlayAbsorb(Vector3 position)
    {
        PlayClip(absorbClip, absorbVolume);
    }

    public void PlayAbsorberBurst(Vector3 position, float delayOverride = -1f, AudioClip clipOverride = null, float volumeOverride = -1f)
    {
        var clip = clipOverride != null ? clipOverride : absorberBurstClip;
        if (clip == null) return;

        float volume = volumeOverride >= 0f ? volumeOverride : absorberBurstVolume;
        float delay = delayOverride >= 0f ? delayOverride : absorberBurstDelay;
        PlayClipDelayed(clip, volume, 1f, delay);
    }

    public void PlayBubbleBurst(Vector3 position)
    {
        PlayClip(bubbleBurstClip, bubbleBurstVolume);
    }

    public void PlayMaterialSound(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        if (materialMap.TryGetValue(key.Trim(), out var entry))
        {
            PlayClip(entry.clip, entry.volume);
        }
    }

    public void PlayBumperHit(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        if (bumperMap.TryGetValue(key.Trim(), out var entry))
        {
            float volume = Mathf.Clamp(entry.volume * volumeScale, 0f, 2f);
            PlayClip(entry.clip, volume, entry.pitch);
        }
    }

    bool IsBumperCollider(Collider2D collider)
    {
        if (collider == null) return false;
        return collider.GetComponentInParent<HeartScape.Interaction.Bumper.HeartBumper2D>() != null;
    }

    public bool IsWallCollider(Collider2D collider)
    {
        if (collider == null) return false;

        if (((1 << collider.gameObject.layer) & wallLayers.value) != 0) return true;

        if (!string.IsNullOrEmpty(wallTag) && collider.CompareTag(wallTag)) return true;

        if (wallNameKeywords != null)
        {
            for (int i = 0; i < wallNameKeywords.Length; i++)
            {
                var keyword = wallNameKeywords[i];
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                if (collider.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        if (collider.GetComponentInParent<FlowWall2D>() != null) return true;

        return false;
    }

    void PlayClip(AudioClip clip, float volume, float pitch = 1f)
    {
        PlayClipDelayed(clip, volume, pitch, 0f);
    }

    void PlayClipDelayed(AudioClip clip, float volume, float pitch, float delay)
    {
        if (clip == null) return;

        if (delay <= 0f)
        {
            PlayClipImmediate(clip, volume, pitch);
        }
        else
        {
            StartCoroutine(PlayClipDelayedRoutine(clip, volume, pitch, delay));
        }
    }

    void PlayClipImmediate(AudioClip clip, float volume, float pitch)
    {
        var source = GetSource();
        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.volume = Mathf.Clamp(volume, 0f, 2f);
        source.PlayOneShot(clip);
    }

    IEnumerator PlayClipDelayedRoutine(AudioClip clip, float volume, float pitch, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayClipImmediate(clip, volume, pitch);
    }

    [Serializable]
    public class BumperClip
    {
        public string key = "default";
        public AudioClip clip;
        [Range(0f, 2f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    [Serializable]
    public class MaterialClip
    {
        public string key = "metal";
        public AudioClip clip;
        [Range(0f, 2f)] public float volume = 0.6f;
    }

    [Serializable]
    public class BgmTrack
    {
        public AudioClip clip;
        [Range(0f, 1.5f)] public float volume = 1f;
        [Tooltip("Gap seconds after this track. Negative uses default gap.")]
        public float gapSeconds = -1f;
    }

    public LayerMask WallLayers => wallLayers;
}
