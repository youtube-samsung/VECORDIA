using UnityEngine;
using System.Collections;

public class DeathTransitionController : MonoBehaviour
{
    [Header("Ссылки на игрока")]
    public Transform playerCamera;
    public CharacterController playerController;
    public InputReader inputReader;

    [Header("Скример (Заглушка)")]
    public GameObject placeholderHands;
    public SoundData screamerSound;

    [Header("Настройки падения")]
    public float fallDuration = 0.5f;
    public float floorHeightOffset = 0.2f;

    [Header("Катсцены пробуждения")]
    public CutsceneManager[] wakeUpCutscenes;

    [Header("Настройки Лежания в кровати")]
    public float bedCameraYOffset = -0.7f;
    public float bedCameraXRotation = -70f;
    public float riseFromBedDuration = 1.5f;

    private Vector3 _normalCameraLocalPos;
    private Quaternion _normalCameraLocalRot;

    private void OnEnable()
    {
        GameLoopManager.OnDeathScreamerRequested += StartDeathSequence;
    }

    private void OnDisable()
    {
        GameLoopManager.OnDeathScreamerRequested -= StartDeathSequence;
    }

    private void Start()
    {
        if (playerCamera != null)
        {
            _normalCameraLocalPos = playerCamera.localPosition;
            _normalCameraLocalRot = playerCamera.localRotation;
        }
    }

    private void StartDeathSequence()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // ИСПРАВЛЕНИЕ БАГА 1: Находим все ритуальные хэндлеры камер на сцене 
        // и жестко тушим их корутины. Больше они не смогут вернуть управление посреди скримера!
        RitualCameraHandler[] ritualCameras = Object.FindObjectsByType<RitualCameraHandler>(FindObjectsSortMode.None);
        foreach (var cam in ritualCameras)
        {
            cam.StopAllCoroutines();
        }

        // Жестко глушим инпут и физическое тело персонажа
        if (inputReader != null) inputReader.SwitchToUI();
        if (playerController != null) playerController.enabled = false;

        // Принудительно выключаем скрипт ходьбы через CinematicController на время всей смерти
        if (CinematicController.Instance != null)
            CinematicController.Instance.ToggleControl(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Включаем руки-скример и звук испуга
        if (placeholderHands != null) placeholderHands.SetActive(true);
        if (screamerSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySound2D(screamerSound);

        yield return new WaitForSeconds(3.0f);

        // Падение камеры на пол квартиры
        Vector3 startPos = playerCamera.localPosition;
        Quaternion startRot = playerCamera.localRotation;
        Vector3 targetPos = new Vector3(startPos.x, -playerController.height / 2f + floorHeightOffset, startPos.z);
        Quaternion targetRot = Quaternion.Euler(-90f, startRot.eulerAngles.y, Random.Range(-15f, 15f));

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / fallDuration);
            playerCamera.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / fallDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        playerCamera.localPosition = targetPos;
        playerCamera.localRotation = targetRot;

        // Уходим в черный экран
        if (CinematicController.Instance != null)
            yield return StartCoroutine(CinematicController.Instance.FadeRoutine(1f, 0.4f));

        if (placeholderHands != null) placeholderHands.SetActive(false);

        // ТЕЛЕПОРТАЦИЯ К КРОВАТИ
        GameLoopManager.Instance.StartNewLoop();

        // ИСПРАВЛЕНИЕ БАГА 2: Принудительно синхронизируем внутренние матрицы трансформаций Unity.
        // Это заставляет физический движок мгновенно забыть старые коллизии места смерти.
        Physics.SyncTransforms();

        // Пока экран черный — кладем камеру на подушку
        playerCamera.localPosition = _normalCameraLocalPos + new Vector3(0f, bedCameraYOffset, 0f);
        playerCamera.localRotation = Quaternion.Euler(bedCameraXRotation, _normalCameraLocalRot.eulerAngles.y, 10f);

        // Запуск катсцены пробуждения (Персонаж всё еще заблокирован!)
        if (wakeUpCutscenes != null && wakeUpCutscenes.Length > 0)
        {
            int index = Random.Range(0, wakeUpCutscenes.Length);
            yield return StartCoroutine(wakeUpCutscenes[index].PlayRoutine());
        }
        else
        {
            if (CinematicController.Instance != null)
                yield return StartCoroutine(CinematicController.Instance.FadeRoutine(0f, 1f));
        }

        // Анимация подъема из кровати
        float riseElapsed = 0f;
        Vector3 lyingLocalPos = playerCamera.localPosition;
        Quaternion lyingLocalRot = playerCamera.localRotation;

        while (riseElapsed < riseFromBedDuration)
        {
            playerCamera.localPosition = Vector3.Lerp(lyingLocalPos, _normalCameraLocalPos, riseElapsed / riseFromBedDuration);
            playerCamera.localRotation = Quaternion.Slerp(lyingLocalRot, _normalCameraLocalRot, riseElapsed / riseFromBedDuration);
            riseElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.localPosition = _normalCameraLocalPos;
        playerCamera.localRotation = _normalCameraLocalRot;

        // ТОЛЬКО ТЕПЕРЬ, когда всё полностью закончилось, возвращаем свободу перемещения
        if (CinematicController.Instance != null)
            CinematicController.Instance.ToggleControl(true);

        if (inputReader != null) inputReader.SwitchToGameplay();
        if (playerController != null) playerController.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}