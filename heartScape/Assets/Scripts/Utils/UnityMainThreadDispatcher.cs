// Scripts/Utils/MainThreadDispatcher.cs
using UnityEngine;
using System;
using System.Collections.Generic;
public class UnityMainThreadDispatcher : MonoBehaviour
{
    static Queue<Action> q = new(); public static void Enqueue(Action a) { lock (q) q.Enqueue(a); }
    void Update() { lock (q) while (q.Count > 0) q.Dequeue().Invoke(); }
}
