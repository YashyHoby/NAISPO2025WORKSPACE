// Scripts/Core/HeartAgent.cs
using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

public class HeartAgent : MonoBehaviour
{
    public int id;
    public ShapeType shapeType;
    public float radius;
    public Vector2 vel;
    public float energy;
    public Color color;
    public HeartProfile profile;
    public Material mat;   // SDF風マテリアル

    void Update()
    {
        transform.position += (Vector3)(vel * Time.deltaTime);

        // 壁バウンス（±X:8, ±Y:4.5 に仮定）
        var p = transform.position;
        if (Mathf.Abs(p.x) > 8f) { vel.x *= -1; p.x = Mathf.Sign(p.x) * 8f; }
        if (Mathf.Abs(p.y) > 4.5f) { vel.y *= -1; p.y = Mathf.Sign(p.y) * 4.5f; }
        transform.position = p;

        // 拍動に応じた発光
        float bpm01 = Mathf.InverseLerp(50f, 120f, profile.hr);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse = (Mathf.Sin(Time.time * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        // シェーダへ
        mat.SetFloat("_Pulse", pulse);
        mat.SetFloat("_Radius", radius);
        mat.SetColor("_Tint", color);
        mat.SetFloat("_ShapeType", (float)shapeType);
    }
}
