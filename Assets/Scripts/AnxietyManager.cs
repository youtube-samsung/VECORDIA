using UnityEngine;
using System;
using TMPro;

public class AnxietyManager : MonoBehaviour
{
    public static AnxietyManager Instance { get; private set; }

    [Header("Настройки")]
    public float maxAnxiety = 100f;
    public TextMeshProUGUI anxietyDebugText;

    public event Action OnMentalBreakdown;
    public event Action<int> OnAnxietyThresholdReached;

    // ТВОЕ НОВОЕ НАЗВАНИЕ
    public float CurrentTotalAnxiety { get; private set; }

    private int _lastThreshold = 0;
    private bool _isDead = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetAnxiety()
    {
        CurrentTotalAnxiety = 0f;
        _lastThreshold = 0;
        _isDead = false;
        UpdateUI();
    }

    // ТВОЕ НОВОЕ НАЗВАНИЕ (Сюда прилетают штрафы от ритуалов)
    public void AddPenalty(float amount)
    {
        if (_isDead) return;

        CurrentTotalAnxiety += amount;
        CurrentTotalAnxiety = Mathf.Clamp(CurrentTotalAnxiety, 0, maxAnxiety);

        CheckThresholds();
        UpdateUI();

        if (CurrentTotalAnxiety >= maxAnxiety)
        {
            _isDead = true;
            OnMentalBreakdown?.Invoke();
        }
    }

    private void Update()
    {
        if (_isDead || TimeManager.Instance == null) return;

        // Равномерный рост: 100% делится на время петли
        float anxietyPerSecond = maxAnxiety / TimeManager.Instance.totalLoopDuration;

        CurrentTotalAnxiety += anxietyPerSecond * Time.deltaTime;
        CurrentTotalAnxiety = Mathf.Clamp(CurrentTotalAnxiety, 0, maxAnxiety);

        UpdateUI();
        CheckThresholds();

        if (CurrentTotalAnxiety >= maxAnxiety)
        {
            _isDead = true;
            OnMentalBreakdown?.Invoke();
        }
    }

    private void CheckThresholds()
    {
        int currentDecade = Mathf.FloorToInt(CurrentTotalAnxiety / 10f) * 10;
        if (currentDecade > _lastThreshold && currentDecade < 100)
        {
            _lastThreshold = currentDecade;
            OnAnxietyThresholdReached?.Invoke(_lastThreshold);
        }
    }

    public float GetTremorIntensity() => CurrentTotalAnxiety / maxAnxiety;

    private void UpdateUI()
    {
        if (anxietyDebugText != null)
            anxietyDebugText.text = $"Тревожность: {Mathf.FloorToInt(CurrentTotalAnxiety)}%";
    }
}