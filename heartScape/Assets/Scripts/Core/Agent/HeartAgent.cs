using UnityEngine;

public class HeartAgent : MonoBehaviour
{
    [Header("Identity / Profile")]
    public int id;
    public HeartProfile profile;

    [Header("Capture State")]
    [HideInInspector] public bool       isCaptured = false;
    [HideInInspector] public BubbleZone capturedBy = null;

    void Awake()
    {
        // ID 未設定なら割り当て
        if (id == 0) id = Random.Range(int.MinValue, int.MaxValue);
    }
}
