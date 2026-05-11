using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SoilMeshGenerator : MonoBehaviour
{
    [Header("Настройки Маски")]
    public float soilRadius = 0.5f;

    [Header("Прозрачность и Цвета (Слои)")]
    public Color colorDry = new Color(0f, 0f, 0f, 0f);
    public Color colorMoist = new Color(0.2f, 0.1f, 0.05f, 0.5f);
    public Color colorIdeal = new Color(0.1f, 0.05f, 0.02f, 0.9f);
    public Color colorOverwatered = new Color(0f, 0f, 0f, 1f);

    private Mesh soilMesh;
    private Color[] meshColors = new Color[361];

    public void GenerateMesh()
    {
        soilMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = soilMesh;

        Vector3[] vertices = new Vector3[361];
        int[] triangles = new int[360 * 3];

        vertices[0] = Vector3.zero;
        meshColors[0] = colorDry;

        for (int i = 0; i < 360; i++)
        {
            float rad = Mathf.Deg2Rad * i;
            vertices[i + 1] = new Vector3(Mathf.Sin(rad) * soilRadius, 0, Mathf.Cos(rad) * soilRadius);
            meshColors[i + 1] = colorDry;

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i == 359) ? 1 : i + 2;
        }

        soilMesh.vertices = vertices;
        soilMesh.triangles = triangles;
        soilMesh.colors = meshColors;

        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (rend.sharedMaterial == null)
        {
            rend.material = new Material(Shader.Find("Sprites/Default"));
        }
    }


    public void UpdateColors(int[] soilDegrees)
    {
        for (int i = 0; i < 360; i++)
        {
            int state = soilDegrees[i];
            Color targetColor = colorDry;
            if (state == 1) targetColor = colorMoist;
            else if (state == 2) targetColor = colorIdeal;
            else if (state == 3) targetColor = colorOverwatered;

            meshColors[i + 1] = targetColor;
        }
        meshColors[0] = meshColors[1]; 
        soilMesh.colors = meshColors;
    }
}