using UnityEngine;

public class CucumberBoardController : MonoBehaviour
{
    [Header("Твой префаб шрама (ОБЫЧНЫЙ QUAD)")]
    public GameObject scarPrefab;

    [Header("Коллайдер доски (для высоты)")]
    [Tooltip("Перетащи сюда объект cuc_board")]
    public Collider boardCollider;

    private void Start()
    {
        if (SessionProgress.cucumberScars == null) return;

        foreach (var scarData in SessionProgress.cucumberScars)
        {
            ApplyScar(scarData.localPosition, scarData.missFactor);
        }
    }

    public void RegisterNewScar(Vector3 hitWorldPos, float missFactor)
    {
        // 1. Берем мировую точку удара
        Vector3 surfacePos = hitWorldPos;

        // 2. Игнорируем высоту огурца! Жестко ставим высоту по верхней границе доски
        if (boardCollider != null)
        {
            // bounds.max.y — это математически самая верхняя точка объекта в мире
            surfacePos.y = boardCollider.bounds.max.y + 0.002f;
        }

        // 3. Переводим в локальные координаты нашего чистого Board_Root
        Vector3 localPos = transform.InverseTransformPoint(surfacePos);

        SessionProgress.cucumberScars.Add(new SessionProgress.CucumberScarData
        {
            localPosition = localPos,
            missFactor = missFactor
        });

        ApplyScar(localPos, missFactor);
    }

    private void ApplyScar(Vector3 localPos, float missFactor)
    {
        GameObject newScar = Instantiate(scarPrefab, transform);
        newScar.transform.localPosition = localPos;

        // Тот самый идеальный угол
        newScar.transform.localRotation = Quaternion.Euler(90f, 45f, 0f);

        float baseScale = 0.2f;
        float scaleMultiplier = Mathf.Lerp(1f, 0.3f, missFactor);
        float finalScale = baseScale * scaleMultiplier;
        float randomThickness = finalScale + Random.Range(-0.02f, 0.02f);

        newScar.transform.localScale = new Vector3(finalScale, randomThickness, 1f);
    }
}