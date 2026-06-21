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

    private int[] soilDegrees = new int[360];
    private int successfullyWateredAngles = 0;
    private float currentCooldown = 0f;

    private bool _isRitualActive = false;
    private bool _isPaused = false;
    private bool _isRitualCompleted = false; 

    public bool IsRitualActive => _isRitualActive;

    private void OnEnable()
    {

        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.OnLoopReset += ResetRitualGlobal;
        }
    }

    private void OnDisable()
    {
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.OnLoopReset -= ResetRitualGlobal;
        }

        if (inputReader != null)
        {
            inputReader.OnRitualInteractPerformed -= ExitRitualManual;
        }
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
        }


        if (enterRitualThought != null && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowThought(enterRitualThought);
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
    }

    private void SprayWater(int centerAngle)
    {
        if (sprayParticles != null) sprayParticles.Play();

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

        if (ritualActivator != null)
            ritualActivator.gameObject.SetActive(false);

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

        if (ritualActivator != null && !_isRitualCompleted)
            ritualActivator.RitualFinished(); 


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
            ritualActivator.gameObject.SetActive(true); 

        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees); 
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

    public void AbortRitual() { EndRitual(); }
}