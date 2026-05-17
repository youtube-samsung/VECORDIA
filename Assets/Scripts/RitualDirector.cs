using System.Collections;
using UnityEngine;

// ВРЕМЕННО: привязка к Огурцу (потом вернем на интерфейс)
[RequireComponent(typeof(CucumberRitualController))]
public class RitualDirector : MonoBehaviour
{
    [Header("Связь с миром")]
    [Tooltip("Перетащи сюда Кран (для Огурца) или Телевизор (для Шкафа)")]
    public UniversalDistraction targetDistraction;

    [Header("Тайминги режиссуры")]
    [Tooltip("Сколько секунд игрок еще может резать после начала звука")]
    public float delayBeforePause = 1.0f;
    [Tooltip("Сколько секунд висит пауза (оцепенение) перед появлением текста")]
    public float pauseBeforeChoice = 1.5f;

    [Header("Настройки QTE (Выбора)")]
    [TextArea] public string choicePromptText = "Прерваться? - [E]";
    public float timeToChoose = 3f;

    private CucumberRitualController _ritual;
    private bool _hasTriggered = false;

    private void Awake()
    {
        _ritual = GetComponent<CucumberRitualController>();
        if (_ritual != null)
            _ritual.OnInterruptionRequested += HandleInterruption;
    }

    private void OnDestroy()
    {
        if (_ritual != null) _ritual.OnInterruptionRequested -= HandleInterruption;
    }

    private void HandleInterruption()
    {
        if (_hasTriggered) return;
        _hasTriggered = true;

        StartCoroutine(InterruptionRoutine());
    }

    private IEnumerator InterruptionRoutine()
    {
        // 1. ЗАПУСК ЗВУКА ИЗ ДРУГОЙ КОМНАТЫ
        if (targetDistraction != null)
        {
            targetDistraction.TurnOn();
        }

        // 2. ИГРОК В ШОКЕ, НО РИТУАЛ ИДЕТ: 
        // Игрок еще 1 секунду может двигать ножом и резать огурец, пока звук уже капает
        yield return new WaitForSeconds(delayBeforePause);

        // 3. УНИВЕРСАЛЬНАЯ ПАУЗА: ритуал замирает (вырубаем скрипт огурца)
        _ritual.enabled = false;

        // 4. ПАУЗА ОЦЕПЕНЕНИЯ (Тишина в управлении)
        yield return new WaitForSeconds(pauseBeforeChoice);

        // 5. ВЫБОР QTE
        ChoiceManager.Instance.ShowTimedChoice(choicePromptText, timeToChoose, (isAborted) =>
        {
            _ritual.enabled = true; // Включаем скрипт обратно

            if (isAborted)
            {
                Debug.Log("[RitualDirector] Игрок прервал ритуал!");
                _ritual.AbortRitual();

                // Ритуал завершен, мы выходим из зума камеры.
                // Звук крана ВСЕ ЕЩЕ ИГРАЕТ в ванной!
                // Игрок может пойти туда, нажать E, и скрипт UniversalDistraction его выключит.
            }
            else
            {
                Debug.Log("[RitualDirector] Игрок проигнорировал. Ритуал продолжается.");
                // Герой продолжает резать. 
                // А КРАН ВСЕ ЕЩЕ КАПАЕТ (потом Гейм-менеджер закроет дверь в ванную).
            }
        });
    }
}