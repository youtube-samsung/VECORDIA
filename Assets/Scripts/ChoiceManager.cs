using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance { get; private set; }

    [Header("Ссылки")]
    public InputReader inputReader;

    [Header("UI Элементы")]
    public GameObject choicePanel;
    public TextMeshProUGUI promptText;

    private Action<bool> _onChoiceMade;
    private bool _playerPressedButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (choicePanel != null) choicePanel.SetActive(false);
    }

    public void ShowTimedChoice(string prompt, float timeToWait, Action<bool> callback)
    {
        if (inputReader == null)
        {
            Debug.LogError(" [ChoiceManager] КРИТИЧЕСКАЯ ОШИБКА: Слот InputReader пуст! Перетащи сюда игрока (или объект с InputReader)!");
        }

        if (promptText != null) promptText.text = prompt;
        _onChoiceMade = callback;

        choicePanel.SetActive(true);
        StartCoroutine(ChoiceRoutine(timeToWait));
    }

    private IEnumerator ChoiceRoutine(float timeToWait)
    {
        float timer = 0f;
        _playerPressedButton = false;

        if (inputReader != null)
        {
            Debug.Log(" [ChoiceManager] Подписался на кнопку, жду нажатия...");
            inputReader.OnInteractPerformed += OnInteractPressed;
        }

        while (timer < timeToWait)
        {
            if (_playerPressedButton)
            {
                EndChoice(true);
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        EndChoice(false);
    }

    private void OnInteractPressed()
    {
        Debug.Log(" [ChoiceManager] Сигнал получен! Прерываю ритуал!");
        _playerPressedButton = true;
    }

    private void EndChoice(bool isAborted)
    {
        if (inputReader != null)
        {
            inputReader.OnInteractPerformed -= OnInteractPressed;
            Debug.Log(" [ChoiceManager] Отписался от кнопки.");
        }

        choicePanel.SetActive(false);
        _onChoiceMade?.Invoke(isAborted);
    }
}