using UnityEngine;
using System.Collections.Generic;

public class DishwashingZoneRitual : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator; // Ссылка для скрытия надписи
    public InputReader inputReader;
    public Transform spongeVisual;
    public List<Transform> zones;
    public Transform ritualCameraTarget;

    [Header("Настройки размеров")]
    public float spongeRadius = 0.1f;
    public float zoneRadius = 0.15f;
    [Tooltip("Максимальный радиус от центра тарелки, за который губка не вылетит")]
    public float maxMovementRadius = 0.8f;

    [Header("Физика инерции")]
    public float maxMoveSpeed = 4f;
    public float baseFriction = 15f;
    public float panicFriction = 1.5f;

    [Header("Баланс времени")]
    public float baseHoldTime = 2f;
    public float maxHoldTime = 4f;

    [Header("Штрафы")]
    public float outsideAnxietyRate = 10f;
    [Tooltip("Время (в секундах), которое прощается при вылете губки из зоны")]
    public float gracePeriod = 0.5f;

    private int currentZoneIndex = 0;
    private float currentZoneTimer = 0f;
    private float outOfZoneTimer = 0f; // Таймер нахождения вне зоны

    private Vector3 logicalPosition;
    private Vector3 currentVelocity;
    private Vector3 ritualCenter; // Начальная точка (центр тарелки)

    private bool _isRitualActive = false;
    private bool _ritualStarted = false;
    public bool IsRitualActive => _isRitualActive;

    public void Interact(int stage)
    {
        StartRitual();
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        _ritualStarted = false;
        currentZoneIndex = 0;
        currentZoneTimer = 0f;
        outOfZoneTimer = 0f;

        // Запоминаем центр объекта ритуала (он должен стоять по центру тарелки)
        ritualCenter = transform.position;
        logicalPosition = spongeVisual.position;

        // Скрываем надпись [E] помыть посуду
        if (ritualActivator != null) ritualActivator.HidePrompt();

        // Сбрасываем и показываем только первую зону
        foreach (var z in zones) z.gameObject.SetActive(false);
        if (zones.Count > 0)
        {
            zones[currentZoneIndex].gameObject.SetActive(true);
            zones[currentZoneIndex].GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);
    }

    private void Update()
    {
        if (!_isRitualActive) return;

        // Физика движения теперь работает ВСЕГДА
        HandleInertiaMovement();

        // А вот прогресс зоны и штрафы считаем ТОЛЬКО при зажатой кнопке
        if (inputReader.IsRitualClickHeld)
        {
            if (!_ritualStarted) _ritualStarted = true;
            ProcessZoneProgress();
        }
    }

    private void HandleInertiaMovement()
    {
        // 1. Получаем ввод, но если кнопка НЕ зажата — считаем, что игрок "не толкает" губку (ввод = 0)
        Vector2 input = inputReader.IsRitualClickHeld ? inputReader.RitualLookValue : Vector2.zero;

        // Рассчитываем желаемую скорость (куда игрок давит)
        Vector3 targetVel = (cameraHandler.playerCamera.transform.right * input.x +
                             cameraHandler.playerCamera.transform.up * input.y) * maxMoveSpeed;

        // 2. Рассчитываем трение (как быстро губка остановится сама по себе)
        float anxietyPercent = AnxietyManager.Instance.GetTremorIntensity();
        float currentFriction = Mathf.Lerp(baseFriction, panicFriction, anxietyPercent);

        // 3. Плавно меняем текущую скорость в сторону желаемой.
        // Если input занулен (кнопка отпущена), Lerp будет плавно тянуть currentVelocity к нулю.
        // Это и есть физика инерции.
        currentVelocity = Vector3.Lerp(currentVelocity, targetVel, currentFriction * Time.deltaTime);

        // Рассчитываем новую позицию
        Vector3 nextPosition = logicalPosition + currentVelocity * Time.deltaTime;

        // 4. Ограничение зоны (чтобы губка не улетала за тарелку даже по инерции)
        float distanceFromCenter = Vector3.Distance(nextPosition, ritualCenter);
        if (distanceFromCenter <= maxMovementRadius)
        {
            logicalPosition = nextPosition;
        }
        else
        {
            // Если ударились об край — гасим скорость и прижимаем к границе
            Vector3 directionFromCenter = (nextPosition - ritualCenter).normalized;
            logicalPosition = ritualCenter + directionFromCenter * maxMovementRadius;
            currentVelocity = Vector3.zero;
        }

        spongeVisual.position = logicalPosition;
    }

    private void ProcessZoneProgress()
    {
        if (currentZoneIndex >= zones.Count) return;

        Transform activeZone = zones[currentZoneIndex];
        float dist = Vector3.Distance(logicalPosition, activeZone.position);

        // Губка полностью внутри активной зоны
        bool isFullyInside = (dist + spongeRadius) <= zoneRadius;

        if (isFullyInside)
        {
            outOfZoneTimer = 0f; // Сбрасываем таймер "прощения", так как вернулись

            float anxietyPercent = AnxietyManager.Instance.GetTremorIntensity();
            float requiredTime = Mathf.Lerp(baseHoldTime, maxHoldTime, anxietyPercent);

            currentZoneTimer += Time.deltaTime;

            // Плавное изменение цвета от красного к зеленому
            activeZone.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.red, Color.green, currentZoneTimer / requiredTime);

            if (currentZoneTimer >= requiredTime)
            {
                AdvanceToNextZone();
            }
        }
        else
        {
            // Губка выскользнула, запускаем таймер "прощения"
            outOfZoneTimer += Time.deltaTime;

            if (outOfZoneTimer > gracePeriod)
            {
                // Начисляем тревогу только если игрок "тупит" дольше gracePeriod
                AnxietyManager.Instance.AddAnxiety(outsideAnxietyRate * Time.deltaTime);
            }

            // Прогресс зоны медленно откатывается назад
            currentZoneTimer = Mathf.Max(0, currentZoneTimer - Time.deltaTime * 0.5f);
        }
    }

    private void AdvanceToNextZone()
    {
        zones[currentZoneIndex].gameObject.SetActive(false);
        currentZoneIndex++;
        currentZoneTimer = 0f;
        outOfZoneTimer = 0f;

        if (currentZoneIndex < zones.Count)
        {
            zones[currentZoneIndex].gameObject.SetActive(true);
            zones[currentZoneIndex].GetComponent<MeshRenderer>().material.color = Color.red; // Сброс цвета
        }
        else
        {
            EndRitual();
        }
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        Debug.Log("Тарелка вымыта!");

        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished(); // Возвращаем надпись [E]
    }

    public void AbortRitual()
    {
        EndRitual();
    }
}