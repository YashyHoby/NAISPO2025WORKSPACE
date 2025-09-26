using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HeartScape.IO.Gateway;

public class HeartGateway : MonoBehaviour
{
    [Header("References")]
    public HeartManager manager;

    [Header("Biological Organ")]
    [Tooltip("Use biological organ spawning pathway")]
    public bool useBiologicalOrgan = true;

    [Tooltip("Black hole style biological organ gateway")]
    public BlackHoleBiologicalGateway blackHoleBiologicalGateway;

    [Tooltip("Integrated black hole gateway manager")]
    public BlackHoleGatewayManager blackHoleGatewayManager;

    [Header("Emitters (A/B/C/D)")]
    [Tooltip("Spawn positions for switches A/B/C/D. Transform.right is used as launch direction.")]
    public Transform[] emitters = new Transform[4];

    [Header("Launch Parameters")]
    public float baseSpeed = 3f;
    public float speedJitter = 0.5f;
    public float spreadDeg = 5f;

    [Header("Data Source")]
    [Tooltip("When true and UID is supplied, prefer profiles stored in HeartDB.")]
    public bool preferDbIfUid = true;

    [Header("UDP (optional)")]
    public bool useUdp = false;
    public int listenPort = 33333;

    UdpClient udp;
    Thread thread;
    volatile bool running;
    readonly ConcurrentQueue<HeartInput> queue = new();

    [Serializable]
    public class HeartInput
    {
        public string uid;
        public int switchNo; // 1..4 (A=1, B=2, C=3, D=4)
        public float hr;
        public float cv;
        public float range;
        public float mean;
        public float pressTime;
        public float dynRange;
        public float hue; // normalized hue 0..1
        public bool hasHue; // true when hue override is supplied
    }

    void Awake()
    {
        HeartDB.Load();

        if (useBiologicalOrgan)
        {
            if (blackHoleGatewayManager == null)
            {
                blackHoleGatewayManager = GetComponent<BlackHoleGatewayManager>();
                if (blackHoleGatewayManager == null)
                {
                    blackHoleGatewayManager = gameObject.AddComponent<BlackHoleGatewayManager>();
                    Debug.Log("[HeartGateway] BlackHoleGatewayManager component added automatically");
                }
                else
                {
                    Debug.Log("[HeartGateway] BlackHoleGatewayManager component found");
                }
            }

            if (blackHoleBiologicalGateway == null)
            {
                blackHoleBiologicalGateway = GetComponent<BlackHoleBiologicalGateway>();
                if (blackHoleBiologicalGateway == null)
                {
                    blackHoleBiologicalGateway = gameObject.AddComponent<BlackHoleBiologicalGateway>();
                    Debug.Log("[HeartGateway] BlackHoleBiologicalGateway component added automatically");
                }
            }

            Debug.Log($"[HeartGateway] Integrated black hole system enabled - Manager: {blackHoleGatewayManager != null}, Gateway: {blackHoleBiologicalGateway != null}");
        }
        else
        {
            Debug.Log("[HeartGateway] Using traditional spawn system");
        }
    }

    void Start()
    {
        if (useBiologicalOrgan && blackHoleBiologicalGateway == null)
        {
            blackHoleBiologicalGateway = GetComponent<BlackHoleBiologicalGateway>();
            if (blackHoleBiologicalGateway != null)
            {
                Debug.Log("[HeartGateway] BlackHoleBiologicalGateway reference restored in Start");
            }
        }
    }

    void OnEnable()  { if (useUdp) StartUdp(); }
    void OnDisable() { StopUdp(); }
    void OnApplicationQuit() => StopUdp();

    public void Inject(HeartInput msg)
    {
        if (msg != null) queue.Enqueue(msg);
    }

    void Update()
    {
        while (queue.TryDequeue(out var msg))
        {
            if (useBiologicalOrgan && blackHoleBiologicalGateway != null)
            {
                Debug.Log($"[HeartGateway] Using black hole biological organ system for switch {msg.switchNo}");
                blackHoleBiologicalGateway.TriggerSpawn(msg);
            }
            else
            {
                Debug.Log($"[HeartGateway] Using traditional spawn system for switch {msg.switchNo}");
                SpawnFromMessage(msg);
            }
        }
    }

    void SpawnFromMessage(HeartInput m)
    {
        if (manager == null) { Debug.LogWarning("[HeartGateway] HeartManager not set."); return; }
        if (emitters == null || emitters.Length == 0) { Debug.LogWarning("[HeartGateway] Emitters not assigned."); return; }

        int idx = Mathf.Clamp(m.switchNo - 1, 0, emitters.Length - 1);
        var pad = emitters[idx];
        if (pad == null) { Debug.LogWarning($"[HeartGateway] Emitter for switch {m.switchNo} missing."); return; }

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
                : $"UDP_{Guid.NewGuid():N}".Substring(0, 8);

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

        Vector2 pos = pad.position;
        Vector2 dir = pad.right;
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

    public void ApplyInputOverrides(HeartInput input, HeartAgent agent)
    {
        if (input == null || agent == null) return;

        if (input.hasHue)
        {
            var visual = agent.GetComponent<HeartVisual>();
            if (visual != null)
            {
                float _, s, v;
                Color.RGBToHSV(visual.color, out _, out s, out v);
                float hue = Mathf.Repeat(input.hue, 1f);
                visual.color = Color.HSVToRGB(hue, s, v);
                visual.RefreshMaterial();
            }
        }
    }

    void StartUdp()
    {
        try
        {
            udp = new UdpClient(listenPort);
            running = true;
            thread = new Thread(UdpLoop) { IsBackground = true };
            thread.Start();
            Debug.Log($"[HeartGateway] UDP listening on {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HeartGateway] UDP start failed: {e.Message}");
            useUdp = false;
        }
    }

    void StopUdp()
    {
        running = false;
        try { udp?.Close(); } catch { }
        udp = null;
        if (thread != null)
        {
            try { thread.Join(200); } catch { }
            thread = null;
        }
    }

    void UdpLoop()
    {
        var ep = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
        while (running && udp != null)
        {
            try
            {
                var data = udp.Receive(ref ep);
                var json = Encoding.UTF8.GetString(data);
                var msg  = JsonUtility.FromJson<HeartInput>(json);
                if (msg != null) queue.Enqueue(msg);
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception e) { Debug.LogWarning($"[HeartGateway] UDP recv error: {e.Message}"); }
        }
    }

    void OnDrawGizmos()
    {
        if (emitters == null) return;
        for (int i = 0; i < emitters.Length; i++)
        {
            var t = emitters[i];
            if (t == null) continue;
            Gizmos.color = Color.HSVToRGB(i / Mathf.Max(1f, emitters.Length - 1f), 0.7f, 1f);
            Gizmos.DrawWireSphere(t.position, 0.15f);
            var dir = t.right * 0.8f;
            Gizmos.DrawLine(t.position, t.position + dir);
            var left = Quaternion.Euler(0, 0, 150) * dir * 0.25f;
            var right = Quaternion.Euler(0, 0, -150) * dir * 0.25f;
            Gizmos.DrawLine(t.position + dir, t.position + dir + left);
            Gizmos.DrawLine(t.position + dir, t.position + dir + right);
        }
    }
}
