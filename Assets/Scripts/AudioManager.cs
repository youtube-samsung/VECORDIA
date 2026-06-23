using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Постоянные динамики (AudioSource)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource heartbeatSource;
    public AudioSource footstepSource;

    [Header("Кассеты по умолчанию (Фон)")]
    public SoundData defaultAmbientMusic;
    public SoundData defaultHeartbeat;

    [Header("Система Тревоги")]
    [Tooltip("Резкие звуки (удар по ушам), когда тревога пересекает порог")]
    public SoundData anxietyStingers;
    [Tooltip("Время плавного нарастания громкости сердцебиения (в секундах)")]
    public float heartbeatFadeDuration = 2f;

    [Header("Настройки громкости по порогам (от 0 до 1)")]
    public float volumeAt10 = 0.05f;
    public float volumeAt20 = 0.1f;
    public float volumeAt30 = 0.2f;
    public float volumeAt40 = 0.3f;
    public float volumeAt50 = 0.4f;
    public float volumeAt60 = 0.5f;
    public float volumeAt70 = 0.6f;
    public float volumeAt80 = 0.7f;
    public float volumeAt90 = 0.8f;

    private Coroutine _heartbeatFadeCoroutine;
    private Coroutine _musicPlayCoroutine;

    [Header("Случайный Эмбиент (Диссонанс)")]
    public SoundData randomAmbientScareSounds;
    public float minTimeBetweenScares = 15f;
    public float maxTimeBetweenScares = 40f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (AnxietyManager.Instance != null)
        {
            AnxietyManager.Instance.OnAnxietyThresholdReached -= HandleAnxietySpike;
            AnxietyManager.Instance.OnMentalBreakdown -= ResetAudioOnDeath;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (AnxietyManager.Instance != null)
        {
            // Отписываемся от старых, чтобы не дублировать
            AnxietyManager.Instance.OnAnxietyThresholdReached -= HandleAnxietySpike;
            AnxietyManager.Instance.OnMentalBreakdown -= ResetAudioOnDeath;

            // Подписываемся заново на новой сцене
            AnxietyManager.Instance.OnAnxietyThresholdReached += HandleAnxietySpike;
            AnxietyManager.Instance.OnMentalBreakdown += ResetAudioOnDeath;
        }

        // Если загрузилась сцена игры (buildIndex > 0)
        if (scene.buildIndex > 0)
        {
            if (defaultAmbientMusic != null && (musicSource == null || !musicSource.isPlaying))
                PlayMusic(defaultAmbientMusic);

            if (defaultHeartbeat != null && (heartbeatSource == null || !heartbeatSource.isPlaying))
                StartHeartbeat(defaultHeartbeat);
        }

        // ЕСЛИ МЫ ВЕРНУЛИСЬ В ГЛАВНОЕ МЕНЮ (buildIndex == 0) — ЖЕСТКАЯ ЗАЧИСТКА
        if (scene.buildIndex == 0)
        {
            StopAllCoroutines();

            // 1. Принудительно затыкаем главные источники, чтобы музыка квартиры не пела в меню
            if (musicSource != null) musicSource.Stop();
            if (heartbeatSource != null) heartbeatSource.Stop();
            if (sfxSource != null) sfxSource.Stop();

            // 2. Сбрасываем их параметры в дефолт
            ResetAnxietySounds();

            // 3. Запускаем пугалки меню заново
            StartCoroutine(RandomAmbientRoutine());

            // 4. Уничтожаем все временные 3D-звуки из квартиры
            GameObject[] gameSounds = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject go in gameSounds)
            {
                if (go.name.StartsWith("TempAudio_3D") || go.name.StartsWith("LoopingAudio_3D_"))
                {
                    Destroy(go);
                }
            }
        }
    }

    private void Start()
    {
        // Проверяем по индексу сборки. 0 — это всегда MainMenu.
        // Если запустили сразу сцену квартиры в редакторе (индекс > 0), эмбиент включится сам.
        if (SceneManager.GetActiveScene().buildIndex > 0)
        {
            if (defaultAmbientMusic != null) PlayMusic(defaultAmbientMusic);
            if (defaultHeartbeat != null) StartHeartbeat(defaultHeartbeat);
        }

        StartCoroutine(RandomAmbientRoutine());
    }

    public void StopMusic()
    {
        if (_musicPlayCoroutine != null) StopCoroutine(_musicPlayCoroutine);
        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
    }

    private void ResetAudioOnDeath()
    {
        ResetAnxietySounds();
    }

    public void ResetAnxietySounds()
    {
        if (_heartbeatFadeCoroutine != null) StopCoroutine(_heartbeatFadeCoroutine);

        if (heartbeatSource != null)
        {
            heartbeatSource.volume = 0f;
            heartbeatSource.pitch = 1f;
        }
    }

    private void HandleAnxietySpike(int threshold)
    {
        if (anxietyStingers != null)
        {
            PlaySound2D(anxietyStingers);
        }

        float targetVolume = 0f;
        switch (threshold)
        {
            case 10: targetVolume = volumeAt10; break;
            case 20: targetVolume = volumeAt20; break;
            case 30: targetVolume = volumeAt30; break;
            case 40: targetVolume = volumeAt40; break;
            case 50: targetVolume = volumeAt50; break;
            case 60: targetVolume = volumeAt60; break;
            case 70: targetVolume = volumeAt70; break;
            case 80: targetVolume = volumeAt80; break;
            case 90: targetVolume = volumeAt90; break;
            default: targetVolume = (threshold / 100f) * 0.8f; break;
        }

        float factor = threshold / 100f;
        float targetPitch = 1f + (factor * 0.35f);

        if (_heartbeatFadeCoroutine != null) StopCoroutine(_heartbeatFadeCoroutine);
        _heartbeatFadeCoroutine = StartCoroutine(FadeHeartbeatRoutine(targetVolume, targetPitch, heartbeatFadeDuration));
    }

    private IEnumerator FadeHeartbeatRoutine(float targetVol, float targetPitch, float duration)
    {
        float startVol = heartbeatSource.volume;
        float startPitch = heartbeatSource.pitch;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (heartbeatSource == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            heartbeatSource.volume = Mathf.SmoothStep(startVol, targetVol, t);
            heartbeatSource.pitch = Mathf.SmoothStep(startPitch, targetPitch, t);
            yield return null;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.volume = targetVol;
            heartbeatSource.pitch = targetPitch;
        }
    }

    // БЕЗОПАСНЫЙ ЗАПУСК МУЗЫКИ ДЛЯ БИЛДА (Разводит потоки через корутину на 1 кадр)
    public void PlayMusic(SoundData musicData)
    {
        if (musicData == null || musicData.clips.Length == 0) return;

        if (_musicPlayCoroutine != null) StopCoroutine(_musicPlayCoroutine);
        _musicPlayCoroutine = StartCoroutine(PlayMusicDelayedRoutine(musicData));
    }

    private IEnumerator PlayMusicDelayedRoutine(SoundData musicData)
    {
        yield return null; // Пропускаем первый кадр инициализации

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = musicData.GetRandomClip();
        musicSource.volume = musicData.GetRandomVolume();
        musicSource.pitch = musicData.GetRandomPitch();
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StartHeartbeat(SoundData heartbeatData)
    {
        if (heartbeatData == null || heartbeatData.clips.Length == 0) return;
        if (heartbeatSource == null) heartbeatSource = gameObject.AddComponent<AudioSource>();

        heartbeatSource.clip = heartbeatData.GetRandomClip();
        heartbeatSource.loop = true;
        heartbeatSource.volume = 0f;
        heartbeatSource.Play();
    }

    public void PlaySound2D(SoundData soundData)
    {
        if (soundData == null || soundData.clips.Length == 0) return;
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.pitch = soundData.GetRandomPitch();
        sfxSource.PlayOneShot(soundData.GetRandomClip(), soundData.GetRandomVolume());
    }

    public void PlaySound3D(SoundData soundData, Vector3 position)
    {
        if (soundData == null || soundData.clips.Length == 0) return;

        GameObject tempAudioObj = new GameObject("TempAudio_3D");
        tempAudioObj.transform.position = position;

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.clip = soundData.GetRandomClip();
        tempSource.volume = soundData.GetRandomVolume();
        tempSource.pitch = soundData.GetRandomPitch();
        tempSource.spatialBlend = 1f;
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 15f;

        tempSource.Play();
        Destroy(tempAudioObj, tempSource.clip.length / tempSource.pitch);
    }

    public AudioSource PlayLoopingSound3D(SoundData soundData, Vector3 position, float minDistance = 1f, float maxDistance = 15f, float spatialBlend = 1f)
    {
        if (soundData == null || soundData.clips.Length == 0) return null;

        GameObject tempAudioObj = new GameObject("LoopingAudio_3D_" + soundData.name);
        tempAudioObj.transform.position = position;

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.clip = soundData.GetRandomClip();
        tempSource.volume = soundData.GetRandomVolume();
        tempSource.pitch = soundData.GetRandomPitch();

        tempSource.spatialBlend = spatialBlend;
        tempSource.rolloffMode = AudioRolloffMode.Linear;

        tempSource.minDistance = minDistance;
        tempSource.maxDistance = maxDistance;

        tempSource.loop = true;
        tempSource.Play();

        return tempSource;
    }

    public void StopLoopingSound(AudioSource sourceToStop)
    {
        if (sourceToStop != null)
        {
            sourceToStop.Stop();
            Destroy(sourceToStop.gameObject);
        }
    }

    private IEnumerator RandomAmbientRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minTimeBetweenScares, maxTimeBetweenScares);
            yield return new WaitForSeconds(waitTime);

            if (randomAmbientScareSounds != null && sfxSource != null)
            {
                PlaySound2D(randomAmbientScareSounds);
            }
        }
    }

    public void PlayFootstep(SoundData footstepData)
    {
        if (footstepData == null || footstepData.clips.Length == 0 || footstepSource == null) return;
        footstepSource.pitch = footstepData.GetRandomPitch();
        footstepSource.PlayOneShot(footstepData.GetRandomClip(), footstepData.GetRandomVolume());
    }

    public Coroutine StartDynamicLoop(AudioSource source, SoundData soundData, float loopStart, float loopEnd)
    {
        if (source == null || soundData == null || soundData.clips.Length == 0) return null;

        source.clip = soundData.GetRandomClip();
        source.volume = soundData.GetRandomVolume();
        source.pitch = soundData.GetRandomPitch();
        source.loop = false;

        return StartCoroutine(DynamicLoopRoutine(source, loopStart, loopEnd));
    }

    private IEnumerator DynamicLoopRoutine(AudioSource source, float loopStart, float loopEnd)
    {
        if (!source.isPlaying || source.time < loopStart)
        {
            source.time = 0f;
            source.Play();
        }

        while (true)
        {
            if (source == null) yield break;

            if (source.time >= loopEnd)
            {
                source.time = loopStart;
            }
            yield return null;
        }
    }

    public void StopDynamicLoop(Coroutine loopCoroutine, AudioSource source, float loopEnd, float clipEnd)
    {
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
        if (source == null || !source.isPlaying) return;

        StartCoroutine(DynamicLoopExitRoutine(source, loopEnd, clipEnd));
    }

    private IEnumerator DynamicLoopExitRoutine(AudioSource source, float loopEnd, float clipEnd)
    {
        if (source == null) yield break;

        source.time = loopEnd;
        while (source != null && source.time < clipEnd && source.isPlaying)
        {
            yield return null;
        }

        if (source != null) source.Stop();
    }
}