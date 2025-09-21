// Scripts/Core/EventApplier.cs
using UnityEngine;
using System.Collections.Generic;

public class EventApplier : MonoBehaviour
{
    public HeartManager manager;
    public List<EventZone> zones = new List<EventZone>();

    void FixedUpdate()
    {
        if (manager == null || manager.agents == null) return;

        var agents = manager.agents;
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            var agent = agents[i];
            if (agent == null)
            {
                agents.RemoveAt(i);
                continue;
            }

            Vector3 pos;
            try
            {
                pos = agent.transform.position;
            }
            catch (MissingReferenceException)
            {
                agents.RemoveAt(i);
                continue;
            }

            for (int z = 0; z < zones.Count; z++)
            {
                var zone = zones[z];
                if (zone == null) continue;
                if (zone.Contains(pos))
                {
                    zone.Apply(agent);
                }
            }
        }
    }
}
