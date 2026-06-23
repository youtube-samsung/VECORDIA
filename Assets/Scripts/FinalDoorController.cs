using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(RitualActivator))]
public class FinalDoorController : MonoBehaviour
{
    [Header("Ссылки на Игрока")]
    public Transform playerCamera;
    public CharacterController playerController;
    public InputReader inputReader;

    [Header("Настройки Анимации Двери")]
    public Transform doorMesh;
    public Vector3 openRotationOffset = new Vector3(0f, -90f, 0f);
    public float doorOpenDuration = 3.0f;
    public AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Настройки Ослепляющего Света")]
    public Light doorVolumeLight;
    [Tooltip("Яркость, когда дверь просто распахнулась")]
    public float openLightIntensity = 8f;
    [Tooltip("Бешеная яркость (вспышка) во время белого фейда")]
    public float blindingLightIntensity = 150f;

    [Header("Цепочка Затишья (Этап А)")]
    [Tooltip("Шаг 1: Время плавного угасания всех бытовых звуков техники в квартире")]
    public float houseSoundsFadeDuration = 2.5f;
    [Tooltip("Шаг 2: Длительность гробовой тишины ПОСЛЕ затухания звуков и ПЕРЕД началом стука и музыки")]
    public float delayInDeadSilence = 2.0f;

    [Header("Настройки Периодичности Стука в Дверь")]
    [Tooltip("Минимальная пауза между случайными звуками/стуками из двери")]
    public float doorSoundMinInterval = 3.0f;
    [Tooltip("Максимальная пауза между случайными звуками/стуками из двери")]
    public float doorSoundMaxInterval = 7.0f;
    [Tooltip("Время плавного затухания стука, если игрок нажал [E] прямо во время воспроизведения клипа")]
    public float doorSoundFadeOnOpenDuration = 0.5f;

    [Header("Настройки Финальной Музыки")]
    [Tooltip("Громкость, с которой музыка плавно стартует одновременно со стуком")]
    [Range(0f, 1f)] public float musicStartVolume = 0.15f;
    [Tooltip("Целевая громкость, до которой музыка разгонится при открытии двери")]
    [Range(0f, 2f)] public float musicTargetVolume = 0.75f;
    [Tooltip("Время плавного разгона музыки после открытия двери")]
    public float musicFadeInDuration = 4.0f;

    [Header("Ресурсы Финала")]
    public SoundData finalCalmMelody;
    public SoundData soundBehindDoor; // Контейнер со звуками стуков/скрежета
    public SoundData doorOpenSound;

    [Header("UI Статистики")]
    public CanvasGroup whiteFadeScreen;
    public float whiteFadeDuration = 1.5f;
    [Space(5)]
    public GameObject statsPanel;
    public TextMeshProUGUI loopsCountText;
    public TextMeshProUGUI totalTimeText;

    private RitualActivator _doorActivator;
    private AudioSource _doorAudioSource; // Наш локальный контролируемый 3D-динамик двери
    private AudioSource _finalMusicSource;
    private Coroutine _periodicDoorSoundTrack;
    private bool _hasBeenOpened = false;

    private void Awake()
    {
        _doorActivator = GetComponent<RitualActivator>();
        SetupLocalDoorAudioSource();
    }

    private void OnEnable()
    {
        GameLoopManager.OnAllRitualsCompleted += StartFinalSequenceChain;
    }

    private void OnDisable()
    {
        GameLoopManager.OnAllRitualsCompleted -= StartFinalSequenceChain;
    }

    private void Start()
    {
        if (whiteFadeScreen != null)
        {
            whiteFadeScreen.alpha = 0f;
            whiteFadeScreen.interactable = false;
            whiteFadeScreen.blocksRaycasts = false;
        }
        if (statsPanel != null) statsPanel.SetActive(false);
        if (doorVolumeLight != null) doorVolumeLight.intensity = 0f;

        if (_doorActivator != null) _doorActivator.enabled = false;
    }

