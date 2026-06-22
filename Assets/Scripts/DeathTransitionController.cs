using UnityEngine;
using System.Collections;

public class DeathTransitionController : MonoBehaviour
{
    [Header("Ссылки на игрока")]
    public Transform playerCamera;
    public CharacterController playerController;
    public InputReader inputReader;

    [Header("Скример (Заглушка)")]
    [Tooltip("Закинь сюда UI-картинку рук или 3D-модель перед лицом (изначально выключенную)")]
    public GameObject placeholderHands;
    public SoundData screamerSound;

    [Header("Настройки падения")]
    public float fallDuration = 0.5f; // Время завала камеры
    public float floorHeightOffset = 0.2f; // Насколько низко опускается голова

    [Header("Катсцены пробуждения")]
    [Tooltip("Перетащи сюда объекты с твоим CutsceneManager")]
    public CutsceneManager[] wakeUpCutscenes;

    private void OnEnable()
    {
        GameLoopManager.OnDeathScreamerRequested += StartDeathSequence;
    }

    private void OnDisable()
    {
        GameLoopManager.OnDeathScreamerRequested -= StartDeathSequence;
    }

    private void StartDeathSequence()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // 1. Блокируем управление
        if (inputReader != null) inputReader.SwitchToUI();
        if (playerController != null) playerController.enabled = false;


        // ПРЯЧЕМ КУРСОР ЖЕСТКО
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 2. СКРИМЕР (Резкое появление рук и звука)
        if (placeholderHands != null) placeholderHands.SetActive(true);
        if (screamerSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySound2D(screamerSound);

        // Ждем, пока руки на экране (например, 0.8 секунд)
        yield return new WaitForSeconds(3.0f);

        // Прячем руки
        //if (placeholderHands != null) placeholderHands.SetActive(false);

        // 3. ПАДЕНИЕ (Камера летит вниз и заваливается)
        Vector3 startPos = playerCamera.localPosition;
        Quaternion startRot = playerCamera.localRotation;

        Vector3 targetPos = new Vector3(startPos.x, -playerController.height / 2f + floorHeightOffset, startPos.z);
        // Заваливаем назад (-90 по X) и чуть набок для реализма
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

        // 4. ЗАТЕМНЕНИЕ (Быстрое, но не резкое)
        if (CinematicController.Instance != null)
            yield return StartCoroutine(CinematicController.Instance.FadeRoutine(1f, 0.4f));
        if (placeholderHands != null) placeholderHands.SetActive(false);


        GameLoopManager.Instance.StartNewLoop();

        if (wakeUpCutscenes != null && wakeUpCutscenes.Length > 0)
        {
            int index = Random.Range(0, wakeUpCutscenes.Length);

            // ВАЖНО: Теперь мы ЖДЕМ, пока твоя катсцена (и все ее шаги) не проиграются до конца
            yield return StartCoroutine(wakeUpCutscenes[index].PlayRoutine());
        }
        else
        {
            // Резервный выход, если катсцен нет
            if (CinematicController.Instance != null)
                yield return StartCoroutine(CinematicController.Instance.FadeRoutine(0f, 1f));
        }

        // 7. ЖЕСТКОЕ ВОЗВРАЩЕНИЕ УПРАВЛЕНИЯ
        // Катсцена закончилась, возвращаем инпуты и физику в норму
        if (inputReader != null) inputReader.SwitchToGameplay();
        if (playerController != null) playerController.enabled = true;

        // Прячем курсор
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

}
