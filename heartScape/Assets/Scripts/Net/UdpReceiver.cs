// Scripts/Net/UdpReceiver.cs
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public class UdpReceiver : MonoBehaviour
{
    UdpClient client; public int listenPort = 9000;
    public HeartManager manager;

    void Start()
    {
        HeartDB.Load();
        client = new UdpClient(listenPort);
        client.BeginReceive(OnRecv, null);
    }
    void OnRecv(IAsyncResult ar)
    {
        IPEndPoint ep = new(IPAddress.Any, 0);
        var data = client.EndReceive(ar, ref ep);
        var msg = Encoding.UTF8.GetString(data);
        UnityMainThreadDispatcher.Enqueue(() => Handle(msg));
        client.BeginReceive(OnRecv, null);
    }
    void Handle(string msg)
    {
        var d = Parse(msg);
        string uid = d.GetValueOrDefault("uid", "UNK");
        float speed = float.Parse(d.GetValueOrDefault("speed", "0.5"));
        var hp = HeartDB.Get(uid);
        var pos = new Vector2(-9f, UnityEngine.Random.Range(-3f, 3f));
        var v = new Vector2(Mathf.Lerp(1f, 4f, Mathf.Clamp01(speed)), 0f);
        manager.Spawn(hp, pos, v);
        LedSender.SendPacket(hp, speed);
    }
    Dictionary<string, string> Parse(string m)
    {
        var dd = new Dictionary<string, string>();
        foreach (var part in m.Split(','))
        {
            var kv = part.Split(':');
            if (kv.Length == 2) dd[kv[0]] = kv[1];
        }
        return dd;
    }
}
