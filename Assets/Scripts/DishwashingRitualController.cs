using UnityEngine;
using System.Collections;

public class DishwashingZoneRitual : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;
    public Transform spongeVisual;
    public Transform ritualCameraTarget;
    public Transform activeZoneContour;
    public Renderer dirtRenderer;
    public Transform plateTransform;

    [Header("Настройки размеров")]
    public float spongeRadius = 0.04f;
    public float zoneRadius = 0.08f;
    public float maxMovementRadius = 0.22f;

    [Header("Физика инерции губки")]
    public float maxMoveSpeed = 4f;
    public float baseFriction = 15f;
    public float panicFriction = 1.5f;

    [Header("Настройки движения Контура")]
    public float baseZoneSpeed = 0.5f;
    public float maxZoneSpeed = 1.5f;
    public float zoneSmoothTime = 0.5f;

    [Header("Штрафы и Баланс")]
    public float outsideAnxietyRate = 12f;
    public float gracePeriod = 0.4f;
    public float totalCleanDuration = 9f;
    public float interruptionThresholdAlpha = 0.7f;

    public event System.Action OnInterruptionRequested;

    private float outOfZoneTimer = 0f;
    private Vector3 logicalPosition;
    private Vector3 currentVelocity;
    private Vector3 ritualCenter;
    private Vector3 zoneTargetPosition;
    private Vector3 zoneSmoothVelocity;

    private bool _isRitualActive = false;
    private bool _isPaused = false;

    private float initialContourY;
    private float initialSpongeY;

    private float cleanProgress = 0f;
    private bool hasTriggeredInterruption = false;

    public bool IsRitualActive => _isRitualActive;

    private void Start()
    {
        if (activeZoneContour != null)
        {
            initialContourY = activeZoneContour.position.y;
        }
        if (spongeVisual != null)
        {
            initialSpongeY = spongeVisual.position.y;
        }
    }

    public void Interact(int stage)
    {
        if (!_isRitualActive) StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        _isPaused = false;
        outOfZoneTimer = 0f;

        ritualCenter = plateTransform != null ? plateTransform.position : transform.position;

        if (activeZoneContour != null)
        {
            initialContourY = activeZoneContour.position.y;
        }
        if (spongeVisual != null)
        {
            initialSpongeY = spongeVisual.position.y;
        }

        logicalPosition = new Vector3(spongeVisual.position.x, initialSpongeY, spongeVisual.position.z);
        spongeVisual.position = logicalPosition;

        if (ritualActivator != null) ritualActivator.HidePrompt();

        UpdateDirtVisual();

        if (activeZoneContour != null)
        {
            activeZoneContour.gameObject.SetActive(true);
            activeZoneContour.position = new Vector3(activeZoneContour.position.x, initialContourY, activeZoneContour.position.z);
            PickNewZoneTarget();
        }

        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);
    }

    private void Update()
    {
        if (!_isRitualActive || _isPaused) return;

        HandleInertiaMovement();
        HandleZoneMovement();

        if (inputReader.IsRitualClickHeld)
        {
            ProcessZoneProgress();
        }
        else
        {
            outOfZoneTimer = 0f;
        }
    }

    private void HandleInertiaMovement()
    {
        Vector2 input = inputReader.IsRitualClickHeld ? inputReader.RitualLookValue : Vector2.zero;

        Vector3 flatRight = cameraHandler.playerCamera.transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 flatUp = cameraHandler.playerCamera.transform.up;
        flatUp.y = 0f;
        flatUp.Normalize();

        Vector3 targetVel = (flatRight * input.x + flatUp * input.y) * maxMoveSpeed;

        float anxietyPercent = AnxietyManager.Instance != null ? AnxietyManager.Instance.GetTremorIntensity() : 0f;
        float currentFriction = Mathf.Lerp(baseFriction, panicFriction, anxietyPercent);

        currentVelocity = Vector3.Lerp(currentVelocity, targetVel, currentFriction * Time.deltaTime);
        Vector3 nextPosition = logicalPosition + currentVelocity * Time.deltaTime;
        nextPosition.y = initialSpongeY;

        float distanceFromCenter = Vector2.Distance(new Vector2(nextPosition.x, nextPosition.z), new Vector2(ritualCenter.x, ritualCenter.z));
        if (distanceFromCenter <= maxMovementRadius)
        {
            logicalPosition = nextPosition;
        }
        else
        {
            Vector3 directionFromCenter = (nextPosition - ritualCenter).normalized;
            directionFromCenter.y = 0f;
            logicalPosition = ritualCenter + directionFromCenter.normalized * maxMovementRadius;
            logicalPosition.y = initialSpongeY;
            currentVelocity = Vector3.zero;
        }

        spongeVisual.position = logicalPosition;
    }

    private void HandleZoneMovement()
    {
        if (activeZoneContour == null) return;

        float anxietyPercent = AnxietyManager.Instance != null ? AnxietyManager.Instance.GetTremorIntensity() : 0f;
        float currentMaxSpeed = Mathf.Lerp(baseZoneSpeed, maxZoneSpeed, anxietyPercent);

        activeZoneContour.position = Vector3.SmoothDamp(
            activeZoneContour.position,
            zoneTargetPosition,
            ref zoneSmoothVelocity,
            zoneSmoothTime,
            currentMaxSpeed
        );

        Vector3 clampedPos = activeZoneContour.position;
        clampedPos.y = initialContourY;
        activeZoneContour.position = clampedPos;

        float distToTarget = Vector2.Distance(
            new Vector2(activeZoneContour.position.x, activeZoneContour.position.z),
            new Vector2(zoneTargetPosition.x, zoneTargetPosition.z)
        );

        if (distToTarget < 0.04f)
        {
            PickNewZoneTarget();
        }
    }

    private void PickNewZoneTarget()
    {
        float safeRadius = maxMovementRadius - zoneRadius;
        if (safeRadius < 0f) safeRadius = maxMovementRadius * 0.5f;

        Vector2 randomCircle = Random.insideUnitCircle * safeRadius;

        zoneTargetPosition = new Vector3(
            ritualCenter.x + randomCircle.x,
            initialContourY,
            ritualCenter.z + randomCircle.y
        );
    }

    private void ProcessZoneProgress()
    {
        float dist = Vector2.Distance(
            new Vector2(logicalPosition.x, logicalPosition.z),
            new Vector2(activeZoneContour.position.x, activeZoneContour.position.z)
        );

        bool isInside = (dist + spongeRadius) <= zoneRadius;

        if (isInside)
        {
            outOfZoneTimer = 0f;
            cleanProgress += Time.deltaTime;

            UpdateDirtVisual();
            CheckInterruptionTrigger();

            if (cleanProgress >= totalCleanDuration)
            {
                EndRitual();
            }
        }
        else
        {
            outOfZoneTimer += Time.deltaTime;
            if (outOfZoneTimer > gracePeriod)
            {
                AnxietyManager.Instance.AddAnxiety(outsideAnxietyRate * Time.deltaTime);
            }
        }
    }

    private void UpdateDirtVisual()
    {
        if (dirtRenderer == null) return;

        float currentDirtAlpha = Mathf.Clamp01(1f - (cleanProgress / totalCleanDuration));
        Color color = dirtRenderer.material.color;
        color.a = currentDirtAlpha;
        dirtRenderer.material.color = color;
    }

    private void CheckInterruptionTrigger()
    {
        if (hasTriggeredInterruption) return;

        float currentDirtAlpha = Mathf.Clamp01(1f - (cleanProgress / totalCleanDuration));
        if (currentDirtAlpha <= interruptionThresholdAlpha)
        {
            hasTriggeredInterruption = true;
            // OnInterruptionRequested?.Invoke();
        }
    }

    public void PauseRitual()
    {
        _isPaused = true;
        currentVelocity = Vector3.zero;
    }

    public void ResumeRitual()
    {
        _isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
    }

    public void EndRitual()
    {
        _isRitualActive = false;
        _isPaused = false;
        if (cleanProgress >= totalCleanDuration)
        {
            cleanProgress = 0f;
            hasTriggeredInterruption = false;
        }
        if (activeZoneContour != null) activeZoneContour.gameObject.SetActive(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }

    public void AbortRitual()
    {
        _isRitualActive = false;
        _isPaused = false;
        if (activeZoneContour != null) activeZoneContour.gameObject.SetActive(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }
}