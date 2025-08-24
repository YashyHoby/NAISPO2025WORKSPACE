using UnityEngine;
using System.Collections.Generic;

public class BubbleZone : EventZone
{
    [Tooltip("捕獲数がこの値に達すると破裂演出")]
    public int threshold = 10;

    [Tooltip("中心へ引き寄せる強さ")]
    public float pullStrength = 3.5f;

    [Tooltip("内部ダンピング（速度減衰）")]
    public float damping = 6.0f;

    private readonly HashSet<int> captured = new();

    public override void Apply(HeartAgent a)
    {
        // 初回侵入で捕獲状態へ
        if (!captured.Contains(a.id))
        {
            captured.Add(a.id);
            a.isCaptured = true;
            a.capturedBy = this;
            a.vel *= 0.2f;

            if (captured.Count >= threshold)
                Burst();
        }

        // 内部に留める：中心へ吸引＋ダンピング
        Vector2 center = transform.position;
        Vector2 toC    = center - (Vector2)a.transform.position;

        if (toC.sqrMagnitude > 1e-6f)
            a.vel += toC.normalized * pullStrength * Time.deltaTime;

        a.vel *= Mathf.Clamp01(1f - damping * Time.deltaTime);

        // 中心近くは位置Lerpで静止に寄せる
        if (toC.magnitude < radius * 0.3f)
            a.transform.position = Vector2.Lerp(a.transform.position, transform.position, 0.15f);
    }

    void Burst()
    {
        AudioHub.PlayBubbleBurst();

        // 捕獲解放（軽く弾き出す）
        foreach (var agent in Object.FindObjectsOfType<HeartAgent>())
        {
            if (agent != null && agent.capturedBy == this)
            {
                agent.isCaptured = false;
                agent.capturedBy = null;
                agent.vel += Random.insideUnitCircle.normalized * 2.0f;
            }
        }
        captured.Clear();
    }

    public void Release(HeartAgent a)
    {
        if (a == null) return;
        if (captured.Remove(a.id))
        {
            a.isCaptured = false;
            a.capturedBy = null;
        }
    }
}
