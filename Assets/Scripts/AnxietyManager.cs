using UnityEngine;
using System;
using TMPro; // <-- ОБЯЗАТЕЛЬНО добавить для работы с текстом

public class AnxietyManager : MonoBehaviour
{
    public static AnxietyManager Instance { get; private set; }

    [Header("Настройки")]
    public float maxAnxiety = 100f;
    public float currentAnxiety = 0f;

    [Header("UI для дебага")]
    public TextMeshProUGUI anxietyDebugText; // <-- Ссылка на текст на экране

    public event Action OnMentalBreakdown;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI(); // Показываем нули при старте игры
    }

    public void AddAnxiety(float amount)
    {
        currentAnxiety += amount;
        currentAnxiety = Mathf.Clamp(currentAnxiety, 0, maxAnxiety);

        UpdateUI(); // <-- Обновляем текст при каждом росте

        if (currentAnxiety >= maxAnxiety)
        {
            Debug.LogWarning("ГЛОБАЛЬНЫЙ МЕНТАЛЬНЫЙ СРЫВ!");
            OnMentalBreakdown?.Invoke();
        }
    }

    public void ReduceAnxiety(float amount)
    {
        currentAnxiety -= amount;
        currentAnxiety = Mathf.Clamp(currentAnxiety, 0, maxAnxiety);

        UpdateUI(); // <-- Обновляем текст при снижении
    }

    public float GetTremorIntensity()
    {
        return currentAnxiety / maxAnxiety;
    }

    private void UpdateUI()
    {
        if (anxietyDebugText != null)
        {
            // Форматируем до 1 знака после запятой, чтобы цифры не мельтешили
            anxietyDebugText.text = $"Тревожность: {currentAnxiety:F1} / {maxAnxiety}";
        }
    }
}