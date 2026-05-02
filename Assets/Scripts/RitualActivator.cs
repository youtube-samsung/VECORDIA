using UnityEngine;
using TMPro;

public class RitualActivator : MonoBehaviour
{
    [Header("Ссылки")]
    public InputReader inputReader;
    public TextMeshProUGUI interactionPromptText;
    public MonoBehaviour targetRitualScript;

    [Header("Настройки")]
    [Tooltip("Текст, который будет отображаться на каждой стадии взаимодействия.")]
    public string[] interactionPrompts;

    private IRitualController _ritualController;
    private bool playerIsInZone = false;
    private int interactionStage = 0;

    private void Awake()
    {
        _ritualController = targetRitualScript as IRitualController;
        if (_ritualController == null)
        {
            Debug.LogError("Target Ritual Script не реализует IRitualController!");
            enabled = false;
            return;
        }
        if (inputReader != null) inputReader.OnInteractPerformed += OnInteractInputPerformed;
    }

    private void OnDestroy() { if (inputReader != null) inputReader.OnInteractPerformed -= OnInteractInputPerformed; }

    private void Start() { UpdatePrompt(); }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_ritualController.IsRitualActive)
        {
            playerIsInZone = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInZone = false;
            UpdatePrompt();
        }
    }

    private void OnInteractInputPerformed()
    {
        if (playerIsInZone && _ritualController != null && !_ritualController.IsRitualActive)
        {
            _ritualController.Interact(interactionStage);
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
        GetComponent<Collider>().enabled = true;
        UpdatePrompt();
    }
    public void HidePrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }

    private void UpdatePrompt()
    {
        if (interactionPromptText == null) return;

        if (playerIsInZone && interactionStage < interactionPrompts.Length)
        {
            interactionPromptText.text = $"Нажмите [E] - {interactionPrompts[interactionStage]}";
            interactionPromptText.gameObject.SetActive(true);
        }
        else
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
}
