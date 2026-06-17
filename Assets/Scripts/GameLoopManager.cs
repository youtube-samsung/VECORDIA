using UnityEngine;
using System;
using System.Collections;
using TMPro;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    [Header("—сылки на сцену")]
    public Transform playerTransform;
    public Transform bedSpawnPoint;
    public TextMeshProUGUI clockText;

    public static event Action OnDeathScreamerRequested;
    public static event Action OnLoopReset;

    private int _completedRituals = 0;
    private bool _isLoopEnding = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.OnMentalBreakdown += TriggerDeath;

        StartNewLoop();
    }

    private void OnDestroy()
    {
        if (AnxietyManager.Instance != null)
            AnxietyManager.Instance.OnMentalBreakdown -= TriggerDeath;
    }

    private void Update()
    {
        if (_isLoopEnding || AnxietyManager.Instance == null) return;
        UpdateClockUI(AnxietyManager.Instance.CurrentTotalAnxiety);
    }

    public void RegisterRitualComplete()
    {
        if (_isLoopEnding) return;
        _completedRituals++;
        if (_completedRituals >= 4) Win();
    }

    private void TriggerDeath()
    {
        if (_isLoopEnding) return;
        _isLoopEnding = true;

        SessionProgress.loopCount++;
        TimeManager.Instance.StopTimer();

        OnDeathScreamerRequested?.Invoke();
        StartCoroutine(SeamlessRestartRoutine());
    }

    private void Win()
    {
        _isLoopEnding = true;
        TimeManager.Instance.StopTimer();
        Debug.Log("ѕќЅ≈ƒј!");
    }

    private IEnumerator SeamlessRestartRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (CinematicController.Instance != null)
            yield return CinematicController.Instance.FadeRoutine(1f, 0.5f);

        StartNewLoop();

        if (CinematicController.Instance != null)
            yield return CinematicController.Instance.FadeRoutine(0f, 0.5f);
    }

    private void StartNewLoop()
    {
        _isLoopEnding = false;
        _completedRituals = 0;

        if (playerTransform != null && bedSpawnPoint != null)
        {
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = bedSpawnPoint.position;
            playerTransform.rotation = bedSpawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

        AnxietyManager.Instance.ResetAnxiety();
        TimeManager.Instance.StartTimer();

        OnLoopReset?.Invoke();
    }

    private void UpdateClockUI(float currentAnxiety)
    {
        int startMinutesTotal = 22 * 60 + 53;

        // currentAnxiety уже в формате 0-100. ƒелим на 100, чтобы получить процент от 7 минут.
        float minutesPassed = (currentAnxiety / 100f) * 7f;
        int currentMinutesTotal = startMinutesTotal + Mathf.FloorToInt(minutesPassed);

        int h = (currentMinutesTotal / 60) % 24;
        int m = currentMinutesTotal % 60;

        if (clockText != null) clockText.text = $"{h:00}:{m:00}";
    }
}