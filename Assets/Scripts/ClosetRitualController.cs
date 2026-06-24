using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClosetRitualController : MonoBehaviour, IRitualController
{
    [System.Serializable]
    public struct ClosetDifficultyTier
    {
        public float maxAnxiety;
        public int extraSwaps;
        public ThoughtData enterThought;
    }

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

    [Header("Настройки дверей")]
    [Tooltip("Время открытия дверей (в секундах)")]
    public float doorOpenDuration = 0.5f;
    [Tooltip("Время закрытия дверей (в секундах)")]
    public float doorCloseDuration = 0.5f;
    public float leftDoorOpenTargetAngle = -90f;
    public float rightDoorOpenTargetAngle = 90f;

    [Header("Настройки перетаскивания")]
    public float snapDistance = 0.5f;
    public LayerMask clothingLayer;

    [Header("Сложность и Субтитры")]
    public float extraSwapPenalty = 3f;
    public ClosetDifficultyTier[] difficultyTiers;

    [Header("Звуки")]
    public SoundData doorOpenSound;
    public SoundData doorCloseSound;
    public SoundData clothesMoveSound;

    private bool _isRitualActive = false;
    private bool _isRitualCompleted = false;
    public bool IsRitualActive => _isRitualActive;
    private bool areDoorsOpen = false;

    private ClothingItem currentlyDraggedItem;
    private Vector3 dragOffset;
    private float dragPlaneDistance;
    private Coroutine doorAnimationCoroutine;
    public static event System.Action OnClosetColorsReady;

    private int _minimumRequiredSwaps = 0;
    private int _currentSwapsMade = 0;

    private Quaternion closedRotationLeft;
    private Quaternion closedRotationRight;

    private void OnEnable()
    {
        GameLoopManager.OnLoopReset += ResetRitualGlobal;
        GameLoopManager.OnDeathScreamerRequested += ForceExitOnDeath;
    }

    private void OnDisable()
    {
        GameLoopManager.OnLoopReset -= ResetRitualGlobal;
        GameLoopManager.OnDeathScreamerRequested -= ForceExitOnDeath;
        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed -= OnDragStartOrEnd;
            inputReader.OnRitualInteractPerformed -= EndRitual;
        }
    }

    private void Start()
    {
        if (doorLeft != null) closedRotationLeft = doorLeft.localRotation;
        if (doorRight != null) closedRotationRight = doorRight.localRotation;

        for (int i = 0; i < clothes.Length; i++)
        {
            if (clothes[i] != null) clothes[i].clothingID = i;
        }

        GenerateCorrectLoopOrder();
        InitializeClothesRandomly();
    }

    private void GenerateCorrectLoopOrder()
    {
        SessionProgress.correctClosetOrder.Clear();
        SessionProgress.correctClosetColors.Clear();

        List<ClothingItem> shuffledForLoop = clothes.OrderBy(c => Random.value).ToList();

        foreach (var item in shuffledForLoop)
        {
            SessionProgress.correctClosetOrder.Add(item.clothingID);
            Color itemColor = item.data != null ? item.data.itemColor : Color.white;
            SessionProgress.correctClosetColors.Add(itemColor);
        }
        OnClosetColorsReady?.Invoke();
    }

    private void InitializeClothesRandomly()
    {
        List<ClothingItem> shuffledClothes = clothes.OrderBy(c => Random.value).ToList();
        for (int i = 0; i < hangers.Length; i++)
        {
            if (i < shuffledClothes.Count && shuffledClothes[i] != null)
            {
                shuffledClothes[i].transform.position = hangers[i].position;
                shuffledClothes[i].transform.rotation = hangers[i].rotation;
                shuffledClothes[i].currentHangerIndex = i;
                shuffledClothes[i].Initialize();
            }
        }

        _currentSwapsMade = 0;
        _minimumRequiredSwaps = CalculateMinimumSwaps();
    }

    private int CalculateMinimumSwaps()
    {
        int n = hangers.Length;
        bool[] visited = new bool[n];
        int cycles = 0;

        int[] currentHangerToID = new int[n];
        foreach (var item in clothes)
        {
            if (item.currentHangerIndex >= 0 && item.currentHangerIndex < n)
                currentHangerToID[item.currentHangerIndex] = item.clothingID;
        }

        for (int i = 0; i < n; i++)
        {
            if (!visited[i])
            {
                cycles++;
                int curr = i;
                while (!visited[curr])
                {
                    visited[curr] = true;
                    int currentID = currentHangerToID[curr];
                    int targetHanger = SessionProgress.correctClosetOrder.IndexOf(currentID);
                    curr = targetHanger;
                }
            }
        }
        return n - cycles;
    }

    private ClosetDifficultyTier GetCurrentTier()
    {
        if (AnxietyManager.Instance == null || difficultyTiers == null || difficultyTiers.Length == 0)
            return new ClosetDifficultyTier { extraSwaps = 4 };

        float currentAnxiety = AnxietyManager.Instance.CurrentTotalAnxiety;

        foreach (var tier in difficultyTiers)
        {
            if (currentAnxiety < tier.maxAnxiety)
                return tier;
        }

        return difficultyTiers[difficultyTiers.Length - 1];
    }

    public void Interact(int stage)
    {
        if (_isRitualCompleted) return;

        if (!areDoorsOpen) OpenDoors();
        else StartRitual();
    }

    private void OpenDoors()
    {
        if (areDoorsOpen) return;
        areDoorsOpen = true;

        if (AudioManager.Instance != null && doorOpenSound != null)
        {
            AudioManager.Instance.PlaySound3D(doorOpenSound, transform.position);
        }

        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);
        doorAnimationCoroutine = StartCoroutine(AnimateDoors(true));
    }

    private IEnumerator AnimateDoors(bool opening)
    {
        // Динамически выбираем скорость в зависимости от того, открываем или закрываем шкаф
        float currentDuration = opening ? doorOpenDuration : doorCloseDuration;

        Quaternion startRotationLeft = doorLeft.localRotation;
        Quaternion startRotationRight = doorRight.localRotation;

        Quaternion targetRotationLeft = opening ?
            Quaternion.Euler(doorLeft.localRotation.eulerAngles.x, leftDoorOpenTargetAngle, doorLeft.localRotation.eulerAngles.z) :
            closedRotationLeft;

        Quaternion targetRotationRight = opening ?
            Quaternion.Euler(doorRight.localRotation.eulerAngles.x, rightDoorOpenTargetAngle, doorRight.localRotation.eulerAngles.z) :
            closedRotationRight;

        float timeElapsed = 0;
        while (timeElapsed < currentDuration)
        {
            doorLeft.localRotation = Quaternion.Slerp(startRotationLeft, targetRotationLeft, timeElapsed / currentDuration);
            doorRight.localRotation = Quaternion.Slerp(startRotationRight, targetRotationRight, timeElapsed / currentDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        doorLeft.localRotation = targetRotationLeft;
        doorRight.localRotation = targetRotationRight;

        if (opening && ritualActivator != null && !_isRitualCompleted)
            ritualActivator.AdvanceInteractionStage();
    }

    public void StartRitual()
    {
        if (_isRitualActive || _isRitualCompleted) return;
        _isRitualActive = true;

        if (ritualActivator != null) ritualActivator.HidePrompt();
        if (cameraHandler != null) cameraHandler.EnterRitualMode(ritualCameraTarget);

        if (inputReader != null) inputReader.SwitchToRitual();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClosetDifficultyTier currentTier = GetCurrentTier();
        if (currentTier.enterThought != null && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowThought(currentTier.enterThought);
        }

        StartCoroutine(EnableRitualInputDelayed());
    }

    private IEnumerator EnableRitualInputDelayed()
    {
        transform.gameObject.SetActive(true); // Страховка
        yield return new WaitForSeconds(0.2f);

        if (_isRitualActive && inputReader != null)
        {
            inputReader.OnRitualClickPerformed += OnDragStartOrEnd;
            inputReader.OnRitualInteractPerformed += EndRitual;
        }
    }

    private void Update()
    {
        if (!_isRitualActive || currentlyDraggedItem == null) return;

        Vector2 mousePos = inputReader.RitualPointValue;
        Ray ray = cameraHandler.playerCamera.ScreenPointToRay(mousePos);
        Vector3 targetPos = ray.GetPoint(dragPlaneDistance) + dragOffset;

        currentlyDraggedItem.transform.position = targetPos;
    }

    private void OnDragStartOrEnd()
    {
        if (!_isRitualActive) return;

        if (currentlyDraggedItem == null)
        {
            Vector2 mousePos = inputReader.RitualPointValue;
            Ray ray = cameraHandler.playerCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 5f, clothingLayer))
            {
                ClothingItem item = hit.transform.GetComponentInParent<ClothingItem>();
                if (item != null)
                {
                    currentlyDraggedItem = item;
                    dragPlaneDistance = Vector3.Dot(cameraHandler.playerCamera.transform.forward, currentlyDraggedItem.transform.position - cameraHandler.playerCamera.transform.position);
                    dragOffset = currentlyDraggedItem.transform.position - ray.GetPoint(dragPlaneDistance);

                    if (AudioManager.Instance != null && clothesMoveSound != null)
                    {
                        AudioManager.Instance.PlaySound3D(clothesMoveSound, currentlyDraggedItem.transform.position);
                    }
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
                int oldHangerIndex = currentlyDraggedItem.currentHangerIndex;

                if (oldHangerIndex != closestHangerIndex)
                {
                    bool isHangerOccupied = clothes.Any(c => c != currentlyDraggedItem && c.currentHangerIndex == closestHangerIndex);

                    if (isHangerOccupied)
                    {
                        ClothingItem otherItem = clothes.First(c => c.currentHangerIndex == closestHangerIndex);
                        if (oldHangerIndex != -1)
                        {
                            otherItem.transform.position = hangers[oldHangerIndex].position;
                            otherItem.currentHangerIndex = oldHangerIndex;
                        }
                    }

                    currentlyDraggedItem.transform.position = hangers[closestHangerIndex].position;
                    currentlyDraggedItem.currentHangerIndex = closestHangerIndex;
                    _currentSwapsMade++;

                    if (AudioManager.Instance != null && clothesMoveSound != null)
                    {
                        AudioManager.Instance.PlaySound3D(clothesMoveSound, currentlyDraggedItem.transform.position);
                    }

                    int currentThreshold = _minimumRequiredSwaps + GetCurrentTier().extraSwaps;

                    if (_currentSwapsMade > currentThreshold)
                    {
                        if (AnxietyManager.Instance != null) AnxietyManager.Instance.AddPenalty(extraSwapPenalty);
                    }
                }
                else
                {
                    currentlyDraggedItem.transform.position = hangers[oldHangerIndex].position;
                }
            }
            else
            {
                if (currentlyDraggedItem.currentHangerIndex != -1)
                    currentlyDraggedItem.transform.position = hangers[currentlyDraggedItem.currentHangerIndex].position;
            }

            currentlyDraggedItem = null;
            CheckForCompletion();
        }
    }

    private void DropCurrentlyDraggedItem()
    {
        if (currentlyDraggedItem != null)
        {
            if (currentlyDraggedItem.currentHangerIndex != -1 && currentlyDraggedItem.currentHangerIndex < hangers.Length)
            {
                currentlyDraggedItem.transform.position = hangers[currentlyDraggedItem.currentHangerIndex].position;
            }
            currentlyDraggedItem = null;
        }
    }

    private void CheckForCompletion()
    {
        bool allHangersFilled = clothes.All(c => c.currentHangerIndex >= 0 && c.currentHangerIndex < hangers.Length);
        if (!allHangersFilled) return;

        foreach (var item in clothes)
        {
            int requiredHanger = SessionProgress.correctClosetOrder.IndexOf(item.clothingID);
            if (item.currentHangerIndex != requiredHanger) return;
        }

        StartCoroutine(CompleteRitualSequence());
    }

    private IEnumerator CompleteRitualSequence()
    {
        _isRitualCompleted = true;

        // Игрок закончил ритуал — сразу отдаем ему управление, чтобы он мог свободно уйти
        EndRitual();

        // Звук закрытия запускается только тут
        if (AudioManager.Instance != null && doorCloseSound != null)
        {
            AudioManager.Instance.PlaySound3D(doorCloseSound, transform.position);
        }

        // Двери закрываются со скоростью doorCloseDuration
        yield return StartCoroutine(AnimateDoors(false));
        areDoorsOpen = false;

        if (GameLoopManager.Instance != null) GameLoopManager.Instance.RegisterRitualComplete();
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        DropCurrentlyDraggedItem();

        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed -= OnDragStartOrEnd;
            inputReader.OnRitualInteractPerformed -= EndRitual;
        }

        if (cameraHandler != null) cameraHandler.ExitRitualMode();

        if (ritualActivator != null)
        {
            if (_isRitualCompleted)
            {
                ritualActivator.gameObject.SetActive(false);
            }
            else
            {
                // Если ритуал НЕ завершен (игрок просто вышел назад) — 
                // сбрасываем стадии текста, но двери ОСТАЮТСЯ открытыми.
                ritualActivator.RitualFinished();
                if (areDoorsOpen) ritualActivator.AdvanceInteractionStage();
            }
        }

        if (inputReader != null) inputReader.SwitchToGameplay();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ForceExitOnDeath()
    {
        if (_isRitualActive) EndRitual();
    }

    private void ResetRitualGlobal()
    {
        if (_isRitualActive) EndRitual();

        _isRitualCompleted = false;
        areDoorsOpen = false;

        if (ritualActivator != null) ritualActivator.gameObject.SetActive(true);

        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);

        if (doorLeft != null) doorLeft.localRotation = closedRotationLeft;
        if (doorRight != null) doorRight.localRotation = closedRotationRight;

        GenerateCorrectLoopOrder();
        InitializeClothesRandomly();
    }

    public void AbortRitual() { EndRitual(); }
}