using System.Collections.Generic;
using UnityEngine;

public enum ShapeType { Circle, Triangle, Box }

[RequireComponent(typeof(Renderer)), RequireComponent(typeof(MeshFilter))]
public class HeartVisual : MonoBehaviour
{
    [Header("Visual")]
    public ShapeType shapeType = ShapeType.Circle;
    public Color     color     = Color.white;
    public float     radius    = 0.6f;

    [Header("Refs (optional)")]
    public HeartProfile profile;
    public HeartAgent   agent;

    [Header("Material")]
    public Material baseMaterial;

    Material        mat;
    Renderer        rend;
    MeshFilter      meshFilter;
    Mesh            meshInstance;
    CircleCollider2D circleCollider;

    Vector3 initialScale;
    float   initialColliderRadius;
    float   initialWorldRadius;
    bool    initialized;

    readonly List<Vector2> vertexScratch = new List<Vector2>(16);

    int  lastAppliedVertexCount = -1;
    bool lastWasCircle;

    static readonly int[] VertexCycle =
    {
        0, 3, 4, 5, 6, 7, 8, 9, 10,
        0, 3, 4, 5, 6, 7, 8, 9, 10,
        0, 3, 4, 5, 6, 7, 8, 9, 10
    };

    const int CircleSegments = 32;

