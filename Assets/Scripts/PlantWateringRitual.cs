using UnityEngine;
using System.Collections;

public class PlantWateringRitual : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;
    public SoilMeshGenerator meshGenerator;
    public Transform ritualCameraTarget;

    [Header("Объекты геймплея")]
    public GameObject pulverizerObject;
    public ParticleSystem sprayParticles;

    [Header("Мысли (Thought Data)")]
    public ThoughtData enterRitualThought;
    public ThoughtData completeRitualThought;

    [Header("Настройки полива")]
    public int sprayAngleWidth = 45;
    public float sprayCooldown = 0.5f;
    public float baseRotationSpeed = 40f;
    public float maxRotationSpeed = 100f;
    public float overwaterAnxietyPenalty = 0.5f;
    public float requiredWateredPercentage = 85f;

    [Header("Звуки")]
    public SoundData spraySound;
    public SoundData ritualCompleteSound;
    [Space(5)]
    public SoundData potRotateLoopSound;
    public float loopStartSeconds = 0.5f;
    public float loopEndSeconds = 3.5f;
    public float clipEndSeconds = 4.0f;

    private int[] soilDegrees = new int[360];
    private int successfullyWateredAngles = 0;
    private float currentCooldown = 0f;

    private bool _isRitualActive = false;
    private bool _isPaused = false;
    private bool _isRitualCompleted = false;

    public bool IsRitualActive => _isRitualActive;

    private AudioSource _potAudioSource;
    private Coroutine _potLoopCoroutine;

    private void OnEnable()
    {
        // ИСПРАВЛЕНИЕ: Для статических эвентов проверка Instance НЕ НУЖНА.
        // Теперь подписка сработает со 100% гарантией при любом порядке инициализации кадров.
        GameLoopManager.OnLoopReset += ResetRitualGlobal;
        GameLoopManager.OnDeathScreamerRequested += ForceExitOnDeath;
    }

    private void OnDisable()
    {
        GameLoopManager.OnLoopReset -= ResetRitualGlobal;
        GameLoopManager.OnDeathScreamerRequested -= ForceExitOnDeath;

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed -= ExitRitualManual;
        }

        StopPotRotationSound();
    }

    private void Start()
    {
        if (meshGenerator != null)
        {
            meshGenerator.GenerateMesh(sprayAngleWidth);
        }

        if (pulverizerObject != null) pulverizerObject.SetActive(false);
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
        _isPaused = false;
        currentCooldown = 0f;

        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees);
            meshGenerator.TogglePreview(true);
        }

        if (ritualActivator != null) ritualActivator.HidePrompt();
        if (pulverizerObject != null) pulverizerObject.SetActive(true);
        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed += ExitRitualManual;
            inputReader.SwitchToRitual();
        }

        if (enterRitualThought != null && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowThought(enterRitualThought);
        }

        if (AudioManager.Instance != null && potRotateLoopSound != null)
        {
            _potAudioSource = transform.GetComponent<AudioSource>();
            if (_potAudioSource == null) _potAudioSource = transform.gameObject.AddComponent<AudioSource>();

            _potLoopCoroutine = AudioManager.Instance.StartDynamicLoop(_potAudioSource, potRotateLoopSound, loopStartSeconds, loopEndSeconds);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!_isRitualActive || _isPaused) return;

        HandleRotation();

        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        if (inputReader.IsRitualClickHeld && currentCooldown <= 0f)
        {
            float currentYRotation = transform.eulerAngles.y;
            int centerAngle = Mathf.RoundToInt(360f - currentYRotation) % 360;
            SprayWater(centerAngle);
            currentCooldown = sprayCooldown;
        }
    }

    private void HandleRotation()
    {
        float anxiety = AnxietyManager.Instance != null ? AnxietyManager.Instance.GetTremorIntensity() : 0f;
        float currentSpeed = Mathf.Lerp(baseRotationSpeed, maxRotationSpeed, anxiety);

        if (anxiety > 0.1f)
        {
            float noise = (Mathf.PerlinNoise(Time.time * 3f, 0f) - 0.4f) * 3f;
            currentSpeed += currentSpeed * noise * anxiety;
        }

        transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);

        if (_potAudioSource != null && _potAudioSource.isPlaying)
        {
            _potAudioSource.pitch = 1f + (anxiety * 0.15f);
        }
    }

    private void SprayWater(int centerAngle)
    {
        if (sprayParticles != null) sprayParticles.Play();

        if (AudioManager.Instance != null && spraySound != null)
        {
            AudioManager.Instance.PlaySound3D(spraySound, pulverizerObject != null ? pulverizerObject.transform.position : transform.position);
        }

        int halfWidth = sprayAngleWidth / 2;
        int overwateredDegreesInThisSpray = 0;

        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            int angle = (centerAngle + i + 360) % 360;

            if (soilDegrees[angle] < 3)
            {
                soilDegrees[angle]++;

                if (soilDegrees[angle] == 2) successfullyWateredAngles++;
                if (soilDegrees[angle] == 3) overwateredDegreesInThisSpray++;
            }
        }

        if (meshGenerator != null) meshGenerator.UpdateColors(soilDegrees);

        if (overwateredDegreesInThisSpray > 0 && AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.AddPenalty(overwaterAnxietyPenalty * overwateredDegreesInThisSpray);
        }

        float currentProgressPercent = ((float)successfullyWateredAngles / 360f) * 100f;

        if (currentProgressPercent >= requiredWateredPercentage)
        {
            CompleteRitual();
        }
    }

    private void CompleteRitual()
    {
        _isRitualCompleted = true;

        if (AudioManager.Instance != null && ritualCompleteSound != null)
        {
            AudioManager.Instance.PlaySound2D(ritualCompleteSound);
        }

        if (ritualActivator != null)
            ritualActivator.enabled = false; // Отключаем только компонент, чтобы объект жил

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.RegisterRitualComplete();

        if (completeRitualThought != null && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowThought(completeRitualThought);
        }

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

    private void StopPotRotationSound()
    {
        if (_potAudioSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopDynamicLoop(_potLoopCoroutine, _potAudioSource, loopEndSeconds, clipEndSeconds);
            _potLoopCoroutine = null;
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

        if (pulverizerObject != null) pulverizerObject.SetActive(false);
        if (meshGenerator != null) meshGenerator.TogglePreview(false);

        if (cameraHandler != null) cameraHandler.ExitRitualMode();

        StopPotRotationSound();

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

    private void ResetRitualGlobal()
    {
        if (_isRitualActive) EndRitual();

        _isRitualCompleted = false;
        successfullyWateredAngles = 0;
        System.Array.Clear(soilDegrees, 0, soilDegrees.Length);

        if (ritualActivator != null)
            ritualActivator.enabled = true; // Оживляем триггер

        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees);
            meshGenerator.TogglePreview(false); // ИСПРАВЛЕНИЕ: Жестко выключаем маску при рестарте
        }
    }

    public void PauseRitual()
    {
        _isPaused = true;
        if (meshGenerator != null) meshGenerator.TogglePreview(false);
        if (_potAudioSource != null && _potAudioSource.isPlaying) _potAudioSource.Pause();
    }

    public void ResumeRitual()
    {
        _isPaused = false;
        if (meshGenerator != null) meshGenerator.TogglePreview(true);
        if (_potAudioSource != null) _potAudioSource.UnPause();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
    }

    public void AbortRitual() { EndRitual(); }
}