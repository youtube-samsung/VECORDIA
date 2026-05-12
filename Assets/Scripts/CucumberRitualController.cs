using UnityEngine;
using System.Collections;
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
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
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

    private void OnDestroy()
    {
        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnRitualClick;
    }

    public void Interact(int stage)
    {
        if (!_isRitualActive) StartRitual();
    }

    private void Update()
    {
        if (!_isRitualActive) return;

        UpdateAnxietyUI();

        if (isAnimatingCut) return;

        float mouseXDelta = inputReader.RitualLookValue.x * knifeMovementSpeed * Time.deltaTime;

        float currentTremorAmp = baseTremorAmplitude;
        if (AnxietyManager.Instance != null)
        {
            float anxietyIntensity = AnxietyManager.Instance.GetTremorIntensity();
            currentTremorAmp += maxAnxietyTremorAmplitude * anxietyIntensity;
        }

        float noise = (Mathf.PerlinNoise(Time.time * tremorSpeed, 0f) - 0.5f) * 2f;
        float tremorOffset = noise * currentTremorAmp * Time.deltaTime;

        Vector3 newKnifePos = knifeObject.transform.position;
        newKnifePos += cameraHandler.playerCamera.transform.right * (mouseXDelta + tremorOffset);
        knifeObject.transform.position = newKnifePos;

        DrawDebugRay();
    }

    private void OnRitualClick()
    {
        if (!_isRitualActive || !canCut || isAnimatingCut) return;
        StartCoroutine(KnifeSliceAnimation());
    }

    private IEnumerator KnifeSliceAnimation()
    {
        isAnimatingCut = true;
        canCut = false;

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
        canCut = true;
    }

    private void ProcessCutImpact(Vector3 originalPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(originalPos, Vector3.down, out hit, 1f))
        {
            if (currentStageIndex >= cutPoints.Length) return;

            Transform targetCutPoint = cutPoints[currentStageIndex].transform;
            float distanceToTarget = Vector3.Distance(hit.point, targetCutPoint.position);

            if (distanceToTarget > maxCutDistance)
            {
                FailRitual("Слишком далеко от цели!");
                return;
            }

            float missFactor = Mathf.InverseLerp(0, maxCutDistance, distanceToTarget);

            if (missFactor > 0 && AnxietyManager.Instance != null)
            {
                float penalty = missFactor * anxietyPenaltyMultiplier;
                AnxietyManager.Instance.AddAnxiety(penalty);
                Debug.Log($"Рез с помаркой. Штраф: +{penalty:F2}");
            }

            PerformCut();
        }
        else
        {
            FailRitual("Нож ударил мимо доски!");
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
            Invoke("EndRitual", 0.5f);
        }
    }

    private void FailRitual(string reason)
    {
        Debug.LogWarning($"РИТУАЛ ПРОВАЛЕН: {reason}");

        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.AddAnxiety(failAnxietyPenalty);
        }

        EndRitual();
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
            anxietyCounterText.text = $"Тревожность: {AnxietyManager.Instance.currentAnxiety:F1}";
        }
    }

    public void AbortRitual() { EndRitual(); }

    private void DrawDebugRay()
    {
        RaycastHit hit;
        Vector3 rayStart = knifeObject.transform.position;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1f))
        {
            Color rayColor = Color.grey;
            if (currentStageIndex < cutPoints.Length)
            {
                float dist = Vector3.Distance(hit.point, cutPoints[currentStageIndex].transform.position);
                if (dist <= cuttingTolerance) rayColor = Color.green;
                else if (dist <= maxCutDistance) rayColor = Color.yellow;
                else rayColor = Color.red;
            }
            Debug.DrawRay(rayStart, Vector3.down * hit.distance, rayColor);
        }
    }
}