    // Создаем честный локальный 3D источник звука прямо на двери
    private void SetupLocalDoorAudioSource()
    {
        _doorAudioSource = gameObject.AddComponent<AudioSource>();
        _doorAudioSource.playOnAwake = false;
        _doorAudioSource.spatialBlend = 1.0f; // 100% честное 3D позиционирование
        _doorAudioSource.minDistance = 1.0f;
        _doorAudioSource.maxDistance = 12.0f;
        _doorAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void StartFinalSequenceChain()
    {
        StartCoroutine(PrepareFinalPhaseRoutine());
    }

    // ЧЕСТНАЯ ЦЕПОЧКА ЗАТИШЬЯ И СТАРТА КОШМАРА
    private IEnumerator PrepareFinalPhaseRoutine()
    {
        // 1. Плавное угасание звуков бытовой техники в доме
        AudioSource[] allAudioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        yield return StartCoroutine(FadeOutAllHouseSoundsRoutine(allAudioSources, houseSoundsFadeDuration));

        // 2. Гробовая мертвая тишина (игрок стоит в полном вакууме и не понимает что происходит)
        yield return new WaitForSeconds(delayInDeadSilence);

        // 3. Запуск легкой музыки затишья через AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.ResetAnxietySounds();

            if (finalCalmMelody != null)
                AudioManager.Instance.PlayMusic(finalCalmMelody);

            yield return StartCoroutine(CacheAndSetupMusicSourceRoutine());
        }

        // 4. Запуск корутины периодического случайного стука в дверь
        _periodicDoorSoundTrack = StartCoroutine(PeriodicDoorSoundRoutine());

        // 5. Включаем активатор двери, разрешая игроку подойти к ней
        if (_doorActivator != null) _doorActivator.enabled = true;
    }

    // КОРУТИНА ПЕРИОДИЧНОСТИ: Рандомит клики, питч, громкость и интервалы из твоего SoundData
    private IEnumerator PeriodicDoorSoundRoutine()
    {
        while (!_hasBeenOpened)
        {
            if (soundBehindDoor != null && _doorAudioSource != null)
            {
                // Выдергиваем случайный клип и настройки из твоего ScriptableObject SoundData
                AudioClip randomClip = soundBehindDoor.GetRandomClip();
                if (randomClip != null)
                {
                    _doorAudioSource.pitch = soundBehindDoor.GetRandomPitch();
                    _doorAudioSource.volume = soundBehindDoor.GetRandomVolume();
                    _doorAudioSource.clip = randomClip;
                    _doorAudioSource.Play();
                }
            }

            // Рандомим интервал периодичности стуков из инспектора
            float nextWaitTime = Random.Range(doorSoundMinInterval, doorSoundMaxInterval);
            yield return new WaitForSeconds(nextWaitTime);
        }
    }

