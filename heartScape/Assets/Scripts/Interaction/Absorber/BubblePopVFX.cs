using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class BubblePopVFX : MonoBehaviour
{
    public BubbleVisual2D bubble; // 同一バブル参照
    public AnimationCurve speedOverLife = AnimationCurve.EaseInOut(0,1,1,0);
    public int burstCount = 600;
    public float startSpeed = 6f;
    public float lifeTime = 1.6f;
    public float size = 0.03f;

    ParticleSystem ps;

    void Awake(){ ps = GetComponent<ParticleSystem>(); SetupIfNeeded(); }

    void SetupIfNeeded()
    {
        var main = ps.main;
        main.loop = false;
        main.startLifetime = lifeTime;
        main.startSpeed = startSpeed;
        main.startSize = size;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
    }

    public void PlayPop()
    {
        var palette = bubble? bubble.GetPalette() : new List<Color>{ Color.white };
        if (palette.Count == 0) palette.Add(Color.white);

        for (int i=0; i<burstCount; i++)
        {
            var p = new ParticleSystem.EmitParams();
            // 放射方向
            Vector2 dir = Random.insideUnitCircle.normalized;
            p.velocity = new Vector3(dir.x, dir.y, 0f) * (startSpeed * Random.Range(0.7f, 1.3f));
            p.startLifetime = lifeTime * Random.Range(0.7f,1.1f);
            p.startSize = size * Random.Range(0.6f,1.4f);
            // パレットからランダム選択
            p.startColor = palette[Random.Range(0, Mathf.Min(4, palette.Count))];
            ps.Emit(p, 1);
        }
        ps.Play();
    }
}
