using UnityEngine;

public class ClothingItem : MonoBehaviour
{
    public ClothingItemData data;
    public int currentHangerIndex = -1;
    public int clothingID;

    public void Initialize()
    {
        if (data == null) return;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
           
            renderer.material.color =  data.itemColor ;
        }
    }
}