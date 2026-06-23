using UnityEngine;
using System;
using TMPro;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    [Header("Ссылки на сцену")]
    public Transform playerTransform;
    public Transform bedSpawnPoint;
    public TextMeshProUGUI clockText;

    [Header("Настройки условий")]
    public int totalRitualsRequired = 4;

    // Глобальные события
    public static event Action OnDeathScreamerRequested;
    public static event Action OnLoopReset;
    public static event Action OnAllRitualsCompleted; 

    private int _completedRituals = 0;
    private bool _isLoopEnding = false;
    private bool _isFinalPhaseStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.OnMentalBreakdown += TriggerDeath;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        StartNewLoop();
    }

    private void OnDestroy()
    {
        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.OnMentalBreakdown -= TriggerDeath;
    }

    private void Update()
    {
        if (_isLoopEnding || _isFinalPhaseStarted || AnxietyManager.Instance == null) return;
        UpdateClockUI(AnxietyManager.Instance.CurrentTotalAnxiety);
    }

    public void RegisterRitualComplete()
    {
        if (_isLoopEnding || _isFinalPhaseStarted) return;
        _completedRituals++;

        Debug.Log($"[GameLoopManager] Ритуал выполнен: {_completedRituals}/{totalRitualsRequired}");

        if (_completedRituals >= totalRitualsRequired)
        {
            _isFinalPhaseStarted = true;
            OnAllRitualsCompleted?.Invoke(); // Просто уведомляем системы
        }
    }

    private void TriggerDeath()
    {
        if (_isLoopEnding || _isFinalPhaseStarted) return;
        _isLoopEnding = true;

        SessionProgress.loopCount++;
        OnDeathScreamerRequested?.Invoke();
    }

    public void StartNewLoop()
    {
        _isLoopEnding = false;
        _isFinalPhaseStarted = false;
        _completedRituals = 0;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        if (playerTransform != null && bedSpawnPoint != null)
        {
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = bedSpawnPoint.position;
            playerTransform.rotation = bedSpawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.ResetAnxiety();

        OnLoopReset?.Invoke();
    }

    private void UpdateClockUI(float currentAnxiety)
    {
        int startMinutesTotal = 22 * 60 + 53;
        float minutesPassed = (currentAnxiety / 100f) * 7f;
        int currentMinutesTotal = startMinutesTotal + Mathf.FloorToInt(minutesPassed);

        int h = (currentMinutesTotal / 60) % 24;
        int m = currentMinutesTotal % 60;

        if (clockText != null) clockText.text = $"{h:00}:{m:00}";
    }
}