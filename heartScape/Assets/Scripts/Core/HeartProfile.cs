// Scripts/Core/HeartProfile.cs
[System.Serializable]
public class HeartProfile
{
    public string uid;
    public float hr;    // 心拍（50~120程度想定）
    public float cv;    // 変動係数(0~1)
    public float range; // 最大-最小
    public float mean;  // 平均
}