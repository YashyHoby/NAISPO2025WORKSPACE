// Scripts/Net/LedSender.cs
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public static class LedSender
{
    static UdpClient udp = new UdpClient();
    static IPEndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.4.50"), 21324); // WLED IP Ç…ïœçX
    public static void SendPacket(HeartProfile hp, float speed)
    {
        var c = Color.HSVToRGB(Mathf.InverseLerp(50, 120, hp.hr), 0.8f, 1f);
        byte r = (byte)(c.r * 255), g = (byte)(c.g * 255), b = (byte)(c.b * 255);
        int n = Mathf.RoundToInt(Mathf.Lerp(10, 40, Mathf.Clamp01(speed)));
        List<byte> buf = new();
        buf.AddRange(new byte[] { 0x01, 0x02, 0x03, r, g, b }); // ÉwÉbÉ_ïó
        for (int i = 0; i < n; i++) buf.AddRange(new byte[] { r, g, b });
        buf.Add((byte)((r + g + b + n) & 0xFF)); // ä»à’CRCïó
        udp.Send(buf.ToArray(), buf.Count, ep);
    }
}
