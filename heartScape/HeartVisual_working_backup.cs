using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer)), RequireComponent(typeof(MeshFilter))]
public class HeartVisual : MonoBehaviour
{
    [Header("髫穂ｹ昶螺騾ｶ・ｮ髫ｪ・ｭ陞ｳ繝ｻ")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    public float     radius    = 0.6f;

    [Header("陷ｿ繧峨・繝ｻ莠包ｽｻ・ｻ隲｢謫ｾ・ｼ繝ｻ")]
    public HeartProfile profile;
    public HeartAgent   agent;

    [Header("郢晄ｧｭ繝ｦ郢晢ｽｪ郢ｧ・｢郢晢ｽｫ髫ｪ・ｭ陞ｳ繝ｻ")]
    public Material baseMaterial;

    [Header("郢ｧ・ｼ郢晢ｽｪ郢晢ｽｼ髯ｦ・ｨ霑ｴ・ｾ")]
    [Range(0f, 0.5f)] public float smoothness = 0.1f;
    [Range(0f, 0.1f)] public float outlineWidth = 0.01f;
    public Color outlineColor = Color.black;
    [Range(0f, 1f)] public float refraction = 0.1f;
    public Color specularColor = new Color(1,1,1,0.5f);
    [Range(1f, 100f)] public float shininess = 20f;
    public Color fresnelColor = new Color(1,1,1,0.1f);
    [Range(0.1f, 10f)] public float fresnelPower = 2.0f;

    [Header("Global Color Adjustments")]
    [Range(0.0f, 2.0f)] public float brightnessMultiplier = 1.0f;
    [Range(0.0f, 2.0f)] public float saturationMultiplier = 1.0f;
    [Range(-1.0f, 1.0f)] public float hueShift = 0.0f;
    [Range(0.0f, 1.5f)] public float alphaMultiplier = 1.0f;


    Material        mat;
    Renderer        rend;
    MeshFilter      meshFilter;
    Mesh            meshInstance;
    CircleCollider2D circleCollider;
    PolygonCollider2D polygonCollider;

    const int MAX_VERTICES = 64; // Must match shader
    Texture2D _vertexTexture;
    Color[]   _textureColorData = new Color[MAX_VERTICES];

    Vector3 initialScale;
    float   initialColliderRadius;
    float   initialWorldRadius;
    bool    initialized;

    Vector2[] polygonColliderPath = Array.Empty<Vector2>();
    Vector2[] circleColliderPath = Array.Empty<Vector2>();

    readonly List<Vector2> vertexScratch = new List<Vector2>(16);

    int  lastAppliedVertexCount = -1;

    static readonly int[] VertexCycle =
    {
        0, 3, 4, 5, 6, 7, 8, 9, 10,
        0, 3, 4, 5, 6, 7, 8, 9, 10,
        0, 3, 4, 5, 6, 7, 8, 9, 10
    };

    const int CircleSegments = 32;

    public struct ProceduralShapeParameters { public float normalizedHr, normalizedCv, normalizedRange, normalizedMean, shapeSelector, irregularity, orientation, seed; }

    void Awake() { Initialize(); }
    void OnEnable() { Initialize(); ApplyRadiusScale(radius); }

    void Initialize()
    {
        if (initialized) return;
        if (agent == null) agent = GetComponent<HeartAgent>();
        rend = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        circleCollider = GetComponent<CircleCollider2D>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null) polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        if (polygonCollider != null) { polygonCollider.isTrigger = circleCollider != null && circleCollider.isTrigger; polygonCollider.pathCount = 0; }
        if (circleCollider != null && polygonCollider != null) circleCollider.enabled = false;
        polygonColliderPath = Array.Empty<Vector2>();
        circleColliderPath = Array.Empty<Vector2>();
        initialScale = transform.localScale;
        const float minRadius = 0.0001f;
        float colliderLocalRadius = (circleCollider != null && circleCollider.radius > minRadius) ? circleCollider.radius : Mathf.Max(radius, minRadius);
        initialColliderRadius = colliderLocalRadius;
        float baseScale = Mathf.Max(initialScale.x, minRadius);
        float worldRadius = colliderLocalRadius * baseScale;
        if (worldRadius <= minRadius) worldRadius = Mathf.Max(radius, 0.1f);
        initialWorldRadius = worldRadius;
        var src = baseMaterial != null ? baseMaterial : (rend != null ? rend.sharedMaterial : null);
        if (src != null) { mat = new Material(src); if (rend != null) rend.material = mat; }
        else { Debug.LogWarning("[HeartVisual] Source material not found.", this); }
        EnsureMesh();
        initialized = true;
        _vertexTexture = new Texture2D(MAX_VERTICES, 1, TextureFormat.RGFloat, false);
        _vertexTexture.filterMode = FilterMode.Point;
        _vertexTexture.wrapMode = TextureWrapMode.Clamp;
    }

