using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClosetRitualController : MonoBehaviour, IRitualController
{
    [Header("Ссылки")]
    public RitualCameraHandler cameraHandler;
    public RitualActivator ritualActivator;
    public InputReader inputReader;

    [Header("Объекты Ритуала")]
    public Transform ritualCameraTarget;
    public Transform doorLeft;
    public Transform doorRight;
    public Transform[] hangers;
    public ClothingItem[] clothes;


    public float doorOpenDuration = 0.5f; // Сколько секунд длится открытие/закрытие
    [Tooltip("Целевой локальный угол Y для ЛЕВОЙ двери в ОТКРЫТОМ состоянии. Например, -90 (открывается наружу).")]
    public float leftDoorOpenTargetAngle = -90f;
    [Tooltip("Целевой локальный угол Y для ПРАВОЙ двери в ОТКРЫТОМ состоянии. Например, 90 (открывается наружу, если scale.x=-1).")]
    public float rightDoorOpenTargetAngle = 90f;

    private Coroutine doorAnimationCoroutine;

    [Header("Настройки")]
    public float doorOpenAngle = 90f;
  
  

    private bool _isRitualActive = false;
    public bool IsRitualActive => _isRitualActive;
    private bool areDoorsOpen = false;
  public float snapDistance = 0.5f;
    private ClothingItem currentlyDraggedItem;
    private Vector3 dragOffset;
    private float dragPlaneDistance;

    private void Start()
    {
        InitializeClothes();
    }

    private void InitializeClothes()
    {
        List<ClothingItem> shuffledClothes = clothes.OrderBy(c => Random.value).ToList();
        for (int i = 0; i < hangers.Length; i++)
        {
            if (i < shuffledClothes.Count)
            {
                shuffledClothes[i].transform.position = hangers[i].position;
                shuffledClothes[i].transform.rotation = hangers[i].rotation;
                shuffledClothes[i].currentHangerIndex = i;
                shuffledClothes[i].Initialize();
            }
        }
    }

    public void Interact(int stage)
    {
        if (stage == 0 && !areDoorsOpen) OpenDoors();
        else if (stage == 1 && areDoorsOpen) StartRitual();
    }

    // Внутри ClosetRitualController.cs

    //private void OpenDoors()
    //{
    //    areDoorsOpen = true;

    //    // --- ИСПРАВЛЕНИЕ ДЛЯ ДВЕРЕЙ ---

    //    // 1. Для левой двери:
    //    // Предполагаем, что pivot двери находится на левом краю (петлях).
    //    // Если localRotation.y = 0 - закрыто, -90 - открыто.
    //    doorLeft.localRotation = Quaternion.Euler(doorLeft.localRotation.eulerAngles.x, -doorOpenAngle, doorLeft.localRotation.eulerAngles.z);

    //    // 2. Для правой двери (с scale.x = -1):
    //    // Ее локальные оси перевернуты. Если doorOpenAngle > 0, то для нее нужно будет вращать в ту же сторону,
    //    // что и для левой, но из-за scale.x=-1, это может быть 90 или -90.
    //    // Лучше всего - определить, куда "смотрят" двери при закрытом состоянии.
    //    // Если localRotation.y = 0 - закрыто, 90 (или -90) - открыто.
    //    // Из-за scale.x=-1, ее "открытие" может быть в положительную или отрицательную сторону.
    //    // Предположим, что для открытия ей тоже нужно повернуть на +doorOpenAngle.
    //    doorRight.localRotation = Quaternion.Euler(doorRight.localRotation.eulerAngles.x, -doorOpenAngle, doorRight.localRotation.eulerAngles.z);

    //    // --- ДЛЯ ПЛАВНОЙ АНИМАЦИИ ---
    //    // В будущем, когда будет работать, это можно заменить на Coroutine для плавного вращения:
    //    // StartCoroutine(AnimateDoorOpen(doorLeft, -doorOpenAngle));
    //    // StartCoroutine(AnimateDoorOpen(doorRight, doorOpenAngle));


    //    // Сообщаем активатору, что мы перешли на следующую стадию
    //    ritualActivator.AdvanceInteractionStage();
    //}

    // --- (Опционально) Метод для плавной анимации (пока не используется, но будет полезен) ---
    /*
    IEnumerator AnimateDoorOpen(Transform doorTransform, float targetYAngle)
    {
        Quaternion startRotation = doorTransform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(doorTransform.localRotation.eulerAngles.x, targetYAngle, doorTransform.localRotation.eulerAngles.z);
        float time = 0;

        while (time < 1)
        {
            doorTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, time);
            time += Time.deltaTime * doorOpenSpeed;
            yield return null;
        }
        doorTransform.localRotation = targetRotation;
    }
    */
    private void OpenDoors()
    {
        if (areDoorsOpen) return;

        areDoorsOpen = true;

        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);
        doorAnimationCoroutine = StartCoroutine(AnimateDoors(true));
    }

    private void CloseDoors()
    {
        if (!areDoorsOpen) return;

        areDoorsOpen = false;
        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);
        doorAnimationCoroutine = StartCoroutine(AnimateDoors(false));
    }
    IEnumerator AnimateDoors(bool opening)
    {
        // Захватываем текущее вращение pivot-ов
        Quaternion startRotationLeft = doorLeft.localRotation;
        Quaternion startRotationRight = doorRight.localRotation;

        // Вычисляем целевые вращения
        // Для открытия используем заданный targetAngle, для закрытия - 0 (закрытое состояние)
        Quaternion targetRotationLeft = Quaternion.Euler(
            doorLeft.localRotation.eulerAngles.x, // Сохраняем текущий X
            opening ? leftDoorOpenTargetAngle : 0f, // Целевой Y
            doorLeft.localRotation.eulerAngles.z    // Сохраняем текущий Z
        );

        Quaternion targetRotationRight = Quaternion.Euler(
            doorRight.localRotation.eulerAngles.x,  // Сохраняем текущий X
            opening ? rightDoorOpenTargetAngle : 0f, // Целевой Y
            doorRight.localRotation.eulerAngles.z     // Сохраняем текущий Z
        );

        float timeElapsed = 0;

        while (timeElapsed < doorOpenDuration)
        {
            doorLeft.localRotation = Quaternion.Slerp(startRotationLeft, targetRotationLeft, timeElapsed / doorOpenDuration);
            doorRight.localRotation = Quaternion.Slerp(startRotationRight, targetRotationRight, timeElapsed / doorOpenDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Убедимся, что двери точно в конечном положении
        doorLeft.localRotation = targetRotationLeft;
        doorRight.localRotation = targetRotationRight;

        if (opening)
        {
            ritualActivator.AdvanceInteractionStage();
        }
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
        _isRitualActive = true;
        if (ritualActivator != null) ritualActivator.HidePrompt();

        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);

        // ПОДПИСЫВАЕМСЯ ЗДЕСЬ:
        if (inputReader != null) inputReader.OnRitualClickPerformed += OnDragStartOrEnd;
    }

    private void Update()
    {
        if (!_isRitualActive || currentlyDraggedItem == null) return;

        Ray ray = cameraHandler.playerCamera.ScreenPointToRay(inputReader.RitualPointValue);
        currentlyDraggedItem.transform.position = ray.GetPoint(dragPlaneDistance) + dragOffset;
    }

    private void OnDragStartOrEnd()
    {
        if (!_isRitualActive) return;

        if (currentlyDraggedItem == null)
        {
            Ray ray = cameraHandler.playerCamera.ScreenPointToRay(inputReader.RitualPointValue);
            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
                ClothingItem item = hit.transform.GetComponent<ClothingItem>();
                if (item != null)
                {
                    currentlyDraggedItem = item;

                    dragPlaneDistance = Vector3.Dot(cameraHandler.playerCamera.transform.forward, currentlyDraggedItem.transform.position - cameraHandler.playerCamera.transform.position);
                    dragOffset = currentlyDraggedItem.transform.position - ray.GetPoint(dragPlaneDistance);
                }
            }
        }
        else
        {
            float closestDistance = float.MaxValue;
            int closestHangerIndex = -1;

            for (int i = 0; i < hangers.Length; i++)
            {
                float distance = Vector3.Distance(currentlyDraggedItem.transform.position, hangers[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHangerIndex = i;
                }
            }

            if (closestHangerIndex != -1 && closestDistance < snapDistance)
            {
                bool isHangerOccupied = clothes.Any(c => c != currentlyDraggedItem && c.currentHangerIndex == closestHangerIndex);

                if (isHangerOccupied)
                {
                    ClothingItem otherItem = clothes.First(c => c.currentHangerIndex == closestHangerIndex);

                    int oldHangerIndex = currentlyDraggedItem.currentHangerIndex;
                    if (oldHangerIndex != -1)
                    {
                        otherItem.transform.position = hangers[oldHangerIndex].position;
                        otherItem.currentHangerIndex = oldHangerIndex;
                    }
                }

                currentlyDraggedItem.transform.position = hangers[closestHangerIndex].position;
                currentlyDraggedItem.currentHangerIndex = closestHangerIndex;
            }

            currentlyDraggedItem = null;
            CheckForCompletion();
        }
    }

    private void CheckForCompletion()
    {
        bool isSorted = true;
        foreach (var item in clothes)
        {
            if (item.currentHangerIndex != item.data.sortOrder)
            {
                isSorted = false;
                break;
            }
        }
        if (isSorted)
        {
            Debug.Log("РИТУАЛ ЗАВЕРШЕН! Порядок идеален.");
            EndRitual();
        }
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        // ОТПИСЫВАЕМСЯ ЗДЕСЬ:
        if (inputReader != null) inputReader.OnRitualClickPerformed -= OnDragStartOrEnd;

        if (cameraHandler != null) cameraHandler.ExitRitualMode();
        if (ritualActivator != null) ritualActivator.RitualFinished();
    }
}