    public struct ProceduralShapeParameters
    {
        public float normalizedHr;
        public float normalizedCv;
        public float normalizedRange;
        public float normalizedMean;
        public float shapeSelector;
        public float irregularity;
        public float orientation;
        public float seed;
    }

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        Initialize();
        ApplyRadiusScale(radius);
    }

    void Initialize()
    {
        if (initialized) return;

        if (agent == null) agent = GetComponent<HeartAgent>();
        rend           = GetComponent<Renderer>();
        meshFilter     = GetComponent<MeshFilter>();
        circleCollider = GetComponent<CircleCollider2D>();

        initialScale = transform.localScale;

        const float minRadius = 0.0001f;
        float colliderLocalRadius = minRadius;
        if (circleCollider != null && circleCollider.radius > minRadius)
        {
            colliderLocalRadius = circleCollider.radius;
        }
        else
        {
            colliderLocalRadius = Mathf.Max(radius, minRadius);
        }

        initialColliderRadius = colliderLocalRadius;

        float baseScale = Mathf.Max(initialScale.x, minRadius);
        float worldRadius = colliderLocalRadius * baseScale;
        if (worldRadius <= minRadius)
        {
            worldRadius = Mathf.Max(radius, 0.1f);
        }
        initialWorldRadius = worldRadius;

        var src = baseMaterial != null ? baseMaterial : (rend != null ? rend.sharedMaterial : null);
        if (src != null)
        {
            mat = new Material(src);
            if (rend != null) rend.material = mat;
        }
        else
        {
            Debug.LogWarning("[HeartVisual] Source material not found.", this);
        }

        EnsureMesh();
        initialized = true;
    }

    void EnsureMesh()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshFilter == null)
        {
            return;
        }

        if (meshInstance == null)
        {
            meshInstance = new Mesh { name = $"{name}_HeartShape" };
            meshInstance.MarkDynamic();
        }

        if (meshFilter.sharedMesh != meshInstance)
        {
            meshFilter.sharedMesh = meshInstance;
        }
    }

    void Update()
    {
        RefreshMaterial(Time.time);
    }

    public void RefreshMaterial()
    {
        RefreshMaterial(Time.time);
    }

    public void RefreshMaterial(float timeSeconds)
    {
        ApplyMaterialState(timeSeconds);
    }

    void ApplyMaterialState(float timeSeconds)
    {
        Initialize();

        float bpm    = (profile != null) ? profile.hr : 70f;
        float bpm01  = Mathf.InverseLerp(50f, 120f, bpm);
        float beatHz = Mathf.Lerp(1.0f, 2.4f, bpm01);
        float pulse  = (Mathf.Sin(timeSeconds * beatHz * Mathf.PI * 2f) + 1f) * 0.5f;

        if (mat == null) return;

        mat.SetFloat("_Pulse",     pulse);
        mat.SetFloat("_Radius",    radius);
        mat.SetColor("_Tint",      color);
        mat.SetFloat("_ShapeType", (float)shapeType);
    }

    public int ApplyProceduralShape(ProceduralShapeParameters parameters)
    {
        Initialize();
        EnsureMesh();
        if (meshInstance == null)
        {
            return 0;
        }

        int vertexCount = DetermineVertexCount(parameters);

        if (vertexCount <= 0)
        {
            BuildCircleMesh(parameters);
            lastWasCircle = true;
            lastAppliedVertexCount = 0;
            return 0;
        }

        BuildPolygonMesh(vertexCount, parameters);
        lastWasCircle = false;
        lastAppliedVertexCount = vertexCount;
        return vertexCount;
    }

    int DetermineVertexCount(ProceduralShapeParameters p)
    {
        float cycle = Frac(p.shapeSelector + p.normalizedRange * 0.312f + p.normalizedCv * 0.127f);
        int index = Mathf.Clamp(Mathf.FloorToInt(cycle * VertexCycle.Length), 0, VertexCycle.Length - 1);
        int count = VertexCycle[index];

        if (count == 0)
        {
            float circleBias = Mathf.Lerp(0.25f, 0.75f, 1f - p.normalizedCv);
            if (p.shapeSelector < circleBias)
            {
                return 0;
            }
            int nextIndex = (index + 1) % VertexCycle.Length;
            count = VertexCycle[nextIndex];
        }

        if (count != 0)
        {
            float offsetChooser = p.irregularity - 0.5f;
            if (Mathf.Abs(offsetChooser) > 0.33f)
            {
                int offset = offsetChooser > 0f ? 1 : -1;
                int altIndex = (index + offset + VertexCycle.Length) % VertexCycle.Length;
                int alt = VertexCycle[altIndex];
                if (alt != 0)
                {
                    count = alt;
                }
            }
        }

        return Mathf.Clamp(count, 3, 12);
    }

    void BuildCircleMesh(ProceduralShapeParameters p)
    {
        meshInstance.Clear();

        var vertices = new Vector3[CircleSegments + 1];
        var uvs      = new Vector2[CircleSegments + 1];
        var tris     = new int[CircleSegments * 3];

        vertices[0] = Vector3.zero;
        uvs[0]      = new Vector2(0.5f, 0.5f);

        float baseRadius = 0.5f;
        float seedBase   = p.seed * 17.0f;

        for (int i = 0; i < CircleSegments; i++)
        {
            float t     = i / (float)CircleSegments;
            float angle = t * Mathf.PI * 2f;

            float jitter = (Hash(seedBase + i * 0.73f) - 0.5f) * Mathf.Lerp(0.0f, 0.04f, p.irregularity);
            float radiusOffset = Mathf.Clamp(baseRadius - jitter, 0.42f, 0.5f);

            float x = Mathf.Cos(angle) * radiusOffset;
            float y = Mathf.Sin(angle) * radiusOffset;

            vertices[i + 1] = new Vector3(x, y, 0f);
            uvs[i + 1]      = new Vector2(x + 0.5f, y + 0.5f);

            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i == CircleSegments - 1) ? 1 : i + 2;
        }

        meshInstance.vertices  = vertices;
        meshInstance.uv        = uvs;
        meshInstance.triangles = tris;
        meshInstance.RecalculateBounds();
        meshInstance.RecalculateNormals();
    }

    void BuildPolygonMesh(int vertexCount, ProceduralShapeParameters p)
    {
        meshInstance.Clear();
        GeneratePolygonVertices(vertexCount, p);

        int count = vertexScratch.Count;

        var vertices = new Vector3[count + 1];
        var uvs      = new Vector2[count + 1];
        var tris     = new int[count * 3];

        vertices[0] = Vector3.zero;
        uvs[0]      = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < count; i++)
        {
            Vector2 v = vertexScratch[i];
            vertices[i + 1] = new Vector3(v.x, v.y, 0f);
            uvs[i + 1]      = new Vector2(v.x + 0.5f, v.y + 0.5f);
        }

        for (int i = 0; i < count; i++)
        {
            int triIndex = i * 3;
            int b = i + 1;
            int c = (i + 1) % count + 1;
            tris[triIndex]     = 0;
            tris[triIndex + 1] = b;
            tris[triIndex + 2] = c;
        }

        meshInstance.vertices  = vertices;
        meshInstance.uv        = uvs;
        meshInstance.triangles = tris;
        meshInstance.RecalculateBounds();
        meshInstance.RecalculateNormals();
    }

    void GeneratePolygonVertices(int vertexCount, ProceduralShapeParameters p)
    {
        vertexScratch.Clear();

        float halfWidth  = Mathf.Lerp(0.32f, 0.5f, Mathf.Clamp01(p.normalizedRange));
        float halfHeight = Mathf.Lerp(0.32f, 0.5f, Mathf.Clamp01(p.normalizedMean));
        float balance    = (p.normalizedCv - 0.5f) * 0.18f;

        halfWidth  = Mathf.Clamp(halfWidth + balance, 0.26f, 0.5f);
        halfHeight = Mathf.Clamp(halfHeight - balance, 0.26f, 0.5f);

        if (vertexCount < 4)
        {
            GenerateTriangleVertices(vertexCount, p);
        }
        else
        {
            int[] counts = new int[4];
            AllocateEdgeCounts(vertexCount, p, counts);
            AppendEdgeVertices(counts, halfWidth, halfHeight, p);
        }

        ApplyPostProcess(vertexScratch, p);
        EnsureCounterClockwise(vertexScratch);
    }

    void GenerateTriangleVertices(int count, ProceduralShapeParameters p)
    {
        float baseRadius = Mathf.Clamp(Mathf.Lerp(0.28f, 0.48f, p.normalizedRange * 0.65f + p.normalizedMean * 0.35f), 0.24f, 0.5f);
        float seedBase   = p.seed * 9.71f + count * 0.37f;
        float baseAngle  = (p.shapeSelector * 2f - 1f) * Mathf.PI;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float jitterAngle = (Hash(seedBase + i * 1.917f) - 0.5f) * 0.6f / count;
            float angle = baseAngle + (t + jitterAngle) * Mathf.PI * 2f;
            float radiusVariation = (Hash(seedBase + i * 2.618f) - 0.5f) * Mathf.Lerp(0.05f, 0.18f, p.irregularity);
            float rad = Mathf.Clamp(baseRadius + radiusVariation, 0.22f, 0.5f);
            vertexScratch.Add(new Vector2(Mathf.Cos(angle) * rad, Mathf.Sin(angle) * rad));
        }
    }

    void AllocateEdgeCounts(int vertexCount, ProceduralShapeParameters p, int[] counts)
    {
        for (int i = 0; i < 4; i++) counts[i] = 1;
        int remaining = vertexCount - 4;
        if (remaining <= 0) return;

        float[] weights =
        {
            Mathf.Max(0.01f, p.normalizedHr),    // top
            Mathf.Max(0.01f, p.normalizedMean),  // left
            Mathf.Max(0.01f, p.normalizedRange), // bottom
            Mathf.Max(0.01f, p.normalizedCv)     // right
        };

        float total = weights[0] + weights[1] + weights[2] + weights[3];
        float[] fractional = new float[4];
        int assigned = 0;

        for (int i = 0; i < 4; i++)
        {
            float norm = weights[i] / total;
            float exact = norm * remaining;
            int extra = Mathf.FloorToInt(exact);
            counts[i] += extra;
            assigned += extra;
            fractional[i] = exact - extra;
        }

        int left = remaining - assigned;
        while (left > 0)
        {
            int pick = 0;
            float best = fractional[0];
            for (int i = 1; i < 4; i++)
            {
                if (fractional[i] > best)
                {
                    best = fractional[i];
                    pick = i;
                }
            }
            counts[pick]++;
            fractional[pick] = 0f;
            left--;
        }
    }

    void AppendEdgeVertices(int[] counts, float halfWidth, float halfHeight, ProceduralShapeParameters p)
    {
        float seedBase = p.seed * 13.37f + halfWidth * 1.91f;
        AppendEdge(vertexScratch, counts[0], new Vector2( halfWidth,  halfHeight), new Vector2(-halfWidth,  halfHeight), seedBase + 1f, p.irregularity);
        AppendEdge(vertexScratch, counts[1], new Vector2(-halfWidth,  halfHeight), new Vector2(-halfWidth, -halfHeight), seedBase + 2f, p.irregularity);
        AppendEdge(vertexScratch, counts[2], new Vector2(-halfWidth, -halfHeight), new Vector2( halfWidth, -halfHeight), seedBase + 3f, p.irregularity);
        AppendEdge(vertexScratch, counts[3], new Vector2( halfWidth, -halfHeight), new Vector2( halfWidth,  halfHeight), seedBase + 4f, p.irregularity);
    }

    void AppendEdge(List<Vector2> buffer, int count, Vector2 start, Vector2 end, float seed, float irregularity)
    {
        if (count <= 0) return;
        float step = 1f / (count + 1);
        float jitterScale = step * (0.3f + irregularity * 0.5f);

        for (int i = 0; i < count; i++)
        {
            float t = (i + 1) * step;
            float jitter = (Hash(seed + i * 0.618f) - 0.5f) * jitterScale;
            t = Mathf.Clamp01(t + jitter);
            Vector2 point = Vector2.Lerp(start, end, t);
            buffer.Add(point);
        }
    }

    void ApplyPostProcess(List<Vector2> buffer, ProceduralShapeParameters p)
    {
        float angle = (p.orientation - 0.5f) * Mathf.PI * 2f;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float maxRadius = 0.5f;
        float irregularAmp = Mathf.Lerp(0.02f, 0.18f, p.irregularity);
        float seedBase = p.seed * 17.1717f;

        for (int i = 0; i < buffer.Count; i++)
        {
            Vector2 v = buffer[i];
            float noise = (Hash(seedBase + i * 1.318f) - 0.5f) * irregularAmp;
            if (v.sqrMagnitude > 0.0001f)
            {
                v += v.normalized * noise;
            }
            else
            {
                v += new Vector2(noise, noise);
            }

            float mag = v.magnitude;
            if (mag > maxRadius)
            {
                v *= maxRadius / mag;
            }

            float rx = v.x * cos - v.y * sin;
            float ry = v.x * sin + v.y * cos;
            buffer[i] = new Vector2(rx, ry);
        }
    }

    void EnsureCounterClockwise(List<Vector2> buffer)
    {
        if (buffer.Count < 3) return;
        float area = 0f;
        for (int i = 0; i < buffer.Count; i++)
        {
            Vector2 current = buffer[i];
            Vector2 next = buffer[(i + 1) % buffer.Count];
            area += (current.x * next.y) - (next.x * current.y);
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
    }

    public void SetRadius(float r)
    {
        ApplyRadiusScale(Mathf.Max(0f, r));
    }

    void ApplyRadiusScale(float targetRadius)
    {
        Initialize();

        radius = targetRadius;

        const float minRadius = 0.0001f;
        float worldRadius = Mathf.Max(targetRadius, minRadius);
        float baseWorld = Mathf.Max(initialWorldRadius, minRadius);
        float scaleFactor = worldRadius / baseWorld;

        if (!float.IsFinite(scaleFactor))
        {
            scaleFactor = 1f;
        }

        transform.localScale = initialScale * scaleFactor;

        if (circleCollider != null)
        {
            circleCollider.radius = initialColliderRadius;
        }
    }

    public Material MaterialInstance => mat;

    static float Frac(float x) => x - Mathf.Floor(x);

    static float Hash(float x)
    {
        return Frac(Mathf.Sin(x * 12.9898f) * 43758.5453f);
    }
}
