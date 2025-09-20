// ReflectiveBoundary2D.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ReflectiveBoundary2D : MonoBehaviour
{
    [Tooltip("壁コライダーの厚み（ワールド単位）です。")]
    public float thickness = 1f;

    [Tooltip("跳ね返りに使用する PhysicsMaterial。Bounciness=1、Friction=0 を推奨します。")]
    public PhysicsMaterial2D bounceMaterial;

    [Tooltip("Gizmos で描画する枠線の色です。")]
    public Color gizmoColor = new(1,1,1,0.35f);

    BoxCollider2D top, bottom, left, right;
    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Ensure(ref top,    "Wall_Top");
        Ensure(ref bottom, "Wall_Bottom");
        Ensure(ref left,   "Wall_Left");
        Ensure(ref right,  "Wall_Right");
        UpdateWalls();
    }

    void Ensure(ref BoxCollider2D col, string name)
    {
        Transform tr = transform.Find(name);
        if (!tr)
        {
            tr = new GameObject(name).transform;
            tr.SetParent(transform, false);
        }

        // 既存 BoxCollider2D を取得、なければ新規追加
        col = tr.GetComponent<BoxCollider2D>();
        if (col == null)
            col = tr.gameObject.AddComponent<BoxCollider2D>();

        col.sharedMaterial = bounceMaterial;
        col.isTrigger = false;
        col.edgeRadius = 0f;
    }


    void LateUpdate() => UpdateWalls();

    void UpdateWalls()
    {
        if (!cam) return;

        // カメラの表示領域をワールド座標で取得（Orthographic前提）
        var min = cam.ViewportToWorldPoint(new Vector3(0,0, 0));
        var max = cam.ViewportToWorldPoint(new Vector3(1,1, 0));
        float w = Mathf.Abs(max.x - min.x);
        float h = Mathf.Abs(max.y - min.y);

        // 位置
        top.transform.position    = new Vector3((min.x+max.x)/2f, max.y + thickness/2f, 0);
        bottom.transform.position = new Vector3((min.x+max.x)/2f, min.y - thickness/2f, 0);
        left.transform.position   = new Vector3(min.x - thickness/2f, (min.y+max.y)/2f, 0);
        right.transform.position  = new Vector3(max.x + thickness/2f, (min.y+max.y)/2f, 0);

        // サイズ
        top.size    = new Vector2(w + thickness*2f, thickness);
        bottom.size = new Vector2(w + thickness*2f, thickness);
        left.size   = new Vector2(thickness, h + thickness*2f);
        right.size  = new Vector2(thickness, h + thickness*2f);
    }

    void OnDrawGizmos()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) return;
        var min = cam.ViewportToWorldPoint(new Vector3(0,0, 0));
        var max = cam.ViewportToWorldPoint(new Vector3(1,1, 0));
        Gizmos.color = gizmoColor;
        Vector3 p0 = new(min.x, min.y, 0), p1 = new(max.x, min.y, 0),
                p2 = new(max.x, max.y, 0), p3 = new(min.x, max.y, 0);
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
    }
}
