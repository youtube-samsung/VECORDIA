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
    [Tooltip("Если это ритуал — пиши стадии. Если обычный предмет — достаточно одной строки (например: 'Осмотреть картину')")]
    public string[] interactionPrompts;

    [Header("UI: Точка-прицел")]
    public Image crosshairImage;
    public float normalScale = 1f;
    public float activeScale = 1.8f;
    public float crosshairAnimSpeed = 15f;
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    public Color activeColor = Color.white;

    [Header("Событие при взаимодействии")]
    [Tooltip("Сюда можно перетащить ЛЮБОЙ скрипт (например, CutsceneManager.Play) для активации по кнопке [E]")]
    public UnityEvent onInteractEvent;

    private IRitualController _ritualController;
    private bool playerIsInZone = false;
    private int interactionStage = 0;


    private static RitualActivator _ownerOfCrosshair;

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

        if (_ownerOfCrosshair == this) _ownerOfCrosshair = null;
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        UpdatePrompt();
    }

    private void Update()
    {

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
            _ownerOfCrosshair = this;
        }


        if (_ownerOfCrosshair == this)
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
        }


        crosshairImage.transform.localScale = Vector3.Lerp(crosshairImage.transform.localScale, Vector3.one * targetScale, Time.deltaTime * crosshairAnimSpeed);
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * crosshairAnimSpeed);

        if (!playerIsInZone && (_ritualController == null || !_ritualController.IsRitualActive))
        {
            if (Vector3.Distance(crosshairImage.transform.localScale, Vector3.one * normalScale) < 0.05f)
            {
                if (_ownerOfCrosshair == this) _ownerOfCrosshair = null;
            }
        }
    }

    private void OnInteractInputPerformed()
    {
        if (!playerIsInZone) return;

        // Объект является ритуалом
        if (_ritualController != null)
        {
            if (!_ritualController.IsRitualActive)
            {
                _ritualController.Interact(interactionStage);
                onInteractEvent?.Invoke(); 
                HidePrompt();
            }
        }
        // Обычный кликабельный предмет (катсцена, записка, скример, дверь)
        else
        {
            onInteractEvent?.Invoke(); 
            HidePrompt();
        }
    }

    public void AdvanceInteractionStage()
    {
        interactionStage++;
        UpdatePrompt();
    }

    public void RitualFinished()
    {
        interactionStage = 0;
        UpdatePrompt();
    }

    public void HidePrompt()
    {
        playerIsInZone = false;
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
    }

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