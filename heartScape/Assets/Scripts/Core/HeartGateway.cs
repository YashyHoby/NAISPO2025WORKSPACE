using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class HeartGateway : MonoBehaviour
{
    [Header("Refs")]
    public HeartManager manager;

    [Header("Emitters (A,B,C,D)")]
    [Tooltip("スイッチ A/B/C/D に対応する4点。Transform.right が射出方向。")]
    public Transform[] emitters = new Transform[4];

    [Header("Launch Params")]
    public float baseSpeed = 3f;
    public float speedJitter = 0.5f;
    public float spreadDeg = 5f;

    [Header("Data Source")]
    [Tooltip("UID があれば DB を優先する")]
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
        public int switchNo; // 1..4（A=1,B=2,C=3,D=4）
        public float hr, cv, range, mean;
    }

    void Awake()
    {
        HeartDB.Load();
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
            SpawnFromMessage(msg);
    }

    void SpawnFromMessage(HeartInput m)
    {
        if (manager == null) { Debug.LogWarning("[HeartGateway] HeartManager not set."); return; }
        if (emitters == null || emitters.Length == 0) { Debug.LogWarning("[HeartGateway] Emitters not assigned."); return; }

        int idx = Mathf.Clamp(m.switchNo - 1, 0, emitters.Length - 1);
        var pad = emitters[idx];
        if (pad == null) { Debug.LogWarning($"[HeartGateway] Emitter for switch {m.switchNo} missing."); return; }

        // --- プロファイル決定 ---
        HeartProfile hp = null;

        if (preferDbIfUid && !string.IsNullOrEmpty(m.uid))
        {
            hp = HeartDB.Get(m.uid);
        }
        else
        {
            bool hasMsgValues = !(m.hr == 0f && m.cv == 0f && m.range == 0f && m.mean == 0f);
            if (hasMsgValues)
            {
                hp = new HeartProfile { uid = string.IsNullOrEmpty(m.uid) ? $"UDP_{Guid.NewGuid():N}".Substring(0, 8) : m.uid,
                                        hr = m.hr, cv = m.cv, range = m.range, mean = m.mean };
            }
            else if (!string.IsNullOrEmpty(m.uid))
            {
                hp = HeartDB.Get(m.uid);
            }
            else
            {
                hp = HeartDB.Get(null);
            }
        }

        // 射出位置・向き・初速
        Vector2 pos = pad.position;
        Vector2 dir = pad.right;
        if (spreadDeg > 0f)
        {
            float ang = UnityEngine.Random.Range(-spreadDeg, spreadDeg);
            dir = (Vector2)(Quaternion.Euler(0, 0, ang) * dir);
        }
        float spd = Mathf.Max(0f, baseSpeed + UnityEngine.Random.Range(-speedJitter, speedJitter));
        Vector2 vel = dir.normalized * spd;

        manager.Spawn(hp, pos, vel);
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
        try { udp?.Close(); } catch {}
        udp = null;
        if (thread != null)
        {
            try { thread.Join(200); } catch {}
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
            var right= Quaternion.Euler(0, 0,-150) * dir * 0.25f;
            Gizmos.DrawLine(t.position + dir, t.position + dir + left);
            Gizmos.DrawLine(t.position + dir, t.position + dir + right);
        }
    }
}
