using UnityEngine;
using System.Collections;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

public class CucumberRitualController : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;

    public event System.Action OnInterruptionRequested;
    
    [Header("Объекты Ритуала")]
    public Transform ritualCameraTarget;
    public GameObject knifeObject;
    public GameObject[] cucumberStages;
    public GameObject[] cutPoints;
    private List<GameObject> ContainerSlice = new();

    [Header("Лазерный прицел (Свет уже внутри ножа!)")]
    [Tooltip("Перетащи сюда компонент Light, который ты сделал дочерним к ножу")]
    public Light cutLightMarker;
    public float maxLightIntensity = 2f;

    [Header("Звуковое сопровождение")]
    public SoundData sliceSound;
    public SoundData knifeMissSound;

    [Header("Настройки Нарезки")]
    public float sliceForce = 1f;
    public float cuttingTolerance = 0.01f;
    public float maxCutDistance = 0.06f;
    public float knifeMovementSpeed = 0.02f;

    [Header("Анимация Удара Ножом")]
    public float sliceAnimationDepth = 0.15f;
    public float sliceAnimationDuration = 0.2f;

    [Header("Влияние Тревожности (Тремор)")]
    public float anxietyPenaltyMultiplier = 10f;
    public float failAnxietyPenalty = 30f;
    public float baseTremorAmplitude = 0.01f;
    public float maxAnxietyTremorAmplitude = 0.08f;
    public float tremorSpeed = 10f;

    [Header("UI")]
    public TextMeshProUGUI anxietyCounterText;

    private int currentStageIndex = 0;
    private bool _isRitualActive = false;
    public bool IsRitualActive => _isRitualActive;

    private bool canCut = false;
    private bool isAnimatingCut = false;
    private Vector3 initialKnifeLocalPos;

    

    private void Start()
    {
        if (knifeObject != null)
        {
            initialKnifeLocalPos = knifeObject.transform.localPosition;
        }

        if (cutLightMarker != null)
        {
            cutLightMarker.gameObject.SetActive(false);
        }
    }

    public void Interact(int stage)
    {
        if (!_isRitualActive)
            StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;

        foreach ( GameObject obj in ContainerSlice)
        {
            Destroy(obj.gameObject);
        }
        ContainerSlice.Clear();

        _isRitualActive = true;
        currentStageIndex = 0;
        canCut = true;
        isAnimatingCut = false;

        if (ritualActivator != null) ritualActivator.HidePrompt();
        if (inputReader != null) inputReader.OnRitualClickPerformed += OnRitualClick;

        ToggleRitualObjects(true);

        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
         }

    private void Update()
    {
        if (!_isRitualActive) return;

        UpdateAnxietyUI();

        if (isAnimatingCut || !canCut)
        {
  
            if (cutLightMarker != null) cutLightMarker.gameObject.SetActive(false);
            return;
        }

        float mouseXDelta = inputReader.RitualLookValue.x * knifeMovementSpeed * Time.deltaTime;

        float currentTremorAmp = baseTremorAmplitude;
        if (AnxietyManager.Instance != null)
        {
            currentTremorAmp += maxAnxietyTremorAmplitude * AnxietyManager.Instance.GetTremorIntensity();
        }

        float noise = (Mathf.PerlinNoise(Time.time * tremorSpeed, 0f) - 0.5f) * 2f;
        float tremorOffset = noise * currentTremorAmp * Time.deltaTime;

        Vector3 newKnifePos = knifeObject.transform.position;
        newKnifePos += cameraHandler.playerCamera.transform.right * (mouseXDelta + tremorOffset);
        knifeObject.transform.position = newKnifePos;


        UpdateDynamicLightMarker();
    }

    private void UpdateDynamicLightMarker()
    {
        if (cutLightMarker == null || currentStageIndex >= cutPoints.Length) return;

        cutLightMarker.gameObject.SetActive(true);

        // Стреляем невидимым измерительным лучом строго из позиции самого прожектора-света вниз
        Vector3 rayStart = cutLightMarker.transform.position;
        RaycastHit hit;

        // Пускаем луч чуть дальше (на 1.5 метра), чтобы он точно долетел до доски
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            // === ЗАЩИТА ОТ ПОПАДАНИЯ В СЕБЯ ===
            // Если луч наткнулся на нож или на любую дочернюю детальку ножа — игнорируем этот кадр
            if (hit.collider.gameObject == knifeObject || hit.collider.transform.IsChildOf(knifeObject.transform))
            {
                return;
            }

            Transform targetCutPoint = cutPoints[currentStageIndex].transform;

            // Считаем дистанцию до идеального реза только по горизонтали (XZ)
            float dist = Vector2.Distance(
                new Vector2(hit.point.x, hit.point.z),
                new Vector2(targetCutPoint.position.x, targetCutPoint.position.z)
            );

            // Динамически меняем параметры Spotlight
            if (dist <= cuttingTolerance)
            {
                cutLightMarker.color = Color.green;
                cutLightMarker.intensity = maxLightIntensity; // 100% яркости
            }
            else if (dist <= maxCutDistance)
            {
                cutLightMarker.color = Color.yellow;
                cutLightMarker.intensity = maxLightIntensity * 0.7f; // 70% яркости
            }
            else
            {
                cutLightMarker.color = Color.red;
                cutLightMarker.intensity = maxLightIntensity * 0.3f; // 30% яркости
            }
        }
        else
        {
            // Если нож улетел куда-то совсем мимо доски — гасим лазер
            cutLightMarker.gameObject.SetActive(false);
        }
    }

    private void OnRitualClick()
    {
        if (!_isRitualActive || !canCut || isAnimatingCut) return;
        StartCoroutine(KnifeSliceAnimation());
    }

    private IEnumerator KnifeSliceAnimation()
    {
        isAnimatingCut = true;

        Vector3 originalPos = knifeObject.transform.position;
        Vector3 downPos = originalPos + Vector3.down * sliceAnimationDepth;
        float halfDuration = sliceAnimationDuration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            if (!_isRitualActive) yield break;
            knifeObject.transform.position = Vector3.Lerp(originalPos, downPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        knifeObject.transform.position = downPos;

        ProcessCutImpact(originalPos);

        if (!_isRitualActive) yield break;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            knifeObject.transform.position = Vector3.Lerp(downPos, originalPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        knifeObject.transform.position = originalPos;

        isAnimatingCut = false;
    }

    private void ProcessCutImpact(Vector3 originalPos)
    {
        if (currentStageIndex >= cutPoints.Length) return;

        // Проверяем, назначен ли фонарик
        if (cutLightMarker == null)
        {
            Debug.LogError("[Cucumber] КРИТИЧЕСКАЯ ОШИБКА: Забыл перетащить Light в слот CutLightMarker!");
            return;
        }

        // Точка старта луча — строго НАД нашим фонариком, но на исходной (верхней) высоте ножа
        Vector3 rayStart = new Vector3(cutLightMarker.transform.position.x, originalPos.y, cutLightMarker.transform.position.z);

        RaycastHit hit;
        // Пускаем луч вниз из идеальной точки лезвия
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            Transform targetCutPoint = cutPoints[currentStageIndex].transform;

            // Считаем промах ТОЛЬКО по горизонтали (X и Z), полностью игнорируя высоту Y!
            float distanceToTarget = Vector2.Distance(
                new Vector2(hit.point.x, hit.point.z),
                new Vector2(targetCutPoint.position.x, targetCutPoint.position.z)
            );

            // Дебаг-лог в консоль, чтобы ты видел точное расстояние в метрах до цели
            Debug.Log($"[Cut Test] Дистанция до цели по XZ: {distanceToTarget:F4} метров. Макс. лимит: {maxCutDistance}");

            if (distanceToTarget > maxCutDistance)
            {
                if (AudioManager.Instance != null && knifeMissSound != null)
                    AudioManager.Instance.PlaySound3D(knifeMissSound, knifeObject.transform.position);

                FailRitual("Слишком далеко от цели!");
                return;
            }

            // Успешный срез — играем смачный звук
            if (AudioManager.Instance != null && sliceSound != null)
                AudioManager.Instance.PlaySound3D(sliceSound, knifeObject.transform.position);

            // Начисляем микро-штраф тревоги, если отрезал не идеально ровно
            float missFactor = Mathf.InverseLerp(0, maxCutDistance, distanceToTarget);
            if (missFactor > 0 && AnxietyManager.Instance != null)
            {
                float penalty = missFactor * anxietyPenaltyMultiplier;
                AnxietyManager.Instance.AddAnxiety(penalty);
            }

            PerformCut();
        }
        else
        {
            // Если луч вообще ни обо что не ударился
            if (AudioManager.Instance != null && knifeMissSound != null)
                AudioManager.Instance.PlaySound3D(knifeMissSound, knifeObject.transform.position);

            FailRitual("Нож ударил мимо доски!");
        }
    }

    private void PerformCut()
    {
        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            Rigidbody oldSlice = cucumberStages[currentStageIndex].GetComponentInChildren<Rigidbody>();
            if (oldSlice != null) oldSlice.transform.SetParent(null);
            cucumberStages[currentStageIndex].SetActive(false);
        }

        currentStageIndex++;

        if (currentStageIndex == 2)
        {
            OnInterruptionRequested?.Invoke(); 
        }

        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            cucumberStages[currentStageIndex].SetActive(true);
            Rigidbody sliceRb = cucumberStages[currentStageIndex].GetComponentInChildren<Rigidbody>();
            if (sliceRb != null)
            {
                Vector3 forceDirection = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.2f), Random.Range(-0.2f, 0.2f));
                sliceRb.AddForce(forceDirection.normalized * sliceForce, ForceMode.Impulse);
                ContainerSlice.Add(sliceRb.gameObject);
            }
        }

        if (currentStageIndex >= cutPoints.Length)
        {
            Invoke("EndRitual", 0.5f);
        }
    }

    private void FailRitual(string reason)
    {
        if (AnxietyManager.Instance != null) AnxietyManager.Instance.AddAnxiety(failAnxietyPenalty); //[cite: 2]
        EndRitual();
    }

    public void PauseRitual()
    {
        canCut = false;
        if (cutLightMarker != null) cutLightMarker.gameObject.SetActive(false);
    }

    public void ResumeRitual()
    {
        canCut = true;

        // Корректный возврат карты ввода ритуала и блокировка курсора
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnRitualClick;
        StopAllCoroutines();

        if (knifeObject != null)
        {
            knifeObject.transform.localPosition = initialKnifeLocalPos;
        }

        if (cutLightMarker != null) cutLightMarker.gameObject.SetActive(false);

        ToggleRitualObjects(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
    }

    private void UpdateAnxietyUI()
    {
        if (anxietyCounterText != null && AnxietyManager.Instance != null)
        {
            anxietyCounterText.text = $"Тревожность: {AnxietyManager.Instance.currentAnxiety:F1}"; //[cite: 2]
        }
    }

    public void AbortRitual() { EndRitual(); }

    private void OnDestroy()
    {
        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnRitualClick;
    }
}