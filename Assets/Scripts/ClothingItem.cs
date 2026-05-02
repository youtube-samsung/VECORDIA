using UnityEngine;

public class ClothingItem : MonoBehaviour
{
    public ClothingItemData data;
    public int currentHangerIndex = -1;

    public void Initialize()
    {
        if (data == null) return;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Создаем новый материал, чтобы не менять общий ассет материала
            renderer.material = new Material(renderer.material) { color = data.itemColor };
        }
    }
}