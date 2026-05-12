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
    private Color[] meshColors;


    public void GenerateMesh()
    {
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

            meshColors[i * 3] = targetColor;    
            meshColors[i * 3 + 1] = targetColor;
            meshColors[i * 3 + 2] = targetColor; 
        }


        soilMesh.colors = meshColors;
    }
}