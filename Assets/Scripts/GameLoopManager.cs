using UnityEngine;
using System;
using System.Collections;
using TMPro;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    [Header("Ссылки на сцену")]
    public Transform playerTransform;
    public Transform bedSpawnPoint;
    public TextMeshProUGUI clockText;

    [Header("Настройки Финала")]
    [Tooltip("Объект изображения внутри Канваса, который включится при победе")]
    public GameObject victoryImage;
    [Tooltip("Звук победы")]
    public SoundData victorySound;
    [Tooltip("Задержка перед экраном победы")]
    public float finalSequenceDelay = 3f;

    public static event Action OnDeathScreamerRequested;
    public static event Action OnLoopReset;
    public static event Action OnGameFinalReached; // Добавил эвент для финала

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        if (victoryImage != null)
            victoryImage.SetActive(false); // Прячем экран победы на старте

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

        Debug.Log($"[GameLoopManager] Ритуал завершен! Текущий прогресс: {_completedRituals} / 4");

        if (_completedRituals >= 4) Win();
    }

    private void TriggerDeath()
    {
        if (_isLoopEnding) return;
        _isLoopEnding = true;

        SessionProgress.loopCount++;

        OnDeathScreamerRequested?.Invoke();
    }

    private void Win()
    {
        if (_isLoopEnding) return;
        _isLoopEnding = true;

        if (TimeManager.Instance != null)
            TimeManager.Instance.StopTimer();

        Debug.Log("ПОБЕДА! Запуск финальной сцены через пару секунд...");

        // Запускаем корутину финала вместо мгновенного обрыва
        StartCoroutine(TriggerGameFinalWithDelay());
    }

    private IEnumerator TriggerGameFinalWithDelay()
    {
        // Ждем пару секунд, пока доиграют анимации/звуки последнего ритуала
        yield return new WaitForSeconds(finalSequenceDelay);

        if (victoryImage != null)
            victoryImage.SetActive(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.ResetAnxietySounds();

            if (victorySound != null)
            {
                AudioManager.Instance.PlaySound2D(victorySound);
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        OnGameFinalReached?.Invoke();
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

    public void StartNewLoop()
    {
        _isLoopEnding = false;
        _completedRituals = 0;

        if (victoryImage != null)
            victoryImage.SetActive(false); // Выключаем заглушку, если это был рестарт

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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

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