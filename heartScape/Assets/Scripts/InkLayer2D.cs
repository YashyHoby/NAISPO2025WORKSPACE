using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class InkLayer2D : MonoBehaviour
{
    public SpriteRenderer sr;
    [Header("Flow")]
    public float speed = 0.15f;
    public Vector2 flowDir = new Vector2(1, 0);
    public float noiseScale = 2f;
    [Range(0f,1f)] public float threshold = 0.5f;
    [Range(0.001f,0.2f)] public float edgeSoft = 0.06f;
    [Range(0f,1f)] public float maxAlpha = 0.9f;

    MaterialPropertyBlock mpb;
    Vector2 offset;

    void Awake(){
        if (!sr) sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        Apply();
    }

    void LateUpdate(){
        offset += flowDir.normalized * speed * Time.deltaTime;
        Apply();
    }

    void Apply(){
        if (!sr) return;
        sr.GetPropertyBlock(mpb);
        mpb.SetVector("_FlowDir", new Vector4(flowDir.x, flowDir.y, 0, 0));
        mpb.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
        mpb.SetFloat("_NoiseScale", noiseScale);
        mpb.SetFloat("_Threshold", threshold);
        mpb.SetFloat("_EdgeSoft", edgeSoft);
        mpb.SetFloat("_Alpha", maxAlpha);
        sr.SetPropertyBlock(mpb);
    }

    public void Init(Sprite maskSprite, Material inkMat, Color c, Vector2 dir, float seed){
        if (!sr) sr = GetComponent<SpriteRenderer>();
        sr.sprite = maskSprite;
        sr.sharedMaterial = inkMat;
        flowDir = dir.normalized;
        offset = new Vector2(seed, -seed * 0.73f);
        if (mpb == null) mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetColor("_InkColor", c);
        mpb.SetVector("_FlowDir", new Vector4(flowDir.x, flowDir.y, 0, 0));
        mpb.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
        sr.SetPropertyBlock(mpb);
    }
}
