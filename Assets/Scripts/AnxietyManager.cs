using UnityEngine;
using System;
using TMPro;

public class AnxietyManager : MonoBehaviour
{
    public static AnxietyManager Instance { get; private set; }

    [Header("Настройки")]
    public float maxAnxiety = 100f;
    public TextMeshProUGUI anxietyDebugText;

    [Header("Мысли по десяткам (1=10%, 2=20% ... 9=90%)")]
    [Tooltip("Перетаскивай сюда свои Scriptable Objects (ThoughtData) в нужные индексы.")]
    public ThoughtData[] decadeThoughts = new ThoughtData[10];

    // События
    public event Action OnMentalBreakdown;
    public event Action<int> OnAnxietyThresholdReached; // Для старых эффектов экрана

    // Твой новый ивент, который теперь прокидывает сам Scriptable Object дальше
    public event Action<ThoughtData> OnThoughtTriggered;

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


            int index = currentDecade / 10;

            if (decadeThoughts != null && index < decadeThoughts.Length)
            {
                ThoughtData activeThought = decadeThoughts[index];


                if (activeThought != null)
                {
                    OnThoughtTriggered?.Invoke(activeThought);
                }
            }
        }
    }

    public float GetTremorIntensity() => CurrentTotalAnxiety / maxAnxiety;

    private void UpdateUI()
    {
        if (anxietyDebugText != null)
            anxietyDebugText.text = $"Тревожность: {Mathf.FloorToInt(CurrentTotalAnxiety)}%";
    }
}