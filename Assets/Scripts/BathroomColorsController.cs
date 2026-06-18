using UnityEngine;

public class BathroomColorsController : MonoBehaviour
{
    [Header("Цилиндры в ванной (7 штук слева направо)")]
    public MeshRenderer[] cylinders;

    private void OnEnable()
    {
        // Теперь слушаем только Шкаф!
        ClosetRitualController.OnClosetColorsReady += UpdateClueColors;
    }

    private void OnDisable()
    {
        ClosetRitualController.OnClosetColorsReady -= UpdateClueColors;
    }

    private void Start()
    {
        UpdateClueColors();
    }

    private void UpdateClueColors()
    {
        if (SessionProgress.correctClosetColors == null || SessionProgress.correctClosetColors.Count == 0) return;

        for (int i = 0; i < cylinders.Length; i++)
        {
            if (i >= SessionProgress.correctClosetColors.Count) break;

            if (cylinders[i] != null)
            {
                cylinders[i].material.color = SessionProgress.correctClosetColors[i];
            }
        }
        Debug.Log("[Ванная] Цвета подсказок успешно синхронизированы СО ШКАФОМ!");
    }
}