    void EnsureMesh()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        if (meshInstance == null)
        {
            meshInstance = new Mesh
            {
                name = $"{name}_HeartVisualMesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            meshInstance.MarkDynamic();
        }

        if (meshFilter.sharedMesh != meshInstance)
        {
            meshFilter.sharedMesh = meshInstance;
        }
    }

    void Update() { RefreshMaterial(Time.time); }
    public void RefreshMaterial() { RefreshMaterial(Time.time); }
    public void RefreshMaterial(float timeSeconds) { ApplyMaterialState(timeSeconds); }

    void ApplyMaterialState(float timeSeconds)
    {
        Initialize();
        float bpm = (profile != null) ? profile.hr : 70f;
        float bpm01 = Mathf.InverseLerp(50f, 120f, bpm);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse = (Mathf.Sin(timeSeconds * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;
        if (mat == null) return;
        mat.SetFloat("_Pulse", pulse);
        mat.SetFloat("_Radius", radius);
        var adjustedColor = ApplyColorAdjustments(color);
        mat.SetColor("_Tint", adjustedColor);
        mat.SetFloat("_ShapeType", (float)shapeType);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_OutlineWidth", outlineWidth);
        mat.SetColor("_OutlineColor", outlineColor);
        mat.SetFloat("_Refraction", refraction);
        mat.SetColor("_SpecularColor", specularColor);
        mat.SetFloat("_Shininess", shininess);
        mat.SetColor("_FresnelColor", fresnelColor);
        mat.SetFloat("_FresnelPower", fresnelPower);
    }

    public int ApplyProceduralShape(ProceduralShapeParameters parameters)
    {
        Initialize();
        EnsureMesh();
        if (meshInstance == null) return 0;
        int vertexCount = DetermineVertexCount(parameters);
        if (vertexCount <= 0)
        {
            BuildCircleMesh(parameters);
            if (mat != null) mat.SetFloat("_IsCircle", 1.0f);
            lastAppliedVertexCount = 0;
            return 0;
        }
        BuildPolygonMesh(vertexCount, parameters);
        if (mat != null) mat.SetFloat("_IsCircle", 0.0f);
        lastAppliedVertexCount = vertexCount;
        return vertexCount;
    }

    int DetermineVertexCount(ProceduralShapeParameters p)
    {
        if (shapeType == ShapeType.Circle) return 0;
        if (shapeType == ShapeType.Triangle) return 3;
        if (shapeType == ShapeType.Box)
        {
            int extra = Mathf.RoundToInt(Mathf.Lerp(0f, 4f, p.irregularity));
            return Mathf.Clamp(4 + extra, 4, 8);
        }

        float selector = Mathf.Clamp01(p.shapeSelector);
        int index = Mathf.RoundToInt(selector * (VertexCycle.Length - 1));
        int count = VertexCycle[Mathf.Clamp(index, 0, VertexCycle.Length - 1)];
        return Mathf.Clamp(count, 3, 10);
    }

    void BuildCircleMesh(ProceduralShapeParameters p)
    {
        meshInstance.Clear();
        var vertices = new Vector3[CircleSegments + 1];
        var uvs = new Vector2[CircleSegments + 1];
        var tris = new int[CircleSegments * 3];
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        float baseRadius = 0.5f;
        float seedBase = p.seed * 17.0f;
        vertexScratch.Clear();
        for (int i = 0; i < CircleSegments; i++)
        {
            float t = i / (float)CircleSegments;
            float angle = t * Mathf.PI * 2f;
            float jitter = (Hash(seedBase + i * 0.73f) - 0.5f) * Mathf.Lerp(0.0f, 0.04f, p.irregularity);
            float radiusOffset = Mathf.Clamp(baseRadius - jitter, 0.42f, 0.5f);
            float x = Mathf.Cos(angle) * radiusOffset;
            float y = Mathf.Sin(angle) * radiusOffset;
            var v = new Vector2(x, y);
            vertexScratch.Add(v);
            vertices[i + 1] = v;
            uvs[i + 1] = new Vector2(x + 0.5f, y + 0.5f);
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i == CircleSegments - 1) ? 1 : i + 2;
        }
        meshInstance.vertices = vertices;
        meshInstance.uv = uvs;
        meshInstance.triangles = tris;
        meshInstance.RecalculateBounds();
        meshInstance.RecalculateNormals();
        UpdatePolygonColliderCircle(vertices);
        UpdateJellyShaderProperties(vertexScratch);
    }

    void BuildPolygonMesh(int vertexCount, ProceduralShapeParameters p)
    {
        meshInstance.Clear();
        GeneratePolygonVertices(vertexCount, p);
        int count = vertexScratch.Count;
        var vertices = new Vector3[count + 1];
        var uvs = new Vector2[count + 1];
        var tris = new int[count * 3];
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = vertexScratch[i];
            vertices[i + 1] = new Vector3(v.x, v.y, 0f);
            uvs[i + 1] = new Vector2(v.x + 0.5f, v.y + 0.5f);
        }
        for (int i = 0; i < count; i++)
        {
            int triIndex = i * 3;
            int b = i + 1;
            int c = (i + 1) % count + 1;
            tris[triIndex] = 0;
            tris[triIndex + 1] = b;
            tris[triIndex + 2] = c;
        }
        meshInstance.vertices = vertices;
        meshInstance.uv = uvs;
        meshInstance.triangles = tris;
        meshInstance.RecalculateBounds();
        meshInstance.RecalculateNormals();
        UpdatePolygonCollider(vertexScratch);
        UpdateJellyShaderProperties(vertexScratch);
    }

