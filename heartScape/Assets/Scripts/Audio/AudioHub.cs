// Scripts/Audio/AudioHub.cs
using UnityEngine;
using System.Collections.Generic;

public class AudioHub : MonoBehaviour
{
    public static AudioHub I;
    public AudioSource srcOneShot;
    public AudioClip hitSoft, hitHard, bubbleBurst;
    public AudioClip metal, wood, membrane, water;

    Dictionary<string, AudioClip> map;

    void Awake() { I = this; map = new() { { "metal", metal }, { "wood", wood }, { "membrane", membrane }, { "water", water } }; }

    public static void PlayHit(HeartAgent a, HeartAgent b, float rel)
    {
        if (I == null) return;
        var clip = (Mathf.Abs(rel) < 0.8f) ? I.hitSoft : I.hitHard;
        I.srcOneShot.PlayOneShot(clip, Mathf.Clamp01(Mathf.Abs(rel) * 0.2f));
    }
    public static void PlayBubbleBurst() { if (I == null) return; I.srcOneShot.PlayOneShot(I.bubbleBurst, 0.8f); }
    public static void PlayMaterial(string key) { if (I == null) return; if (I.map.TryGetValue(key, out var c)) I.srcOneShot.PlayOneShot(c, 0.6f); }
}
