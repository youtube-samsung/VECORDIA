using System.Collections;
using UnityEngine;
using TMPro; 

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    [Header("UI Компоненты")]
    [Tooltip("Ссылка на текстовый компонент на Canvas")]
    public TextMeshProUGUI subtitleText;

    [Tooltip("Ссылка на CanvasGroup для управления прозрачностью")]
    public CanvasGroup canvasGroup;

    [Header("Настройки анимации")]
    [Tooltip("Сколько секунд длится затухание текста в конце")]
    public float fadeDuration = 0.5f;

    private Coroutine currentRoutine;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
           
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }


        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (subtitleText != null) subtitleText.text = "";
    }
    private void Start()
    {
        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.OnThoughtTriggered += ShowThought;
        }
    }

    public void ShowThought(ThoughtData data)
    {
        if (data == null) return;


        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(DisplayRoutine(data));
    }

    private IEnumerator DisplayRoutine(ThoughtData data)
    {

        subtitleText.color = data.textColor;
        subtitleText.fontSize = data.fontSize;

        if (data.thoughtSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound2D(data.thoughtSound);
        }



        canvasGroup.alpha = 1f; 
        subtitleText.text = ""; 

        if (data.useTypewriterEffect)
        {
            foreach (char c in data.thoughtText)
            {
                subtitleText.text += c;
                yield return new WaitForSeconds(data.typewriterSpeed);
            }
        }
        else
        {
            subtitleText.text = data.thoughtText;
        }


        yield return new WaitForSeconds(data.displayDuration);


        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            fadeElapsed += Time.deltaTime;
            yield return null; 
        }

        canvasGroup.alpha = 0f;
        subtitleText.text = "";
        currentRoutine = null;
    }
    private void OnDestroy()
    {

        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.OnThoughtTriggered -= ShowThought;
        }
    }
}