    private IEnumerator CacheAndSetupMusicSourceRoutine()
    {
        yield return new WaitForSeconds(0.05f);
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            if (src.isPlaying && !src.spatialBlend.Equals(1f) && src.loop)
            {
                _finalMusicSource = src;
                _finalMusicSource.volume = musicStartVolume;
                break;
            }
        }
    }

    // Игрок подошел в тишине под стуки и нажал [E]
    public void InteractWithDoor()
    {
        if (_hasBeenOpened) return;
        _hasBeenOpened = true;

        // Намертво обрубаем цикл периодического стука, чтобы новые звуки не запускались
        if (_periodicDoorSoundTrack != null) StopCoroutine(_periodicDoorSoundTrack);

        StartCoroutine(FinalDoorSequenceRoutine());
    }

    private IEnumerator FinalDoorSequenceRoutine()
    {
        // Лок игрока
        if (inputReader != null) inputReader.SwitchToUI();
        if (playerController != null) playerController.enabled = false;
        if (CinematicController.Instance != null) CinematicController.Instance.ToggleControl(false);

        if (CinematicController.Instance != null)
        {
            yield return StartCoroutine(CinematicController.Instance.LookAtRoutine(transform, 0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1)));
        }

        // ИСПРАВЛЕНИЕ: Плавно глушим текущий стук из двери, если игрок нажал [E] прямо во время воспроизведения звука
        StartCoroutine(FadeOutLocalDoorAudioRoutine(doorSoundFadeOnOpenDuration));

        // Плавно разгоняем финальную музыку на протяжении открытия двери и вспышки
        if (_finalMusicSource != null)
        {
            StartCoroutine(FadeMusicVolumeUpRoutine(musicTargetVolume, musicFadeInDuration));
        }

        // Звук открытия петель двери
        if (AudioManager.Instance != null && doorOpenSound != null)
        {
            AudioManager.Instance.PlaySound3D(doorOpenSound, transform.position);
        }

        // 1. АНИМАЦИЯ ДВЕРИ И СВЕТ ИЗ ЩЕЛИ
        Quaternion startRot = doorMesh != null ? doorMesh.localRotation : transform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(openRotationOffset);

        float elapsed = 0f;
        while (elapsed < doorOpenDuration)
        {
            float t = elapsed / doorOpenDuration;
            float curveT = doorOpenCurve.Evaluate(t);

            if (doorMesh != null) doorMesh.localRotation = Quaternion.Slerp(startRot, targetRot, curveT);

            if (doorVolumeLight != null)
            {
                doorVolumeLight.intensity = Mathf.Lerp(0f, openLightIntensity, curveT);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        if (doorMesh != null) doorMesh.localRotation = targetRot;

        // 2. ВСПЫШКА И ЗАЛИТИЕ КАНВАСА (Ослепление)
        float whiteElapsed = 0f;
        float currentLight = doorVolumeLight != null ? doorVolumeLight.intensity : openLightIntensity;

        while (whiteElapsed < whiteFadeDuration)
        {
            float progress = whiteElapsed / whiteFadeDuration;

            if (whiteFadeScreen != null) whiteFadeScreen.alpha = Mathf.Lerp(0f, 1f, progress);

            if (doorVolumeLight != null)
            {
                doorVolumeLight.intensity = Mathf.Lerp(currentLight, blindingLightIntensity, progress);
            }

            whiteElapsed += Time.deltaTime;
            yield return null;
        }

        // Разблокировка кликов на кнопках статистики
        if (whiteFadeScreen != null)
        {
            whiteFadeScreen.alpha = 1f;
            whiteFadeScreen.interactable = true;
            whiteFadeScreen.blocksRaycasts = true;
        }
        if (doorVolumeLight != null) doorVolumeLight.intensity = blindingLightIntensity;

        // 3. ПОДСЧЕТ СТАТИСТИКИ И ВЫВОД
        float totalSeconds = 0f;
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StopTimer();
            totalSeconds = TimeManager.Instance.ElapsedTime;
        }

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        if (loopsCountText != null) loopsCountText.text = $"Пройдено петель: {SessionProgress.loopCount}";
        if (totalTimeText != null) totalTimeText.text = $"Время прохождения: {minutes:00}:{seconds:00}";

        if (statsPanel != null) statsPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private IEnumerator FadeOutAllHouseSoundsRoutine(AudioSource[] sources, float duration)
    {
        float elapsed = 0f;
        Dictionary<AudioSource, float> startVolumes = new Dictionary<AudioSource, float>();

        foreach (var src in sources)
        {
            // Нам нельзя глушить локальный источник двери, который мы только что создали
            if (src != null && src.isPlaying && src != _doorAudioSource)
                startVolumes[src] = src.volume;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var src in sources)
            {
                if (src != null && startVolumes.ContainsKey(src))
                {
                    src.volume = Mathf.Lerp(startVolumes[src], 0f, t);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var src in sources)
        {
            if (src != null && src != _doorAudioSource) { src.volume = 0f; src.Stop(); }
        }
    }

    private IEnumerator FadeOutLocalDoorAudioRoutine(float duration)
    {
        if (_doorAudioSource == null || !_doorAudioSource.isPlaying) yield break;

        float startVol = _doorAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            _doorAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _doorAudioSource.Stop();
    }

    private IEnumerator FadeMusicVolumeUpRoutine(float targetVolume, float duration)
    {
        float startVolume = _finalMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_finalMusicSource == null) break;
            _finalMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (_finalMusicSource != null) _finalMusicSource.volume = targetVolume;
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        SessionProgress.ResetSession();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void QuitGameButton()
    {
        Application.Quit();
    }
}