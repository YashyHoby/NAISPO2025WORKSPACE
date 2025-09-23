using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D), typeof(Renderer))]
public class Jellyfy : MonoBehaviour
{
    private static readonly int VertexTex = Shader.PropertyToID("_VertexTex");
    private static readonly int VertexCount = Shader.PropertyToID("_VertexCount");

    private const int MAX_VERTICES = 64; // Must match shader in Jelly2D.shader

    private PolygonCollider2D _polygonCollider;
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    
    private Texture2D _vertexTexture;
    private Color[] _textureColorData = new Color[MAX_VERTICES];

    void Awake()
    {
        _polygonCollider = GetComponent<PolygonCollider2D>();
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        _vertexTexture = new Texture2D(MAX_VERTICES, 1, TextureFormat.RGFloat, false);
        _vertexTexture.filterMode = FilterMode.Point;
        _vertexTexture.wrapMode = TextureWrapMode.Clamp;
    }

    void OnDestroy()
    {
        if (_vertexTexture != null) Destroy(_vertexTexture);
    }

    // Use LateUpdate to ensure this runs after HeartVisual has updated the collider
    void LateUpdate()
    {
        // HeartVisual updates the collider path, we just read it
        if (_polygonCollider.pathCount == 0) return; // Add this check to prevent error

        var points = _polygonCollider.GetPath(0);
        if (points == null || points.Length == 0) return;

        int pointCount = Mathf.Min(points.Length, MAX_VERTICES);

        // Convert local points to world space for the shader
        for (int i = 0; i < pointCount; i++)
        {
            Vector2 worldPoint = transform.TransformPoint(points[i]);
            _textureColorData[i] = new Color(worldPoint.x, worldPoint.y, 0, 0);
        }
        // Clear unused part of the texture
        for (int i = pointCount; i < MAX_VERTICES; i++) _textureColorData[i] = Color.clear;

        _vertexTexture.SetPixels(_textureColorData);
        _vertexTexture.Apply(false);

        // Set shader properties using a MaterialPropertyBlock.
        // This adds data to the existing material without replacing it.
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(VertexTex, _vertexTexture);
        _propBlock.SetInt(VertexCount, pointCount);
        _renderer.SetPropertyBlock(_propBlock);
    }
}
