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

    [Header("Финал Ритуала (Салат)")]
    public Transform boardTransform;
    public Collider boardCollider;
    public PhysicsMaterial slipperyMaterial;

    public GameObject emptyPlate;
    public Transform emptyPlateTargetPos;
    public GameObject fullSalad;

    [Tooltip("На сколько поднимается доска вверх. Например: 0.15")]
    public float boardLiftHeight = 0.15f;
    [Tooltip("Укажи углы наклона по осям X, Y, Z.")]
    public Vector3 boardTiltRotation = new Vector3(0f, 0f, 45f);
    public float slideWaitTime = 2.5f;

    [Header("Тайминги Финала (Анимация)")]
    public float plateSlideDuration = 0.6f;
    public float boardLiftDuration = 0.3f;
    public float boardTiltDuration = 0.4f;
    public float boardReturnDuration = 0.4f;

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
    private bool _isRitualCompleted = false;
    public bool IsRitualActive => _isRitualActive;

    private bool canCut = false;
    private bool isAnimatingCut = false;
    private Vector3 initialKnifeLocalPos;

    private PhysicsMaterial originalBoardMaterial;
    private Vector3 emptyPlateStartPos;
    private Quaternion originalBoardRotation;
    private Vector3 originalBoardPosition;

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

        if (boardTransform != null)
        {
            originalBoardRotation = boardTransform.localRotation;
            originalBoardPosition = boardTransform.localPosition;
        }
        if (boardCollider != null) originalBoardMaterial = boardCollider.sharedMaterial;
        if (emptyPlate != null) emptyPlateStartPos = emptyPlate.transform.position;
        if (fullSalad != null) fullSalad.SetActive(false);

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
        if (!_isRitualActive && !_isRitualCompleted)
            StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive || _isRitualCompleted) return;

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

            CucumberBoardController boardController = Object.FindFirstObjectByType<CucumberBoardController>();

            if (distanceToTarget > maxCutDistance)
            {
                if (AudioManager.Instance != null && knifeMissSound != null)
                    AudioManager.Instance.PlaySound3D(knifeMissSound, knifeObject.transform.position);

                if (boardController != null)
                {
                    boardController.RegisterNewScar(hit.point, 1f);
                }

                if (AnxietyManager.Instance != null)
                    AnxietyManager.Instance.AddPenalty(failAnxietyPenalty);

                return;
            }

            if (AudioManager.Instance != null && sliceSound != null)
                AudioManager.Instance.PlaySound3D(sliceSound, knifeObject.transform.position);

            float missFactor = Mathf.InverseLerp(0, maxCutDistance, distanceToTarget);

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
            canCut = false;
            if (cutLightMarker != null) cutLightMarker.gameObject.SetActive(false);

            StartCoroutine(FinalizeSaladRoutine());
        }
    }

    private IEnumerator FinalizeSaladRoutine()
    {
        if (knifeObject != null) knifeObject.SetActive(false);


        if (emptyPlate != null && emptyPlateTargetPos != null)
        {
            float t = 0;
            while (t < plateSlideDuration)
            {
                float easeOut = 1f - Mathf.Pow(1f - (t / plateSlideDuration), 3f);
                emptyPlate.transform.position = Vector3.Lerp(emptyPlateStartPos, emptyPlateTargetPos.position, easeOut);
                t += Time.deltaTime;
                yield return null;
            }
            emptyPlate.transform.position = emptyPlateTargetPos.position;
        }

        if (boardCollider != null && slipperyMaterial != null)
        {
            boardCollider.sharedMaterial = slipperyMaterial;
        }


        if (boardTransform != null)
        {
            Vector3 targetPos = originalBoardPosition + Vector3.up * boardLiftHeight;
            float t = 0;
            while (t < boardLiftDuration)
            {
                boardTransform.localPosition = Vector3.Lerp(originalBoardPosition, targetPos, t / boardLiftDuration);
                t += Time.deltaTime;
                yield return null;
            }
            boardTransform.localPosition = targetPos;
        }


        if (boardTransform != null)
        {
            Quaternion targetRot = originalBoardRotation * Quaternion.Euler(boardTiltRotation);
            float t = 0;
            while (t < boardTiltDuration)
            {
                boardTransform.localRotation = Quaternion.Slerp(originalBoardRotation, targetRot, t / boardTiltDuration);
                t += Time.deltaTime;
                yield return null;
            }
            boardTransform.localRotation = targetRot;
        }


        yield return new WaitForSeconds(slideWaitTime);


        foreach (var mem in sliceMemories)
        {
            if (mem.transform != null)
            {
                mem.transform.gameObject.SetActive(false);
            }
        }


        if (boardTransform != null)
        {
            float t = 0;
            while (t < boardReturnDuration)
            {
                boardTransform.localRotation = Quaternion.Slerp(boardTransform.localRotation, originalBoardRotation, t / boardReturnDuration);
                t += Time.deltaTime;
                yield return null;
            }
            boardTransform.localRotation = originalBoardRotation;
        }


        if (boardTransform != null)
        {
            float t = 0;
            while (t < boardReturnDuration)
            {
                boardTransform.localPosition = Vector3.Lerp(boardTransform.localPosition, originalBoardPosition, t / boardReturnDuration);
                t += Time.deltaTime;
                yield return null;
            }
            boardTransform.localPosition = originalBoardPosition;
        }

        if (emptyPlate != null) emptyPlate.SetActive(false);
        if (fullSalad != null) fullSalad.SetActive(true);

        _isRitualCompleted = true;
        if (ritualActivator != null) ritualActivator.gameObject.SetActive(false);

        if (GameLoopManager.Instance != null) GameLoopManager.Instance.RegisterRitualComplete();
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
        if (ritualActivator != null && !_isRitualCompleted) ritualActivator.RitualFinished();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ResetRitualGlobal()
    {
        // Силентный сброс без вызова EndRitual(), чтобы не ломать логику камеры смерти
        if (_isRitualActive)
        {
            _isRitualActive = false;

            if (inputReader != null)
            {
                inputReader.OnRitualClickPerformed -= OnRitualClick;
                inputReader.OnRitualInteractPerformed -= EndRitual;
            }

            StopAllCoroutines(); // Гасим корутины анимации самого ножа

            if (cameraHandler != null)
            {
                // ХИТРЫЙ ХАК: Вызываем выход, чтобы сбросить флаг isHandlingCamera в false для новой петли...
                cameraHandler.ExitRitualMode();
                // ...и ТУТ ЖЕ аппаратно тушим корутину полета камеры назад к столу, пока она не сделала ни одного кадра!
                cameraHandler.StopAllCoroutines();
            }
        }

        // Стандартный сброс переменных и объектов ритуала огурца
        currentStageIndex = 0;
        _isRitualCompleted = false;

        if (knifeObject != null)
            knifeObject.transform.localPosition = initialKnifeLocalPos;

        if (cutLightMarker != null)
            cutLightMarker.gameObject.SetActive(false);

        ToggleRitualObjects(false);

        if (ritualActivator != null)
            ritualActivator.gameObject.SetActive(true);

        foreach (var mem in sliceMemories)
        {
            if (mem.transform != null) mem.transform.gameObject.SetActive(true);

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

        if (boardTransform != null)
        {
            boardTransform.localRotation = originalBoardRotation;
            boardTransform.localPosition = originalBoardPosition;
        }
        if (boardCollider != null) boardCollider.sharedMaterial = originalBoardMaterial;

        if (emptyPlate != null)
        {
            emptyPlate.SetActive(true);
            emptyPlate.transform.position = emptyPlateStartPos;
        }
        if (fullSalad != null) fullSalad.SetActive(false);
    }

    private void ToggleRitualObjects(bool active)
    {
        if (_isRitualCompleted) return;

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