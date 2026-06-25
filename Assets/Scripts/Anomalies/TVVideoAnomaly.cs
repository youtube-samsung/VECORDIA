using UnityEngine;
using UnityEngine.Video;

public class TVVideoAnomaly : BaseAnomaly
{
    [Header("Компоненты Экрана")]
    [Tooltip("MeshRenderer самого экрана телевизора")]
    [SerializeField] private MeshRenderer screenRenderer;
    [Tooltip("Материал, который настроен на прием RenderTexture от видеоплеера")]
    [SerializeField] private Material tvVideoMaterial;
    [Tooltip("Обычная лампочка (Light), чтобы синхронизировать свет в пространстве")]
    [SerializeField] private Light targetLight;

    [Header("Компоненты Видео")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Жуткий видеоролик, который должен включиться")]
    [SerializeField] private VideoClip scareVideoClip;
    [Tooltip("Локальный AudioSource на телевизоре для трансляции звука видео в 3D")]
    [SerializeField] private AudioSource tvAudioSource;

    private Material _defaultMaterial;

    private void Start()
    {
        if (screenRenderer != null)
        {
            _defaultMaterial = screenRenderer.sharedMaterial;
        }

        if (videoPlayer != null && tvAudioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, tvAudioSource);
        }

        ResetAnomaly();
    }


    public override void TriggerAnomaly()
    {
        if (videoPlayer == null || scareVideoClip == null) return;

        base.TriggerAnomaly();

        if (screenRenderer != null && tvVideoMaterial != null)
        {
            screenRenderer.material = tvVideoMaterial;
        }


        videoPlayer.clip = scareVideoClip;
        videoPlayer.Play();
        if (targetLight != null)
        {
            targetLight.enabled = true;
        }



        Debug.Log($"[TVVideoAnomaly] Режиссер врубил видеоролик: {scareVideoClip.name}");
    }

    public override void ResetAnomaly()
    {
        base.ResetAnomaly(); 


        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }


        if (screenRenderer != null && _defaultMaterial != null)
        {
            screenRenderer.material = _defaultMaterial;
        }

        if (tvAudioSource != null)
        {
            tvAudioSource.Stop();
        }
        if (targetLight != null)
        {
            targetLight.enabled = false;
        }
    }
}