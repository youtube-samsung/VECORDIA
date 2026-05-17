using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class UniversalDistraction : MonoBehaviour
{
    [Header("Ссылки")]
    public InputReader inputReader;
    public GameObject promptUI; // Надпись "Починить [E]" или "Ответить [E]"

    [Header("Настройки")]
    public SoundData distractionSound; // Шум воды, помехи ТВ
    public string distractionID; // Например: "Tap", "TV", "Phone"

    // Событие для Гейм-менеджера (чтобы он знал, что мы починили кран)
    public event Action<string> OnResolved;

    private AudioSource _activeSound;
    private bool _isActive = false;
    private bool _isPlayerInZone = false;

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    // Этот метод будет вызывать RitualDirector!
    public void TurnOn()
    {
        if (_isActive) return;
        _isActive = true;

        if (AudioManager.Instance != null && distractionSound != null)
        {
            // Просим Аудиоменеджер создать шум в координатах крана/телевизора
            _activeSound = AudioManager.Instance.PlayLoopingSound3D(distractionSound, transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Триггер работает ТОЛЬКО если предмет шумит (включен)
        if (_isActive && other.CompareTag("Player"))
        {
            _isPlayerInZone = true;
            if (promptUI != null) promptUI.SetActive(true);

            if (inputReader != null)
                inputReader.OnInteractPerformed += TryResolve;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CleanUp();
        }
    }

    private void TryResolve()
    {
        if (_isActive && _isPlayerInZone)
        {
            _isActive = false;

            // Просим Аудиоменеджер выключить звук
            if (AudioManager.Instance != null && _activeSound != null)
            {
                AudioManager.Instance.StopLoopingSound(_activeSound);
            }

            CleanUp();

            Debug.Log($"[Distraction] Игрок взаимодействовал с {distractionID}!");
            OnResolved?.Invoke(distractionID); // Сообщаем глобальному сюжету
        }
    }

    private void CleanUp()
    {
        _isPlayerInZone = false;
        if (promptUI != null) promptUI.SetActive(false);

        if (inputReader != null)
            inputReader.OnInteractPerformed -= TryResolve;
    }

    private void OnDestroy()
    {
        CleanUp();
        if (_activeSound != null && AudioManager.Instance != null)
            AudioManager.Instance.StopLoopingSound(_activeSound);
    }
}