    void UpdatePolygonColliderCircle(Vector3[] vertices)
    {
        if (polygonCollider == null) return;
        if (vertices == null || vertices.Length <= 1)
        {
            polygonCollider.pathCount = 0;
            return;
        }

        int count = vertices.Length - 1;
        if (count < 3)
        {
            polygonCollider.pathCount = 0;
            return;
        }

        var points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 v = vertices[i + 1];
            points[i] = new Vector2(v.x, v.y);
        }

        polygonCollider.enabled = true;
        polygonCollider.pathCount = 1;
        polygonCollider.SetPath(0, points);
        polygonColliderPath = points;
        circleColliderPath = points;
        if (circleCollider != null) circleCollider.enabled = false;
    }
    void UpdatePolygonCollider(List<Vector2> points)
    {
        if (polygonCollider == null) return;

        if (points == null || points.Count < 3)
        {
            polygonCollider.pathCount = 0;
            if (circleCollider != null)
            {
                circleCollider.enabled = true;
                circleCollider.radius = Mathf.Max(circleCollider.radius, 0.01f);
            }
            return;
        }

        var path = points.ToArray();
        polygonCollider.enabled = true;
        polygonCollider.pathCount = 1;
        polygonCollider.SetPath(0, path);
        polygonColliderPath = path;
        if (circleCollider != null) circleCollider.enabled = false;
    }
    void GeneratePolygonVertices(int vertexCount, ProceduralShapeParameters p)
    {
        vertexScratch.Clear();

        if (shapeType == ShapeType.Triangle)
        {
            GenerateTriangleVertices(Mathf.Max(vertexCount, 3), p);
        }
        else if (shapeType == ShapeType.Box)
        {
            GenerateBoxVertices(Mathf.Max(vertexCount, 4), p);
        }
        else
        {
            GenerateIrregularPolygon(Mathf.Max(vertexCount, 3), p);
        }

        ApplyPostProcess(vertexScratch, p);
        EnsureCounterClockwise(vertexScratch);
    }

    void GenerateBoxVertices(int vertexCount, ProceduralShapeParameters p)
    {
        float angle = p.orientation * Mathf.PI * 2f;
        Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 up = new Vector2(-right.y, right.x);

        float halfWidth = Mathf.Lerp(0.3f, 0.5f, p.normalizedRange);
        float halfHeight = Mathf.Lerp(0.3f, 0.5f, p.normalizedMean);

        int[] counts = new int[4];
        AllocateEdgeCounts(vertexCount, p, counts);
        AppendEdgeVertices(counts, halfWidth, halfHeight, p);
    }

    void GenerateIrregularPolygon(int vertexCount, ProceduralShapeParameters p)
    {
        float angleOffset = p.orientation * Mathf.PI * 2f;
        float irregularity = Mathf.Lerp(0f, 0.45f, p.irregularity);
        float baseRadius = Mathf.Lerp(0.32f, 0.48f, p.normalizedMean);
        float rangeScale = Mathf.Lerp(0.85f, 1.1f, p.normalizedRange);

        for (int i = 0; i < vertexCount; i++)
        {
            float t = i / (float)vertexCount;
            float angle = angleOffset + t * Mathf.PI * 2f;
            float noise = (Hash(p.seed + i * 0.73f) - 0.5f) * irregularity;
            float radius = Mathf.Clamp(baseRadius * rangeScale + noise, 0.18f, 0.5f);
            vertexScratch.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    void GenerateTriangleVertices(int count, ProceduralShapeParameters p)
    {
        int actualCount = Mathf.Max(3, count);
        float angleOffset = p.orientation * Mathf.PI * 2f;
        float irregularity = Mathf.Lerp(0f, 0.25f, p.irregularity);

        for (int i = 0; i < actualCount; i++)
        {
            float angle = angleOffset + i * Mathf.PI * 2f / actualCount;
            float radius = Mathf.Clamp(0.45f + (Hash(p.seed + i * 0.67f) - 0.5f) * irregularity, 0.2f, 0.5f);
            vertexScratch.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    void AllocateEdgeCounts(int vertexCount, ProceduralShapeParameters p, int[] counts)
    {
        if (counts == null || counts.Length == 0) return;
        Array.Clear(counts, 0, counts.Length);

        int extra = Mathf.Max(vertexCount - 4, 0);
        for (int i = 0; i < extra; i++)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(Hash(p.seed + i * 0.19f) * counts.Length), 0, counts.Length - 1);
            counts[index]++;
        }
    }

    void AppendEdgeVertices(int[] counts, float halfWidth, float halfHeight, ProceduralShapeParameters p)
    {
        vertexScratch.Clear();

        float angle = p.orientation * Mathf.PI * 2f;
        Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 up = new Vector2(-right.y, right.x);

        Vector2[] corners =
        {
            -right * halfWidth - up * halfHeight,
            right * halfWidth - up * halfHeight,
            right * halfWidth + up * halfHeight,
            -right * halfWidth + up * halfHeight
        };

        for (int edge = 0; edge < 4; edge++)
        {
            Vector2 start = corners[edge];
            Vector2 end = corners[(edge + 1) % 4];
            int extras = (counts != null && edge < counts.Length) ? counts[edge] : 0;
            AppendEdge(vertexScratch, extras, start, end, p.seed + edge * 0.61f, Mathf.Lerp(0f, 0.35f, p.irregularity));
        }

        if (vertexScratch.Count > 1 && (vertexScratch[vertexScratch.Count - 1] - vertexScratch[0]).sqrMagnitude < 1e-6f)
        {
            vertexScratch.RemoveAt(vertexScratch.Count - 1);
        }
    }

    void AppendEdge(List<Vector2> buffer, int count, Vector2 start, Vector2 end, float seed, float irregularity)
    {
        if (buffer.Count == 0)
        {
            buffer.Add(start);
        }
        else if ((buffer[buffer.Count - 1] - start).sqrMagnitude > 1e-6f)
        {
            buffer.Add(start);
        }

        Vector2 edge = end - start;
        Vector2 normal = edge.sqrMagnitude > 0f ? new Vector2(-edge.y, edge.x).normalized : Vector2.zero;

        for (int i = 1; i <= count; i++)
        {
            float t = i / (float)(count + 1);
            Vector2 point = start + edge * t;
            float offset = (Hash(seed + i * 0.917f) - 0.5f) * irregularity;
            point += normal * offset;
            buffer.Add(point);
        }

        buffer.Add(end);
    }

    void ApplyPostProcess(List<Vector2> buffer, ProceduralShapeParameters p)
    {
        if (buffer == null || buffer.Count < 3) return;
        float smoothing = Mathf.Lerp(0f, 0.5f, p.irregularity * 0.6f + p.normalizedCv * 0.4f);
        if (smoothing <= 0f) return;

        var temp = new Vector2[buffer.Count];
        for (int i = 0; i < buffer.Count; i++)
        {
            Vector2 prev = buffer[(i - 1 + buffer.Count) % buffer.Count];
            Vector2 current = buffer[i];
            Vector2 next = buffer[(i + 1) % buffer.Count];
            Vector2 average = (prev + current + next) / 3f;
            temp[i] = Vector2.Lerp(current, average, smoothing);
        }

        for (int i = 0; i < buffer.Count; i++)
        {
            buffer[i] = temp[i];
        }
    }

    void EnsureCounterClockwise(List<Vector2> buffer)
    {
        if (buffer == null || buffer.Count < 3) return;

        float area = 0f;
        for (int i = 0; i < buffer.Count; i++)
        {
            Vector2 a = buffer[i];
            Vector2 b = buffer[(i + 1) % buffer.Count];
            area += (a.x * b.y) - (b.x * a.y);
        }

        if (area < 0f)
        {
            buffer.Reverse();
        }
    }

    void OnDestroy()
    {
        if (mat != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(mat);
#else
            Destroy(mat);
#endif
        }

        if (meshInstance != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(meshInstance);
#else
            Destroy(meshInstance);
#endif
        }

        if (_vertexTexture != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(_vertexTexture);
#else
            Destroy(_vertexTexture);
#endif
        }
    }

    public void SetRadius(float r)
    {
        float target = Mathf.Max(0.01f, r);
        radius = target;
        ApplyRadiusScale(target);
        RefreshMaterial();
    }
    void ApplyRadiusScale(float targetRadius)
    {
        Initialize();
        float clamped = Mathf.Max(0.01f, targetRadius);
        radius = clamped;

        float reference = Mathf.Max(initialWorldRadius, 0.0001f);
        float scale = clamped / reference;

        transform.localScale = new Vector3(initialScale.x * scale, initialScale.y * scale, initialScale.z);

        if (circleCollider != null)
        {
            circleCollider.radius = initialColliderRadius;
        }

        if (polygonCollider != null && polygonColliderPath.Length > 0)
        {
            polygonCollider.SetPath(0, polygonColliderPath);
        }
    }
    public Material MaterialInstance => mat;
    static float Frac(float x) => x - Mathf.Floor(x);
    static float Hash(float x) { return Frac(Mathf.Sin(x * 12.9898f) * 43758.5453f); }


    Color ApplyColorAdjustments(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        h = Mathf.Repeat(h + hueShift, 1f);
        s = Mathf.Clamp01(s * saturationMultiplier);
        v = Mathf.Clamp01(v * brightnessMultiplier);
        Color adjusted = Color.HSVToRGB(h, s, v);
        adjusted.a = Mathf.Clamp01(baseColor.a * alphaMultiplier);
        return adjusted;
    }

    void UpdateJellyShaderProperties(List<Vector2> localPoints)
    {
        if (mat == null || _vertexTexture == null) return;
        int pointCount = Mathf.Min(localPoints.Count, MAX_VERTICES);
        for (int i = 0; i < pointCount; i++)
        {
            _textureColorData[i] = new Color(localPoints[i].x, localPoints[i].y, 0, 0);
        }
        for (int i = pointCount; i < MAX_VERTICES; i++) _textureColorData[i] = Color.clear;
        _vertexTexture.SetPixels(_textureColorData);
        _vertexTexture.Apply(false);
        mat.SetTexture("_VertexTex", _vertexTexture);
        mat.SetInt("_VertexCount", pointCount);
    }
}


