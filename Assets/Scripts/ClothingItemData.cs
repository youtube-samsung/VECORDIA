using UnityEngine;

[CreateAssetMenu(fileName = "New Clothing Item", menuName = "Ritual/Clothing Item Data")]
public class ClothingItemData : ScriptableObject
{
    public string itemName;
    public int sortOrder; // 0=красный
    public Color itemColor = Color.white;
}