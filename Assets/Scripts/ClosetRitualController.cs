using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClosetRitualController : MonoBehaviour, IRitualController
{
    [System.Serializable]
    public struct ClosetDifficultyTier
    {
        [Tooltip("Верхний порог тревожности (например, 30)")]
        public float maxAnxiety;
        [Tooltip("Сколько лишних ходов прощается при этой тревоге")]
        public int extraSwaps;
        [Tooltip("Thought Data, которая вызовется при входе")]
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
    public float doorOpenDuration = 0.5f;
    [Tooltip("Целевой угол оси Y для левой двери. Например, -90.")]
    public float leftDoorOpenTargetAngle = -90f;
    [Tooltip("Целевой угол оси Y для правой двери. Например, 90.")]
    public float rightDoorOpenTargetAngle = 90f;

    [Header("Настройки перетаскивания")]
    public float snapDistance = 0.5f;

    [Header("Слои")]
    [Tooltip("Выбери здесь слой, на котором находятся футболки, чтобы мышь не цепляла шкаф")]
    public LayerMask clothingLayer;

    [Header("Сложность и Субтитры")]
    public float extraSwapPenalty = 3f;
    [Tooltip("Настрой стадии тревоги. От меньшей к большей!")]
    public ClosetDifficultyTier[] difficultyTiers;

    private bool _isRitualActive = false;
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
        // ЗАПОМИНАЕМ ИДЕАЛЬНЫЕ УГЛЫ ДВЕРЕЙ ИЗ РЕДАКТОРА
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
        Debug.Log("[Шкаф] Сгенерирован новый правильный порядок и цвета для ванной!");
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
        Debug.Log($"[Шкаф] Идеальное количество ходов для этой петли: {_minimumRequiredSwaps}");
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
        if (!areDoorsOpen) OpenDoors();
        else StartRitual();
    }

    private void OpenDoors()
    {
        if (areDoorsOpen) return;
        areDoorsOpen = true;

        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);
        doorAnimationCoroutine = StartCoroutine(AnimateDoors(true));
    }

    private IEnumerator AnimateDoors(bool opening)
    {
        Quaternion startRotationLeft = doorLeft.localRotation;
        Quaternion startRotationRight = doorRight.localRotation;

        Quaternion targetRotationLeft = opening ?
            Quaternion.Euler(doorLeft.localRotation.eulerAngles.x, leftDoorOpenTargetAngle, doorLeft.localRotation.eulerAngles.z) :
            closedRotationLeft;

        Quaternion targetRotationRight = opening ?
            Quaternion.Euler(doorRight.localRotation.eulerAngles.x, rightDoorOpenTargetAngle, doorRight.localRotation.eulerAngles.z) :
            closedRotationRight;

        float timeElapsed = 0;
        while (timeElapsed < doorOpenDuration)
        {
            doorLeft.localRotation = Quaternion.Slerp(startRotationLeft, targetRotationLeft, timeElapsed / doorOpenDuration);
            doorRight.localRotation = Quaternion.Slerp(startRotationRight, targetRotationRight, timeElapsed / doorOpenDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        doorLeft.localRotation = targetRotationLeft;
        doorRight.localRotation = targetRotationRight;

        if (opening && ritualActivator != null) ritualActivator.AdvanceInteractionStage();
    }

    public void StartRitual()
    {
        if (_isRitualActive) return;
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

        Ray ray = cameraHandler.playerCamera.ScreenPointToRay(inputReader.RitualPointValue);
        currentlyDraggedItem.transform.position = ray.GetPoint(dragPlaneDistance) + dragOffset;
    }

    private void OnDragStartOrEnd()
    {
        if (!_isRitualActive) return;

        if (currentlyDraggedItem == null)
        {
            Ray ray = cameraHandler.playerCamera.ScreenPointToRay(inputReader.RitualPointValue);

            if (Physics.Raycast(ray, out RaycastHit hit, 5f, clothingLayer))
            {
                ClothingItem item = hit.transform.GetComponentInParent<ClothingItem>();
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

                    int currentThreshold = _minimumRequiredSwaps + GetCurrentTier().extraSwaps;

                    if (_currentSwapsMade > currentThreshold)
                    {
                        Debug.Log($"[Шкаф] Лишнее движение! Ход {_currentSwapsMade} из порога {currentThreshold}");
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

    private void CheckForCompletion()
    {
        bool allHangersFilled = clothes.All(c => c.currentHangerIndex >= 0 && c.currentHangerIndex < hangers.Length);
        if (!allHangersFilled) return;

        foreach (var item in clothes)
        {
            int requiredHanger = SessionProgress.correctClosetOrder.IndexOf(item.clothingID);

            if (item.currentHangerIndex != requiredHanger)
            {
                return; 
            }
        }


        if (GameLoopManager.Instance != null) GameLoopManager.Instance.RegisterRitualComplete();
        EndRitual();
    }

    public void EndRitual()
    {
        if (!_isRitualActive) return;
        _isRitualActive = false;

        if (inputReader != null)
        {
            inputReader.OnRitualClickPerformed -= OnDragStartOrEnd;
            inputReader.OnRitualInteractPerformed -= EndRitual;
        }

        if (cameraHandler != null) cameraHandler.ExitRitualMode();

        if (ritualActivator != null)
        {
            ritualActivator.RitualFinished();
            if (areDoorsOpen)
            {
                ritualActivator.AdvanceInteractionStage();
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

        areDoorsOpen = false;
        if (doorAnimationCoroutine != null) StopCoroutine(doorAnimationCoroutine);

        if (doorLeft != null) doorLeft.localRotation = closedRotationLeft;
        if (doorRight != null) doorRight.localRotation = closedRotationRight;

        GenerateCorrectLoopOrder();
        InitializeClothesRandomly();
    }

    public void AbortRitual() { EndRitual(); }
}