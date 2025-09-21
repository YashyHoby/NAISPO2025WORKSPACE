using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class BubblePopVFX : MonoBehaviour
{
    [Tooltip("吸収泡の液体サイト。カラー抽出に使用します。")]
    public BubbleLiquidSites2D liquidSites;

    [Tooltip("互換用：旧 BubbleVisual2D を参照する場合に設定します。")]
    public BubbleVisual2D legacyBubbleVisual;

    public AnimationCurve speedOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public int burstCount = 600;
    public float startSpeed = 6f;
    public float lifeTime = 1.6f;
    public float size = 0.03f;

    ParticleSystem ps;
    readonly List<Color> paletteBuffer = new();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        SetupIfNeeded();
    }

    void SetupIfNeeded()
    {
        var main = ps.main;
        main.loop = false;
        main.startLifetime = lifeTime;
        main.startSpeed = startSpeed;
        main.startSize = size;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
    }

    void GatherPalette()
    {
        paletteBuffer.Clear();

        if (liquidSites != null)
            liquidSites.GetPalette(paletteBuffer);

        if (paletteBuffer.Count == 0 && legacyBubbleVisual != null)
            paletteBuffer.AddRange(legacyBubbleVisual.GetPalette());

        if (paletteBuffer.Count == 0)
            paletteBuffer.Add(Color.white);
    }

    public void PlayPop()
    {
        GatherPalette();

        for (int i = 0; i < burstCount; i++)
        {
            var emit = new ParticleSystem.EmitParams();
            Vector2 dir = Random.insideUnitCircle.normalized;
            emit.velocity = new Vector3(dir.x, dir.y, 0f) * (startSpeed * Random.Range(0.7f, 1.3f));
            emit.startLifetime = lifeTime * Random.Range(0.7f, 1.1f);
            emit.startSize = size * Random.Range(0.6f, 1.4f);
            emit.startColor = paletteBuffer[Random.Range(0, paletteBuffer.Count)];
            ps.Emit(emit, 1);
        }
        ps.Play();
    }
}
