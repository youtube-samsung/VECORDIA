using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

public class CucumberRitualController : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;

    [Header("Объекты Ритуала")]
    public Transform ritualCameraTarget;
    public GameObject knifeObject;
    public GameObject[] cucumberStages;
    public GameObject[] cutPoints;

    [Header("Настройки Нарезки")]
    [Tooltip("Сила, с которой отлетает отрезанный ломтик.")]
    public float sliceForce = 1f;
    [Tooltip("Максимальное расстояние от центра точки, которое считается ИДЕАЛЬНЫМ резом.")]
    public float cuttingTolerance = 0.01f;
    [Tooltip("Максимальное расстояние, на котором клик засчитывается как попытка.")]
    public float maxCutDistance = 0.05f;
    [Tooltip("Множитель тревожности.")]
    public float anxietyMultiplier = 0.25f;
    [Tooltip("Множитель скорости движения ножа.")]
    public float knifeMovementSpeed = 0.05f;

    [Header("UI")]
    public TextMeshProUGUI anxietyCounterText;

    private int currentStageIndex = 0;
    private bool _isRitualActive = false;
    public bool IsRitualActive => _isRitualActive;
    private float currentAnxiety = 0f;
    private bool canCut = false;
    private float knifeMinX, knifeMaxX;

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        currentAnxiety = 0f;
        currentStageIndex = 0;
        canCut = true;
        if (ritualActivator != null) ritualActivator.HidePrompt();
        // Подписываемся на событие ТОЛЬКО при старте ритуала
        if (inputReader != null) inputReader.OnRitualClickPerformed += OnRitualClick;

        ToggleRitualObjects(true);
        UpdateAnxietyUI();

        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);
    }

    private void OnDestroy()
    {
        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnRitualClick;
    }
    public void Interact(int stage)
    {
        // Неважно, какая стадия, для огурца это всегда просто начало.
        if (!_isRitualActive)
        {
            StartRitual();
        }
    }

    //public void StartRitual()
    //{
    //    if (_isRitualActive) return;
    //    _isRitualActive = true;
    //    currentAnxiety = 0f;
    //    currentStageIndex = 0;
    //    canCut = true;

    //    ToggleRitualObjects(true);
    //    UpdateAnxietyUI();

    //    if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);

    //    Vector3 initialKnifePos = knifeObject.transform.position;
    //    knifeMinX = initialKnifePos.x - 0.5f;
    //    knifeMaxX = initialKnifePos.x + 0.5f;
    //}

    private void Update()
    {
        if (!_isRitualActive) return;

        float mouseXDelta = inputReader.RitualLookValue.x * knifeMovementSpeed * Time.deltaTime;
        Vector3 newKnifePos = knifeObject.transform.position;
        newKnifePos += cameraHandler.playerCamera.transform.right * mouseXDelta;
        knifeObject.transform.position = newKnifePos;

        RaycastHit hit;
        Vector3 rayStart = knifeObject.transform.position;
        Vector3 rayDirection = Vector3.down;
        float rayLength = 1f;

        if (Physics.Raycast(rayStart, rayDirection, out hit, rayLength))
        {
            Color rayColor;
            if (currentStageIndex < cutPoints.Length)
            {
                Transform targetCutPoint = cutPoints[currentStageIndex].transform;
                float distanceToTarget = Vector3.Distance(hit.point, targetCutPoint.position);

                if (distanceToTarget <= cuttingTolerance)
                {
                    rayColor = Color.green;
                }
                else if (distanceToTarget <= maxCutDistance)
                {
                    rayColor = Color.yellow;
                }
                else
                {
                    rayColor = Color.red;
                }
            }
            else
            {
                rayColor = Color.grey;
            }
            Debug.DrawRay(rayStart, rayDirection * hit.distance, rayColor);
        }
        else
        {
            Debug.DrawRay(rayStart, rayDirection * rayLength, Color.red);
        }
    }

    private void OnRitualClick()
    {
        if (!_isRitualActive || !canCut) return;

        RaycastHit hit;
        Vector3 rayStart = knifeObject.transform.position;
        Vector3 rayDirection = Vector3.down;
        float rayLength = 1f;

        canCut = false;

        if (Physics.Raycast(rayStart, rayDirection, out hit, rayLength))
        {
            if (currentStageIndex >= cutPoints.Length)
            {
                canCut = true;
                return;
            }

            Transform targetCutPoint = cutPoints[currentStageIndex].transform;
            float distanceToTarget = Vector3.Distance(hit.point, targetCutPoint.position);

            if (distanceToTarget > maxCutDistance)
            {
                Debug.Log($"СЛИШКОМ ДАЛЕКО! Разрез не выполнен. Расстояние: {distanceToTarget}");
                canCut = true;
                return;
            }

            float missFactor = Mathf.InverseLerp(0, maxCutDistance, distanceToTarget);
            currentAnxiety += missFactor * anxietyMultiplier;
            Debug.Log($"Рез! Расстояние: {distanceToTarget:F4}, Фактор промаха: {missFactor:F2}, Тревожность + {missFactor * anxietyMultiplier:F3}");
            UpdateAnxietyUI();

            PerformCut();

        }
        else
        {
            Debug.Log("Нож ударил в пустоту!");
            currentAnxiety += anxietyMultiplier;
            UpdateAnxietyUI();
            canCut = true;
        }
    }

    private void PerformCut()
    {
        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            cucumberStages[currentStageIndex].SetActive(false);
        }

        currentStageIndex++;
        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            cucumberStages[currentStageIndex].SetActive(true);
            Rigidbody sliceRb = cucumberStages[currentStageIndex].GetComponentInChildren<Rigidbody>();
            if (sliceRb != null)
            {
                Vector3 forceDirection = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.2f), Random.Range(-0.2f, 0.2f));
                sliceRb.AddForce(forceDirection.normalized * sliceForce, ForceMode.Impulse);
            }
        }

        if (currentStageIndex >= cutPoints.Length)
        {
            Invoke("EndRitual", 1f);
        }
        else
        {
            canCut = true;
        }
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        // Отписываемся от события ТОЛЬКО при завершении ритуала
        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnRitualClick;

        ToggleRitualObjects(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }

    private void ToggleRitualObjects(bool active)
    {
        knifeObject.SetActive(active);
        if (active)
        {
            if (cucumberStages.Length > 0 && cucumberStages[0] != null)
                cucumberStages[0].SetActive(true);
        }
        else
        {
            foreach (var stage in cucumberStages) if (stage != null) stage.SetActive(false);
        }
        if (anxietyCounterText != null) anxietyCounterText.gameObject.SetActive(active);
    }

    private void UpdateAnxietyUI()
    {
        if (anxietyCounterText != null) anxietyCounterText.text = $"Тревожность: {currentAnxiety:F2}";
    }
}
