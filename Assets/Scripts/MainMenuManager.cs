using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Спрайты (Фон)")]
    [Tooltip("Объект Image, который висит на фоне меню")]
    public Image backgroundImage;
    public Sprite defaultSprite;
    public Sprite hoverPlaySprite;
    public Sprite hoverSettingsSprite;
    public Sprite hoverExitSprite;

    [Header("Панели")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("Звуки")]
    public SoundData menuMusic;   // Кассета с музыкой для меню
    public SoundData clickSound;  // Звук клика

    [Header("Настройки Неоновой Лампы")]
    [Tooltip("Динамик AudioSource, который ты добавил на объект меню")]
    public AudioSource neonAudioSource;
    [Tooltip("Аудиоклип треска и гула неоновой вывески")]
    public SoundData neonSoundData;

    [Space(5)]
    [Tooltip("Точка старта трека (обычно 0)")]
    public float clipStartTime = 0f;
    [Tooltip("Время, когда заканчивается включение и начинается бесконечный гуд")]
    public float loopStartTime = 0.5f;
    [Tooltip("Время, когда гуд заканчивается и пора переходить к выключению")]
    public float loopEndTime = 4.5f;
    [Tooltip("Время в самом конце трека, когда звук выключения полностью затих")]
    public float clipEndTime = 5.0f;

    [Header("Анти-спам ламп (Таймаут)")]
    [Tooltip("Время (в секундах), которое лампа ждет перед выключением. Защищает от дергания курсора.")]
    public float lampOffDelay = 0.15f;

    private Coroutine _neonLoopCoroutine;
    private Coroutine _delayFadeCoroutine;
    private bool _isMouseOverAnyButton = false;

    private IEnumerator Start()
    {
        // Даем Unity один кадр/долю секунды, чтобы AudioManager полностью проснулся и настроил микшер
        yield return new WaitForSeconds(0.1f);

        if (menuMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(menuMusic);
        }

        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(true);
            _settingsPanel.SetActive(false);
        }

        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);

        if (backgroundImage != null) backgroundImage.sprite = defaultSprite;
    }

    // --- НАВЕДЕНИЕ МЫШИ (ПОДСВЕТКА И НЕОНОВЫЙ ГУЛ) ---

    public void OnPointerEnterPlay()
    {
        if (backgroundImage != null) backgroundImage.sprite = hoverPlaySprite;
        OnButtonHovered();
    }

    public void OnPointerEnterSettings()
    {
        if (backgroundImage != null) backgroundImage.sprite = hoverSettingsSprite;
        OnButtonHovered();
    }

    public void OnPointerEnterExit()
    {
        if (backgroundImage != null) backgroundImage.sprite = hoverExitSprite;
        OnButtonHovered();
    }

    private void OnButtonHovered()
    {
        _isMouseOverAnyButton = true;

        // Если лампа собиралась тухнуть из-за того, что мышь перелетала между кнопками — отменяем выключение!
        if (_delayFadeCoroutine != null)
        {
            StopCoroutine(_delayFadeCoroutine);
            _delayFadeCoroutine = null;
        }

        // Если лампа еще не горит — зажигаем
        if (_neonLoopCoroutine == null && AudioManager.Instance != null && neonAudioSource != null && neonSoundData != null)
        {
            // Передаем clipStartTime в AudioManager (он начнет играть оттуда)
            neonAudioSource.time = clipStartTime;
            _neonLoopCoroutine = AudioManager.Instance.StartDynamicLoop(neonAudioSource, neonSoundData, loopStartTime, loopEndTime);
        }
    }

    public void OnPointerExitAny()
    {
        _isMouseOverAnyButton = false;

        // Запускаем корутину ожидания. Если игрок просто перевел мышь на соседнюю кнопку, 
        // метод OnButtonHovered прервет её до того, как лампа успеет издать звук выключения.
        if (_delayFadeCoroutine != null) StopCoroutine(_delayFadeCoroutine);
        _delayFadeCoroutine = StartCoroutine(DelayLampOffRoutine());
    }

    private IEnumerator DelayLampOffRoutine()
    {
        // Ждем указанное в инспекторе время (например, 0.15 секунды)
        yield return new WaitForSeconds(lampOffDelay);

        // Если за это время мышь так и не вернулась ни на одну кнопку — гасим лампу
        if (!_isMouseOverAnyButton)
        {
            if (backgroundImage != null) backgroundImage.sprite = defaultSprite;

            if (AudioManager.Instance != null && _neonLoopCoroutine != null)
            {
                AudioManager.Instance.StopDynamicLoop(_neonLoopCoroutine, neonAudioSource, loopEndTime, clipEndTime);
                _neonLoopCoroutine = null;
            }
        }

        _delayFadeCoroutine = null;
    }

    // --- КЛИКИ И ВЗАИМОДЕЙСТВИЕ ---

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySound2D(clickSound);
        }
    }

    public void StartGame()
    {
        PlayClickSound();
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        PlayClickSound();

        // При переходе в настройки гасим лампу мгновенно без всяких задержек
        _isMouseOverAnyButton = false;
        if (_delayFadeCoroutine != null) StopCoroutine(_delayFadeCoroutine);

        if (backgroundImage != null) backgroundImage.sprite = defaultSprite;
        if (AudioManager.Instance != null && _neonLoopCoroutine != null)
        {
            AudioManager.Instance.StopDynamicLoop(_neonLoopCoroutine, neonAudioSource, loopEndTime, clipEndTime);
            _neonLoopCoroutine = null;
        }

        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayClickSound();
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);

        _isMouseOverAnyButton = false;
        if (backgroundImage != null) backgroundImage.sprite = defaultSprite;
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}