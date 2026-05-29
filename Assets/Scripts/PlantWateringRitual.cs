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

    [Header("Настройки полива")]
    public int sprayAngleWidth = 45;
    public float sprayCooldown = 0.5f;
    public float baseRotationSpeed = 40f;
    public float maxRotationSpeed = 100f;
    public float overwaterAnxietyPenalty = 0.5f;
    public float requiredWateredPercentage = 85f;
    public float interruptionPercentage = 50f;

    public event System.Action OnInterruptionRequested;

    private int[] soilDegrees = new int[360];
    private int successfullyWateredAngles = 0;
    private float currentCooldown = 0f;
    private bool _isRitualActive = false;
    private bool _isPaused = false;
    private bool hasTriggeredInterruption = false;

    public bool IsRitualActive => _isRitualActive;

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
        if (!_isRitualActive) StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        _isPaused = false;
        currentCooldown = 0f;
        successfullyWateredAngles = 0;
        hasTriggeredInterruption = false;

        System.Array.Clear(soilDegrees, 0, soilDegrees.Length);

        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees);
            meshGenerator.TogglePreview(true);
        }

        if (ritualActivator != null) ritualActivator.HidePrompt();
        if (pulverizerObject != null) pulverizerObject.SetActive(true);
        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);
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
    }

    private void SprayWater(int centerAngle)
    {
        if (sprayParticles != null)
        {
            sprayParticles.Play();
        }

        int halfWidth = sprayAngleWidth / 2;
        int overwateredDegreesInThisSpray = 0;

        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            int angle = (centerAngle + i + 360) % 360;

            if (soilDegrees[angle] < 3)
            {
                soilDegrees[angle]++;

                if (soilDegrees[angle] == 2)
                {
                    successfullyWateredAngles++;
                }
                if (soilDegrees[angle] == 3)
                {
                    overwateredDegreesInThisSpray++;
                }
            }
        }

        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees);
        }

        if (overwateredDegreesInThisSpray > 0 && AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.AddAnxiety(overwaterAnxietyPenalty * overwateredDegreesInThisSpray);
        }

        float currentProgressPercent = ((float)successfullyWateredAngles / 360f) * 100f;

        if (!hasTriggeredInterruption && currentProgressPercent >= interruptionPercentage)
        {
            hasTriggeredInterruption = true;
            // OnInterruptionRequested?.Invoke();
        }

        if (currentProgressPercent >= requiredWateredPercentage)
        {
            EndRitual();
        }
    }

    public void PauseRitual()
    {
        _isPaused = true;
        if (meshGenerator != null) meshGenerator.TogglePreview(false);
    }

    public void ResumeRitual()
    {
        _isPaused = false;
        if (meshGenerator != null) meshGenerator.TogglePreview(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (inputReader != null) inputReader.SwitchToRitual();
    }

    public void EndRitual()
    {
        _isRitualActive = false;
        _isPaused = false;

        if (pulverizerObject != null) pulverizerObject.SetActive(false);
        if (meshGenerator != null) meshGenerator.TogglePreview(false);

        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }

    public void AbortRitual() { EndRitual(); }
}