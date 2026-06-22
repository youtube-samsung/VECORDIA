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
    [Tooltip("Радиус самой губки")]
    public float spongeRadius = 0.04f;
    [Tooltip("Радиус зеленой зоны (чем меньше, тем сложнее попасть)")]
    public float zoneRadius = 0.08f;
    [Tooltip("Максимальный радиус движения по тарелке")]
    public float maxMovementRadius = 0.22f;

    [Header("Физика инерции губки")]
    public float maxMoveSpeed = 4f;
    [Tooltip("Трение губки, когда игрок спокоен (высокое = послушное управление)")]
    public float baseFriction = 15f;
    [Tooltip("Трение губки при максимальной панике (низкое = мыльное, неуправляемое движение)")]
    public float panicFriction = 1.5f;

    [Header("Настройки движения Контура (Сложность)")]
    [Tooltip("Минимальная скорость движения зеленого круга")]
    public float baseZoneSpeed = 0.5f;
    [Tooltip("Максимальная скорость зеленого круга при высокой тревожности")]
    public float maxZoneSpeed = 1.5f;
    [Tooltip("Время сглаживания движения круга. Чем МЕНЬШЕ значение, тем РЕЗЧЕ и хаотичнее он дергается.")]
    public float zoneSmoothTime = 0.5f;
    [Tooltip("Дистанция до цели, при которой круг выбирает новую точку. Чем БОЛЬШЕ значение, тем хаотичнее и непредсказуемее он меняет траекторию, не долетая до старых целей.")]
    public float targetChangeTolerance = 0.04f;

    [Header("Штрафы и Баланс")]
    [Tooltip("Сколько паники капает в секунду, если мыть мимо зоны при зажатой ЛКМ")]
    public float outsideAnxietyRate = 12f;
    [Tooltip("Время прощения (в секундах) после вылета из зоны, пока штраф еще не капает")]
    public float gracePeriod = 0.4f;
    [Tooltip("Сколько суммарно секунд нужно тереть зону для победы")]
    public float totalCleanDuration = 9f;
    [Tooltip("Сколько прогресса отмывания стирается в секунду, если зажать ЛКМ и терить МИМО зеленого круга. 0 — не пачкается обратно.")]
    public float dirtyRegressRate = 0.4f;

    [Header("Звуковое сопровождение: Губка")]
    public SoundData spongeWashLoopSound;   // Звук мытья (по чистому стеклу, внутри зоны)
    public SoundData spongeScrapeLoopSound; // Скрежет (вилкой по посуде, вне зоны)
    public SoundData cleanCompleteSound;    // Звук чистой посуды при победе

    [Header("Звуковое сопровождение: Кран (Динамическая петля)")]
    public SoundData waterTapLoopSound;     // Кассета звука крана (открытие -> вода -> закрытие)
    public float tapLoopStartSeconds = 0.5f;// Старт зацикленного участка (вода льется)
    public float tapLoopEndSeconds = 3.5f;  // Конец зацикленного участка
    public float tapClipEndSeconds = 4.0f;  // Финал (кран закрыт)

    private float outOfZoneTimer = 0f;
    private Vector3 logicalPosition;
    private Vector3 currentVelocity;
    private Vector3 ritualCenter;
    private Vector3 zoneTargetPosition;
    private Vector3 zoneSmoothVelocity;

    private bool _isRitualActive = false;
    private bool _isPaused = false;
    private bool _isRitualCompleted = false;

    private float initialContourY;
    private float initialSpongeY;
    private float cleanProgress = 0f;
    private Material dirtMaterial;

    // Ссылки на аудио
    private AudioSource _activeWashSource;
    private AudioSource _activeScrapeSource;

    // Переменные для динамической петли крана
    private AudioSource _waterTapSource;
    private Coroutine _waterTapCoroutine;

    public bool IsRitualActive => _isRitualActive;

    private void OnEnable()
    {
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.OnLoopReset += ResetRitualGlobal;
            GameLoopManager.OnDeathScreamerRequested += ForceExitOnDeath;
        }
    }

    private void OnDisable()
    {
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.OnLoopReset -= ResetRitualGlobal;
            GameLoopManager.OnDeathScreamerRequested -= ForceExitOnDeath;
        }

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed -= ExitRitualManual;
        }
        StopAllWashingSounds();
        StopWaterTapSound();
    }

    private void Start()
    {
        if (activeZoneContour != null) initialContourY = activeZoneContour.position.y;
        if (spongeVisual != null) initialSpongeY = spongeVisual.position.y;

        if (dirtRenderer != null) dirtMaterial = dirtRenderer.material;
    }

    public void Interact(int stage)
    {
        if (_isRitualCompleted) return;
        if (!_isRitualActive) StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive || _isRitualCompleted) return;
        _isRitualActive = true;
        _isPaused = false;
        outOfZoneTimer = 0f;

        ritualCenter = plateTransform != null ? plateTransform.position : transform.position;

        if (activeZoneContour != null) initialContourY = activeZoneContour.position.y;
        if (spongeVisual != null) initialSpongeY = spongeVisual.position.y;

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

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed += ExitRitualManual;
            inputReader.SwitchToRitual();
        }

        // ЗАПУСК ЗВУКА КРАНА (Динамическая петля)
        if (AudioManager.Instance != null && waterTapLoopSound != null)
        {
            _waterTapSource = transform.GetComponent<AudioSource>();
            if (_waterTapSource == null) _waterTapSource = transform.gameObject.AddComponent<AudioSource>();

            _waterTapCoroutine = AudioManager.Instance.StartDynamicLoop(_waterTapSource, waterTapLoopSound, tapLoopStartSeconds, tapLoopEndSeconds);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!_isRitualActive || _isPaused) return;

        HandleInertiaMovement();
        HandleZoneMovement();
        HandleWashingAudioLogic();

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
        if (flatRight.sqrMagnitude > 0.001f) flatRight.Normalize();

        Vector3 flatUp = cameraHandler.playerCamera.transform.up;
        flatUp.y = 0f;
        if (flatUp.sqrMagnitude <= 0.001f)
        {
            flatUp = cameraHandler.playerCamera.transform.forward;
            flatUp.y = 0f;
        }
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

        if (distToTarget < targetChangeTolerance)
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
        bool isInside = IsSpongeInsideZone();

        if (isInside)
        {
            outOfZoneTimer = 0f;
            cleanProgress += Time.deltaTime;
            cleanProgress = Mathf.Clamp(cleanProgress, 0f, totalCleanDuration);

            UpdateDirtVisual();

            if (cleanProgress >= totalCleanDuration)
            {
                CompleteRitual();
            }
        }
        else
        {
            outOfZoneTimer += Time.deltaTime;
            if (outOfZoneTimer > gracePeriod)
            {
                if (AnxietyManager.Instance != null)
                {
                    AnxietyManager.Instance.AddPenalty(outsideAnxietyRate * Time.deltaTime);
                }

                if (dirtyRegressRate > 0f && cleanProgress > 0f)
                {
                    cleanProgress -= dirtyRegressRate * Time.deltaTime;
                    cleanProgress = Mathf.Clamp(cleanProgress, 0f, totalCleanDuration);

                    UpdateDirtVisual();
                }
            }
        }
    }

    private bool IsSpongeInsideZone()
    {
        if (activeZoneContour == null) return false;
        float dist = Vector2.Distance(
            new Vector2(logicalPosition.x, logicalPosition.z),
            new Vector2(activeZoneContour.position.x, activeZoneContour.position.z)
        );
        return dist <= zoneRadius;
    }

    private void HandleWashingAudioLogic()
    {
        if (inputReader.IsRitualClickHeld && !_isPaused && AudioManager.Instance != null)
        {
            bool insideZone = IsSpongeInsideZone();
            Vector3 soundPos = spongeVisual != null ? spongeVisual.position : transform.position;

            if (insideZone)
            {
                if (_activeWashSource == null && spongeWashLoopSound != null)
                {
                    _activeWashSource = AudioManager.Instance.PlayLoopingSound3D(spongeWashLoopSound, soundPos);
                }
                if (_activeScrapeSource != null)
                {
                    AudioManager.Instance.StopLoopingSound(_activeScrapeSource);
                    _activeScrapeSource = null;
                }
            }
            else
            {
                if (_activeWashSource != null)
                {
                    AudioManager.Instance.StopLoopingSound(_activeWashSource);
                    _activeWashSource = null;
                }
                if (_activeScrapeSource == null && spongeScrapeLoopSound != null)
                {
                    _activeScrapeSource = AudioManager.Instance.PlayLoopingSound3D(spongeScrapeLoopSound, soundPos);
                }
            }

            if (_activeWashSource != null) _activeWashSource.transform.position = soundPos;
            if (_activeScrapeSource != null) _activeScrapeSource.transform.position = soundPos;
        }
        else
        {
            StopAllWashingSounds();
        }
    }

    private void UpdateDirtVisual()
    {
        if (dirtMaterial == null) return;

        float currentDirtAlpha = Mathf.Clamp01(1f - (cleanProgress / totalCleanDuration));
        Color color = dirtMaterial.color;
        color.a = currentDirtAlpha;
        dirtMaterial.color = color;
    }

    private void CompleteRitual()
    {
        _isRitualCompleted = true;

        if (AudioManager.Instance != null && cleanCompleteSound != null)
        {
            AudioManager.Instance.PlaySound2D(cleanCompleteSound);
        }

        if (ritualActivator != null)
            ritualActivator.enabled = false;

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.RegisterRitualComplete();

        EndRitual();
    }

    private void ExitRitualManual()
    {
        if (_isRitualActive) EndRitual();
    }

    private void ForceExitOnDeath()
    {
        if (_isRitualActive) EndRitual();
    }

    private void StopAllWashingSounds()
    {
        if (AudioManager.Instance == null) return;

        if (_activeWashSource != null)
        {
            AudioManager.Instance.StopLoopingSound(_activeWashSource);
            _activeWashSource = null;
        }
        if (_activeScrapeSource != null)
        {
            AudioManager.Instance.StopLoopingSound(_activeScrapeSource);
            _activeScrapeSource = null;
        }
    }

    private void StopWaterTapSound()
    {
        if (_waterTapSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopDynamicLoop(_waterTapCoroutine, _waterTapSource, tapLoopEndSeconds, tapClipEndSeconds);
            _waterTapCoroutine = null;
        }
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;
        _isPaused = false;

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed -= ExitRitualManual;
        }

        if (activeZoneContour != null) activeZoneContour.gameObject.SetActive(false);
        if (cameraHandler != null) cameraHandler.ExitRitualMode();

        // Останавливаем мыло/скрежет и запускаем звук закрытия крана
        StopAllWashingSounds();
        StopWaterTapSound();

        if (ritualActivator != null)
        {
            if (_isRitualCompleted)
            {
                ritualActivator.enabled = false;
            }
            else
            {
                ritualActivator.RitualFinished();
            }
        }

        if (inputReader != null) inputReader.SwitchToGameplay();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void AbortRitual() { EndRitual(); }

    public void PauseRitual()
    {
        _isPaused = true;
        currentVelocity = Vector3.zero;
        StopAllWashingSounds();
        if (_waterTapSource != null && _waterTapSource.isPlaying) _waterTapSource.Pause();
    }

    public void ResumeRitual()
    {
        _isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
        if (_waterTapSource != null) _waterTapSource.UnPause();
    }

    private void ResetRitualGlobal()
    {
        if (_isRitualActive) EndRitual();

        _isRitualCompleted = false;
        cleanProgress = 0f;
        outOfZoneTimer = 0f;

        UpdateDirtVisual();

        if (ritualActivator != null)
            ritualActivator.enabled = true;
    }
}