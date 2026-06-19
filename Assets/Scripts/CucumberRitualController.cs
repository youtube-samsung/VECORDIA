using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [Header("Лазерный прицел")]
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

    // --- ПАМЯТЬ ДЛЯ КУСОЧКОВ ---
    private struct SliceMemory
    {
        public Transform transform;
        public Transform originalParent;
        public Vector3 originalLocalPos;
        public Quaternion originalLocalRot;
        public Rigidbody rb;
    }
    private List<SliceMemory> sliceMemories = new List<SliceMemory>();

    private void OnEnable()
    {
        GameLoopManager.OnLoopReset += ResetRitualGlobal;
    }

    private void OnDisable()
    {
        GameLoopManager.OnLoopReset -= ResetRitualGlobal;
        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed -= OnRitualClick;
            inputReader.OnRitualInteractPerformed -= EndRitual;
        }
    }

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

        // ЗАПОМИНАЕМ ВСЕ КУСОЧКИ ОГУРЦА ПРИ СТАРТЕ СЦЕНЫ
        foreach (GameObject stage in cucumberStages)
        {
            if (stage == null) continue;
            Rigidbody[] rbs = stage.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody r in rbs)
            {
                sliceMemories.Add(new SliceMemory
                {
                    transform = r.transform,
                    originalParent = r.transform.parent,
                    originalLocalPos = r.transform.localPosition,
                    originalLocalRot = r.transform.localRotation,
                    rb = r
                });
            }
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

        _isRitualActive = true;
        canCut = true;
        isAnimatingCut = false;

        if (ritualActivator != null) ritualActivator.HidePrompt();

        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed += OnRitualClick;
            inputReader.OnRitualInteractPerformed += EndRitual;
        }

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
        Vector3 rayStart = cutLightMarker.transform.position;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            if (hit.collider.gameObject == knifeObject || hit.collider.transform.IsChildOf(knifeObject.transform)) return;

            Transform targetCutPoint = cutPoints[currentStageIndex].transform;
            float dist = Vector2.Distance(
                new Vector2(hit.point.x, hit.point.z),
                new Vector2(targetCutPoint.position.x, targetCutPoint.position.z)
            );

            if (dist <= cuttingTolerance)
            {
                cutLightMarker.color = Color.green;
                cutLightMarker.intensity = maxLightIntensity;
            }
            else if (dist <= maxCutDistance)
            {
                cutLightMarker.color = Color.yellow;
                cutLightMarker.intensity = maxLightIntensity * 0.7f;
            }
            else
            {
                cutLightMarker.color = Color.red;
                cutLightMarker.intensity = maxLightIntensity * 0.3f;
            }
        }
        else
        {
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

        Vector3 rayStart = new Vector3(cutLightMarker.transform.position.x, originalPos.y, cutLightMarker.transform.position.z);
        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1.5f))
        {
            Transform targetCutPoint = cutPoints[currentStageIndex].transform;
            float distanceToTarget = Vector2.Distance(
                new Vector2(hit.point.x, hit.point.z),
                new Vector2(targetCutPoint.position.x, targetCutPoint.position.z)
            );

            // Находим контроллер доски на сцене для регистрации шрамов
            CucumberBoardController boardController = Object.FindFirstObjectByType<CucumberBoardController>();

            // 1. КРИТИЧЕСКИЙ ПРОМАХ (Слишком далеко от точки реза)
            if (distanceToTarget > maxCutDistance)
            {
                if (AudioManager.Instance != null && knifeMissSound != null)
                    AudioManager.Instance.PlaySound3D(knifeMissSound, knifeObject.transform.position);

                // Регистрируем шрам на доске (при сильном промахе передаем мисс-фактор = 1, шрам будет минимальным)
                if (boardController != null)
                {
                    boardController.RegisterNewScar(hit.point, 1f);
                }

                if (AnxietyManager.Instance != null)
                    AnxietyManager.Instance.AddPenalty(failAnxietyPenalty);

                return;
            }

            // 2. УСПЕШНЫЙ / НЕТОЧНЫЙ СРЕЗ (Попали в допустимый диапазон)
            if (AudioManager.Instance != null && sliceSound != null)
                AudioManager.Instance.PlaySound3D(sliceSound, knifeObject.transform.position);

            float missFactor = Mathf.InverseLerp(0, maxCutDistance, distanceToTarget);

            // Оставляем шрам в любом случае, даже если срез идеальный
            if (boardController != null)
            {
                boardController.RegisterNewScar(hit.point, missFactor);
            }

            if (missFactor > 0 && AnxietyManager.Instance != null)
            {
                AnxietyManager.Instance.AddPenalty(missFactor * anxietyPenaltyMultiplier);
            }

            PerformCut();
        }
        else
        {
            // 3. УДАР ВООБЩЕ МИМО ДОСКИ
            if (AudioManager.Instance != null && knifeMissSound != null)
                AudioManager.Instance.PlaySound3D(knifeMissSound, knifeObject.transform.position);

            if (AnxietyManager.Instance != null)
                AnxietyManager.Instance.AddPenalty(failAnxietyPenalty);
        }
    }

    private void PerformCut()
    {
        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            // Физическое отделение старого куска
            Rigidbody oldSlice = cucumberStages[currentStageIndex].GetComponentInChildren<Rigidbody>();
            if (oldSlice != null) oldSlice.transform.SetParent(null);
            cucumberStages[currentStageIndex].SetActive(false);
        }

        currentStageIndex++;

        if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
        {
            cucumberStages[currentStageIndex].SetActive(true);
            Rigidbody sliceRb = cucumberStages[currentStageIndex].GetComponentInChildren<Rigidbody>();
            if (sliceRb != null)
            {
                sliceRb.transform.SetParent(null);
                Vector3 forceDirection = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.2f), Random.Range(-0.2f, 0.2f));
                sliceRb.AddForce(forceDirection.normalized * sliceForce, ForceMode.Impulse);
            }
        }

        if (currentStageIndex >= cutPoints.Length)
        {
            if (GameLoopManager.Instance != null) GameLoopManager.Instance.RegisterRitualComplete();
            Invoke("EndRitual", 0.5f);
        }
    }

    public void PauseRitual()
    {
        canCut = false;
        if (cutLightMarker != null) cutLightMarker.gameObject.SetActive(false);
    }

    public void ResumeRitual()
    {
        canCut = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed -= OnRitualClick;
            inputReader.OnRitualInteractPerformed -= EndRitual;
        }

        StopAllCoroutines();

        if (knifeObject != null)
            knifeObject.transform.localPosition = initialKnifeLocalPos;

        if (cutLightMarker != null)
            cutLightMarker.gameObject.SetActive(false);

        ToggleRitualObjects(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResetRitualGlobal()
    {
        if (_isRitualActive) EndRitual();

        currentStageIndex = 0;

        foreach (var mem in sliceMemories)
        {
            mem.transform.SetParent(mem.originalParent);
            mem.transform.localPosition = mem.originalLocalPos;
            mem.transform.localRotation = mem.originalLocalRot;

            if (mem.rb != null)
            {
                mem.rb.linearVelocity = Vector3.zero;
                mem.rb.angularVelocity = Vector3.zero;
            }
        }

        for (int i = 0; i < cucumberStages.Length; i++)
        {
            if (cucumberStages[i] != null)
                cucumberStages[i].SetActive(i == 0);
        }
    }

    private void ToggleRitualObjects(bool active)
    {
        knifeObject.SetActive(active);

        if (active)
        {
            if (cucumberStages.Length > currentStageIndex && cucumberStages[currentStageIndex] != null)
                cucumberStages[currentStageIndex].SetActive(true);
        }
    }

    private void UpdateAnxietyUI()
    {
        if (anxietyCounterText != null && AnxietyManager.Instance != null)
        {
            anxietyCounterText.text = $"Тревожность: {Mathf.FloorToInt(AnxietyManager.Instance.CurrentTotalAnxiety)}%";
        }
    }

    public void AbortRitual() { EndRitual(); }
}