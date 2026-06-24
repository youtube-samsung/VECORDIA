using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class RitualActivator : MonoBehaviour
{
    [Header("Ссылки (Логика)")]
    public InputReader inputReader;
    public Transform playerCamera;

    [Tooltip("Опционально: скрипт ритуала. Если пусто, объект будет работать как обычный интерактивный предмет.")]
    public MonoBehaviour targetRitualScript;

    [Header("Настройки луча (Raycast)")]
    public float interactDistance = 2.5f;

    [Header("UI: Подсказка (Текст)")]
    public TextMeshProUGUI interactionPromptText;
    public string[] interactionPrompts;

    [Header("UI: Настройки прицела")]
    public Image crosshairImage;
    public float normalScale = 1f;
    public float activeScale = 1.8f;
    public float crosshairAnimSpeed = 15f;
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    public Color activeColor = Color.white;

    [Header("Кастомизация Точки")]
    [Tooltip("Стандартная точка (дефолтный круглый спрайт, если есть)")]
    public Sprite defaultCrosshairSprite;
    [Tooltip("Спрайт, на который заменится точка при наведении (например: ладонь, глаз, ключ)")]
    public Sprite activeInteractionSprite;

    [Header("Событие при взаимодействии")]
    public UnityEvent onInteractEvent;

    private IRitualController _ritualController;
    private bool playerIsInZone = false;
    private int interactionStage = 0;

    // Сделали статический доступ публичным, чтобы мост салата мог сбросить владельца прицела
    public static RitualActivator OwnerOfCrosshair { get; set; }

    private void Awake()
    {
        if (targetRitualScript != null)
        {
            _ritualController = targetRitualScript as IRitualController;
            if (_ritualController == null)
            {
                Debug.LogError($"Target Ritual Script на объекте {gameObject.name} не реализует IRitualController!");
            }
        }

        if (inputReader != null) inputReader.OnInteractPerformed += OnInteractInputPerformed;
        GameLoopManager.OnDeathScreamerRequested += HidePrompt;
    }

    private void OnDestroy()
    {
        if (inputReader != null) inputReader.OnInteractPerformed -= OnInteractInputPerformed;
        GameLoopManager.OnDeathScreamerRequested -= HidePrompt;

        if (OwnerOfCrosshair == this) OwnerOfCrosshair = null;
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        UpdatePrompt();
    }

    // Метод для принудительного жесткого сброса прицела в дефолт из других скриптов
    public void ForceResetCrosshairInstant()
    {
        playerIsInZone = false;
        if (crosshairImage != null)
        {
            crosshairImage.transform.localScale = Vector3.one * normalScale;
            crosshairImage.color = normalColor;
            if (defaultCrosshairSprite != null) crosshairImage.sprite = defaultCrosshairSprite;
        }
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
        if (OwnerOfCrosshair == this) OwnerOfCrosshair = null;
    }

    // Если скрипт отключается кодом (например, при взятии салата), тушим прицел
    private void OnDisable()
    {
        if (OwnerOfCrosshair == this)
        {
            ForceResetCrosshairInstant();
        }
    }

    private void Update()
    {
        SaladDeliveryController saladController = Object.FindFirstObjectByType<SaladDeliveryController>();
        if (saladController != null && saladController.IsCarryingSalad)
        {
            if (this != saladController.fridgeActivator)
            {
                if (playerIsInZone)
                {
                    playerIsInZone = false;
                    UpdatePrompt();
                }
                if (OwnerOfCrosshair == this) OwnerOfCrosshair = null;
                return;
            }
        }

        if (_ritualController != null && _ritualController.IsRitualActive)
        {
            if (playerIsInZone) HidePrompt();
            AnimateCrosshair();
            return;
        }

        bool isLookingAtUs = false;
        if (playerCamera != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, interactDistance))
            {
                if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(this.transform))
                {
                    isLookingAtUs = true;
                }
            }
        }

        if (isLookingAtUs != playerIsInZone)
        {
            playerIsInZone = isLookingAtUs;
            UpdatePrompt();
        }

        if (playerIsInZone)
        {
            OwnerOfCrosshair = this;
        }

        if (OwnerOfCrosshair == this)
        {
            AnimateCrosshair();
        }
    }

    private void AnimateCrosshair()
    {
        if (crosshairImage == null) return;

        float targetScale = normalScale;
        Color targetColor = normalColor;

        if (_ritualController != null && _ritualController.IsRitualActive)
        {
            targetColor = crosshairImage.color;
            targetColor.a = 0f;
        }
        else if (playerIsInZone)
        {
            targetScale = activeScale;
            targetColor = activeColor;

            // Если мы смотрим на предмет и у нас настроена иконка — подменяем спрайт точки на картинку
            if (activeInteractionSprite != null)
                crosshairImage.sprite = activeInteractionSprite;
        }

        crosshairImage.transform.localScale = Vector3.Lerp(crosshairImage.transform.localScale, Vector3.one * targetScale, Time.deltaTime * crosshairAnimSpeed);
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * crosshairAnimSpeed);

        // Когда взгляд ушел с предмета, возвращаем исходный вид
        if (!playerIsInZone && (_ritualController == null || !_ritualController.IsRitualActive))
        {
            if (defaultCrosshairSprite != null)
                crosshairImage.sprite = defaultCrosshairSprite;

            if (Vector3.Distance(crosshairImage.transform.localScale, Vector3.one * normalScale) < 0.05f)
            {
                if (OwnerOfCrosshair == this) OwnerOfCrosshair = null;
            }
        }
    }

    private void OnInteractInputPerformed()
    {
        SaladDeliveryController saladController = Object.FindFirstObjectByType<SaladDeliveryController>();
        if (saladController != null && saladController.IsCarryingSalad)
        {
            if (this != saladController.fridgeActivator) return;
        }

        if (!playerIsInZone) return;

        if (_ritualController != null)
        {
            if (!_ritualController.IsRitualActive)
            {
                _ritualController.Interact(interactionStage);
                onInteractEvent?.Invoke();
                HidePrompt();
            }
        }
        else
        {
            onInteractEvent?.Invoke();
            HidePrompt();
        }
    }

    public void AdvanceInteractionStage() { interactionStage++; UpdatePrompt(); }
    public void RitualFinished() { interactionStage = 0; UpdatePrompt(); }
    public void HidePrompt() { playerIsInZone = false; if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false); }

    private void UpdatePrompt()
    {
        if (interactionPromptText == null) return;

        if (playerIsInZone && interactionStage < interactionPrompts.Length)
        {
            interactionPromptText.text = $"[E] - {interactionPrompts[interactionStage]}";
            interactionPromptText.gameObject.SetActive(true);
        }
        else
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
}