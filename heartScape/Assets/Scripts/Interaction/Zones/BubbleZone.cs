using UnityEngine;
using System.Collections.Generic;

public class BubbleZone : EventZone
{
    [Tooltip("謐慕佐謨ｰ縺後％縺ｮ蛟､縺ｫ驕斐☆繧九→遐ｴ陬よｼ泌・繧定｡後＞縺ｾ縺吶")]
    public int threshold = 10;

    [Tooltip("荳ｭ蠢・∈蠑輔″蟇・○繧句鴨縺ｮ蠑ｷ縺包ｼ・orce・峨〒縺吶'")]
    public float pullStrength = 3.5f;

    [Tooltip("蠑輔″蟇・○荳ｭ縺ｫ蜉縺医ｋ霑ｽ蜉貂幃溘・蠑ｷ縺輔〒縺吶・")]
    public float damping = 6.0f;

    // 蜷ｸ縺・ｾｼ縺ｿ譎ゅ↓蟆代＠貂幃溘＆縺帙ｋ菫よ焚・域立: a.vel *= 0.2f・・
    [Range(0f, 1f)] public float initialSlowdown = 0.2f;

    private readonly HashSet<int> captured = new();

    // EventZone 蛛ｴ縺・FixedUpdate 逶ｸ蠖薙〒 Apply 繧貞他繧薙〒縺・ｋ諠ｳ螳・
    public override void Apply(HeartAgent a)
    {
        if (!a) return;
        var rb = a.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // --- 蛻晏屓萓ｵ蜈･縺ｧ謐慕佐迥ｶ諷九∈
        if (!captured.Contains(a.id))
        {
            captured.Add(a.id);
            a.isCaptured = true;
            a.capturedBy = this;

            // 譌ｧ a.vel *= 0.2f -> 迚ｩ逅・溷ｺｦ繧堤峩謗･繧ｹ繧ｱ繝ｼ繝ｫ
            rb.linearVelocity *= initialSlowdown;

            if (captured.Count >= threshold)
                Burst();
        }

        // --- 蜀・Κ縺ｫ逡吶ａ繧具ｼ壻ｸｭ蠢・∈蜷ｸ蠑・+ 霑ｽ蜉繝繝ｳ繝斐Φ繧ｰ
        Vector2 center = transform.position;
        Vector2 toC = center - rb.position;

        // 蜷ｸ蠑包ｼ亥刈邂怜鴨・・
        if (toC.sqrMagnitude > 1e-6f)
        {
            Vector2 pull = toC.normalized * pullStrength;
            rb.AddForce(pull, ForceMode2D.Force);
        }

        // 霑ｽ蜉繝繝ｳ繝斐Φ繧ｰ・磯溷ｺｦ繧貞鴨縺ｧ貂幄｡ｰ縺輔○繧具ｼ・-c*v ・・
        if (damping > 0f)
        {
            Vector2 dampingForce = -rb.linearVelocity * damping;
            rb.AddForce(dampingForce, ForceMode2D.Force);
        }

        // 荳ｭ蠢・ｻ倩ｿ代・菴咲ｽｮ繧貞ｰ代＠縺縺台ｸｭ蠢・∈蟇・○繧具ｼ育黄逅・↓蜆ｪ縺励＞ MovePosition・・
        if (toC.magnitude < radius * 0.3f)
        {
            Vector2 p = Vector2.Lerp(rb.position, center, 0.15f);
            rb.MovePosition(p);
            // 縺ｻ縺ｼ髱呎ｭ｢縺ｫ蟇・○繧・
            rb.linearVelocity *= 0.8f;
        }
    }

    void Burst()
    {
        HeartSoundManager.Instance?.PlayBubbleBurst(transform.position);

        // 謐慕佐隗｣謾ｾ・郁ｻｽ縺丞ｼｾ縺榊・縺呻ｼ唔mpulse・・
        var all = FindObjectsByType<HeartAgent>(FindObjectsSortMode.None);
        foreach (var agent in all)
        {
            if (!agent) continue;
            if (agent.capturedBy != this) continue;

            agent.isCaptured = false;
            agent.capturedBy = null;

            var rb = agent.GetComponent<Rigidbody2D>();
            if (rb)
            {
                Vector2 dir = (rb.position - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 1e-6f) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * 2.0f, ForceMode2D.Impulse);
            }
        }
        captured.Clear();
    }

    public void Release(HeartAgent a)
    {
        if (!a) return;
        if (captured.Remove(a.id))
        {
            a.isCaptured = false;
            a.capturedBy = null;
        }
    }
}
