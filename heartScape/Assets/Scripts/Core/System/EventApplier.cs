// Scripts/Core/EventApplier.cs
using UnityEngine;
using System.Collections.Generic;
public class EventApplier : MonoBehaviour
{
    public HeartManager manager;
    public List<EventZone> zones = new();
    void FixedUpdate()
    {
        foreach (var a in manager.agents)
        {
            foreach (var z in zones) if (z.Contains(a.transform.position)) z.Apply(a);
        }
    }
}