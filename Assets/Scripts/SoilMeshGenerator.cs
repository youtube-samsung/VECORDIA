using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SoilMeshGenerator : MonoBehaviour
{
    [Header("Настройки формы")]
    public float soilRadius = 0.3f;
    [Range(1, 10)] public int radialSegments = 4;
    public LayerMask soilLayerMask;

    [Header("Материалы для слоев-текстур")]
    public Material materialMoist;
    public Material materialIdeal;
    public Material materialOverwatered;
    public Color previewContourColor = new Color(0f, 0.8f, 1f, 0.5f);

    private Mesh meshMoist, meshIdeal, meshOverwatered;
    private Color[] colorsMoist, colorsIdeal, colorsOverwatered;
    private GameObject previewObject;
    private int pointsPerDegree;
    private int totalVertices;

    public void GenerateMesh(int sprayAngleWidth)
    {
        pointsPerDegree = radialSegments * 6;
        totalVertices = 360 * pointsPerDegree;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] flatVertices = new Vector3[totalVertices];
        Vector2[] uvs = new Vector2[totalVertices];
        int[] triangles = new int[totalVertices];

        int vIndex = 0;


        float baseHeight = 0f;
        RaycastHit centerHit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out centerHit, 5f, soilLayerMask))
        {
            baseHeight = transform.InverseTransformPoint(centerHit.point).y;
        }

        for (int i = 0; i < 360; i++)
        {
            float rad1 = Mathf.Deg2Rad * i;
            float rad2 = Mathf.Deg2Rad * ((i + 1) % 360);

            for (int j = 0; j < radialSegments; j++)
            {
                float rInner = soilRadius * ((float)j / radialSegments);
                float rOuter = soilRadius * ((float)(j + 1) / radialSegments);

                Vector3 p1 = new Vector3(Mathf.Sin(rad1) * rInner, 0, Mathf.Cos(rad1) * rInner);
                Vector3 p2 = new Vector3(Mathf.Sin(rad2) * rInner, 0, Mathf.Cos(rad2) * rInner);
                Vector3 p3 = new Vector3(Mathf.Sin(rad1) * rOuter, 0, Mathf.Cos(rad1) * rOuter);
                Vector3 p4 = new Vector3(Mathf.Sin(rad2) * rOuter, 0, Mathf.Cos(rad2) * rOuter);

                flatVertices[vIndex] = p1; flatVertices[vIndex + 1] = p3; flatVertices[vIndex + 2] = p4;
                flatVertices[vIndex + 3] = p1; flatVertices[vIndex + 4] = p4; flatVertices[vIndex + 5] = p2;


                p1 = SnapToTerrain(p1, baseHeight);
                p2 = SnapToTerrain(p2, baseHeight);
                p3 = SnapToTerrain(p3, baseHeight);
                p4 = SnapToTerrain(p4, baseHeight);

                vertices[vIndex] = p1; vertices[vIndex + 1] = p3; vertices[vIndex + 2] = p4;
                vertices[vIndex + 3] = p1; vertices[vIndex + 4] = p4; vertices[vIndex + 5] = p2;

                for (int k = 0; k < 6; k++)
                {
                    triangles[vIndex + k] = vIndex + k;
                    Vector3 flatP = flatVertices[vIndex + k];
                    uvs[vIndex + k] = new Vector2(flatP.x / (soilRadius * 2f) + 0.5f, flatP.z / (soilRadius * 2f) + 0.5f);
                }
                vIndex += 6;
            }
        }

        GetComponent<MeshRenderer>().enabled = false;

        colorsMoist = new Color[totalVertices];
        colorsIdeal = new Color[totalVertices];
        colorsOverwatered = new Color[totalVertices];

        meshMoist = CreateLayer("Layer_1_Moist", vertices, triangles, uvs, materialMoist, 0.001f, ref colorsMoist);
        meshIdeal = CreateLayer("Layer_2_Ideal", vertices, triangles, uvs, materialIdeal, 0.002f, ref colorsIdeal);
        meshOverwatered = CreateLayer("Layer_3_Over", vertices, triangles, uvs, materialOverwatered, 0.003f, ref colorsOverwatered);

        CreatePreview(flatVertices, triangles, sprayAngleWidth);
    }

    private Vector3 SnapToTerrain(Vector3 point, float fallbackHeight)
    {
        RaycastHit hit;
        Vector3 worldPoint = transform.TransformPoint(point);
        if (Physics.Raycast(worldPoint + Vector3.up * 2f, Vector3.down, out hit, 5f, soilLayerMask))
        {
            point.y = transform.InverseTransformPoint(hit.point).y;
        }
        else
        {
            point.y = fallbackHeight;
        }
        return point;
    }

    private Mesh CreateLayer(string name, Vector3[] verts, int[] tris, Vector2[] uvs, Material mat, float yOffset, ref Color[] colors)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.up * yOffset;
        obj.transform.localRotation = Quaternion.identity;

        Mesh mesh = new Mesh { vertices = verts, triangles = tris, uv = uvs };
        for (int i = 0; i < totalVertices; i++) colors[i] = new Color(1, 1, 1, 0f);
        mesh.colors = colors;
        mesh.RecalculateNormals();

        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = mat;
        return mesh;
    }

    private void CreatePreview(Vector3[] flatVerts, int[] tris, int angleWidth)
    {
        previewObject = new GameObject("PreviewOverlay");
        previewObject.transform.position = transform.position + Vector3.up * 0.02f;
        previewObject.transform.rotation = transform.rotation;

        Mesh pMesh = new Mesh { vertices = flatVerts, triangles = tris };
        Color[] pColors = new Color[totalVertices];

        int halfWidth = angleWidth / 2;
        for (int i = 0; i < 360; i++)
        {
            int diff = (i + 180) % 360 - 180;
            Color col = Mathf.Abs(diff) <= halfWidth ? previewContourColor : new Color(0, 0, 0, 0);
            for (int k = 0; k < pointsPerDegree; k++) pColors[i * pointsPerDegree + k] = col;
        }

        pMesh.colors = pColors;
        previewObject.AddComponent<MeshFilter>().mesh = pMesh;
        previewObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        previewObject.SetActive(false);
    }

    public void TogglePreview(bool active) { if (previewObject) previewObject.SetActive(active); }

    public void UpdateColors(int[] soilDegrees)
    {
        if (meshMoist == null) return;

        for (int i = 0; i < 360; i++)
        {
            int stage = soilDegrees[i];
            int startIdx = i * pointsPerDegree;

            for (int k = 0; k < pointsPerDegree; k++)
            {
                int idx = startIdx + k;
                colorsMoist[idx].a = stage == 1 ? 1f : 0f;
                colorsIdeal[idx].a = stage == 2 ? 1f : 0f;
                colorsOverwatered[idx].a = stage == 3 ? 1f : 0f;
            }
        }

        meshMoist.colors = colorsMoist;
        meshIdeal.colors = colorsIdeal;
        meshOverwatered.colors = colorsOverwatered;
    }

    private void OnDestroy() { if (previewObject) Destroy(previewObject); }
}