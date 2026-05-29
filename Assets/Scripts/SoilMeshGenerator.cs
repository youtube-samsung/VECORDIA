using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SoilMeshGenerator : MonoBehaviour
{
    public float soilRadius = 0.5f;

    public Color colorDry = new Color(0f, 0f, 0f, 0f);
    public Color colorMoist = new Color(0.2f, 0.1f, 0.05f, 0.5f);
    public Color colorIdeal = new Color(0.1f, 0.05f, 0.02f, 0.9f);
    public Color colorOverwatered = new Color(0f, 0f, 0f, 1f);
    public Color previewContourColor = new Color(0f, 0.8f, 1f, 0.5f);

    private Mesh soilMesh;
    private Color[] meshColors;
    private GameObject previewObject;
    private Color[] stateColors;

    public void GenerateMesh(int sprayAngleWidth)
    {
        stateColors = new Color[] { colorDry, colorMoist, colorIdeal, colorOverwatered };

        soilMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = soilMesh;

        Vector3[] vertices = new Vector3[360 * 3];
        int[] triangles = new int[360 * 3];
        meshColors = new Color[360 * 3];

        for (int i = 0; i < 360; i++)
        {
            float rad1 = Mathf.Deg2Rad * i;
            float rad2 = Mathf.Deg2Rad * ((i + 1) % 360);

            vertices[i * 3] = Vector3.zero;
            vertices[i * 3 + 1] = new Vector3(Mathf.Sin(rad1) * soilRadius, 0, Mathf.Cos(rad1) * soilRadius);
            vertices[i * 3 + 2] = new Vector3(Mathf.Sin(rad2) * soilRadius, 0, Mathf.Cos(rad2) * soilRadius);

            triangles[i * 3] = i * 3;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;

            meshColors[i * 3] = colorDry;
            meshColors[i * 3 + 1] = colorDry;
            meshColors[i * 3 + 2] = colorDry;
        }

        soilMesh.vertices = vertices;
        soilMesh.triangles = triangles;
        soilMesh.colors = meshColors;

        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (rend.sharedMaterial == null)
        {
            rend.material = new Material(Shader.Find("Sprites/Default"));
        }

        previewObject = new GameObject("WateringPreviewOverlay");
        previewObject.transform.position = transform.position + Vector3.up * 0.001f;

        MeshFilter pFilter = previewObject.AddComponent<MeshFilter>();
        MeshRenderer pRend = previewObject.AddComponent<MeshRenderer>();

        Mesh previewMesh = new Mesh();
        previewMesh.vertices = vertices;
        previewMesh.triangles = triangles;

        Color[] previewColors = new Color[vertices.Length];
        int halfWidth = sprayAngleWidth / 2;

        for (int i = 0; i < 360; i++)
        {
            int diff = (i + 180) % 360 - 180;
            if (Mathf.Abs(diff) <= halfWidth)
            {
                previewColors[i * 3] = new Color(0, 0, 0, 0);
                previewColors[i * 3 + 1] = previewContourColor;
                previewColors[i * 3 + 2] = previewContourColor;
            }
            else
            {
                previewColors[i * 3] = new Color(0, 0, 0, 0);
                previewColors[i * 3 + 1] = new Color(0, 0, 0, 0);
                previewColors[i * 3 + 2] = new Color(0, 0, 0, 0);
            }
        }

        previewMesh.colors = previewColors;
        pFilter.mesh = previewMesh;
        pRend.material = new Material(Shader.Find("Sprites/Default"));
        previewObject.SetActive(false);
    }

    public void TogglePreview(bool active)
    {
        if (previewObject != null)
        {
            previewObject.SetActive(active);
        }
    }

    public void UpdateColors(int[] soilDegrees)
    {
        if (soilMesh == null || meshColors == null) return;

        for (int i = 0; i < 360; i++)
        {
            int val = soilDegrees[i];
            Color blendedColor = stateColors[val];

            meshColors[i * 3] = blendedColor;
            meshColors[i * 3 + 1] = blendedColor;
            meshColors[i * 3 + 2] = blendedColor;
        }

        soilMesh.colors = meshColors;
    }

    private void OnDestroy()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }
}