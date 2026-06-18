using UnityEngine;
using UnityEngine.InputSystem; // Обязательно добавляем эту библиотеку!

[RequireComponent(typeof(Collider))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Что запускаем?")]
    public CutsceneManager cutsceneManager;

    [Header("UI Подсказка")]
    public GameObject promptUI;

    private bool isPlayerInZone = false;
    private bool hasPlayed = false;

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void Update()
    {

        if (isPlayerInZone && !hasPlayed && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            hasPlayed = true;

            if (promptUI != null) promptUI.SetActive(false);

            if (cutsceneManager != null)
            {
                cutsceneManager.Play();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed && other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}