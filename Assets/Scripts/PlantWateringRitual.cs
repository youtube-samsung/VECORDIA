using UnityEngine;

public class PlantWateringRitual : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;
    public SoilMeshGenerator meshGenerator;
    public Transform ritualCameraTarget;

    [Header("Настройки полива")]
    public int sprayAngleWidth = 45;
    public float sprayCooldown = 0.5f;
    public float baseRotationSpeed = 40f;
    public float overwaterAnxietyPenalty = 15f;

    private int[] soilDegrees = new int[360];
    private float currentCooldown = 0f;
    private bool _isRitualActive = false;
    public bool IsRitualActive => _isRitualActive;

    private void Start()
    {
        if (meshGenerator != null)
        {
            meshGenerator.GenerateMesh();
        }
    }

    public void Interact(int stage) { StartRitual(); }

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        currentCooldown = 0f;

        if (ritualActivator != null) ritualActivator.HidePrompt();
        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);
    }

    private void Update()
    {
        if (!_isRitualActive) return;

        HandleRotation();

        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        if (inputReader.IsRitualClickHeld && currentCooldown <= 0f)
        {
            SprayWater();
            currentCooldown = sprayCooldown;
        }
    }

    private void HandleRotation()
    {
        float currentSpeed = baseRotationSpeed;
        float anxiety = AnxietyManager.Instance.GetTremorIntensity();

        if (anxiety > 0.1f)
        {
            float noise = (Mathf.PerlinNoise(Time.time * 3f, 0f) - 0.4f) * 3f;
            currentSpeed += currentSpeed * noise * anxiety;
        }

        transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
    }

    private void SprayWater()
    {
        float currentYRotation = transform.eulerAngles.y;
        int centerAngle = Mathf.RoundToInt(360f - currentYRotation) % 360;
        int halfWidth = sprayAngleWidth / 2;
        bool overwateredThisSpray = false;

     
        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            int angle = (centerAngle + i + 360) % 360;

            if (soilDegrees[angle] < 3)
            {
                soilDegrees[angle]++;
                if (soilDegrees[angle] == 3) overwateredThisSpray = true;
            }
        }

      
        if (meshGenerator != null)
        {
            meshGenerator.UpdateColors(soilDegrees);
        }

        if (overwateredThisSpray)
        {
            AnxietyManager.Instance.AddAnxiety(overwaterAnxietyPenalty);
            Debug.LogWarning("ПЕРЕЛИВ! Образовалась лужа!");
        }

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool isPerfect = true;
        for (int i = 0; i < 360; i++)
        {
            if (soilDegrees[i] < 2)
            {
                isPerfect = false;
                break;
            }
        }
        if (isPerfect) EndRitual();
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;
        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }

    public void AbortRitual() { EndRitual(); }
}