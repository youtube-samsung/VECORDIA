using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Постоянные динамики (AudioSource)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource heartbeatSource;

    [Header("Кассеты по умолчанию (Фон)")]
    public SoundData defaultAmbientMusic;
    public SoundData defaultHeartbeat;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        if (defaultAmbientMusic != null) PlayMusic(defaultAmbientMusic);
        if (defaultHeartbeat != null) StartHeartbeat(defaultHeartbeat);
    }


    public void PlayMusic(SoundData musicData)
    {
        if (musicData == null || musicData.clips.Length == 0) return;
        musicSource.clip = musicData.GetRandomClip();
        musicSource.volume = musicData.GetRandomVolume();
        musicSource.pitch = musicData.GetRandomPitch();
        musicSource.loop = true;
        musicSource.Play();
    }


    public void StartHeartbeat(SoundData heartbeatData)
    {
        if (heartbeatData == null || heartbeatData.clips.Length == 0) return;
        heartbeatSource.clip = heartbeatData.GetRandomClip();
        heartbeatSource.loop = true;
        heartbeatSource.volume = 0f;
        heartbeatSource.Play();
    }
    public void PlaySound2D(SoundData soundData)
    {
        if (soundData == null || soundData.clips.Length == 0) return;
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
    // Метод создает бесконечный 3D-звук и возвращает на него ссылку
    public AudioSource PlayLoopingSound3D(SoundData soundData, Vector3 position)
    {
        if (soundData == null || soundData.clips.Length == 0) return null;

        GameObject tempAudioObj = new GameObject("LoopingAudio_3D");
        tempAudioObj.transform.position = position;

        AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
        tempSource.clip = soundData.GetRandomClip();
        tempSource.volume = soundData.GetRandomVolume();
        tempSource.pitch = soundData.GetRandomPitch();

        tempSource.spatialBlend = 1f;
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 15f;

        tempSource.loop = true; // Бесконечный цикл
        tempSource.Play();

        return tempSource;
    }

    // Метод для остановки и удаления конкретного звука
    public void StopLoopingSound(AudioSource sourceToStop)
    {
        if (sourceToStop != null)
        {
            sourceToStop.Stop();
            Destroy(sourceToStop.gameObject);
        }
    }

    private void Update()
    {

        if (AnxietyManager.Instance != null && heartbeatSource != null)
        {
            float anxietyFactor = AnxietyManager.Instance.currentAnxiety / 100f;
            if (anxietyFactor > 0.6f)
            {
                heartbeatSource.volume = anxietyFactor/2;
                heartbeatSource.pitch = 1f + (anxietyFactor * 0.5f);
            }
            else
            {
                heartbeatSource.volume = 0f;
                heartbeatSource.pitch = 1f;
            }
        }
    }
}