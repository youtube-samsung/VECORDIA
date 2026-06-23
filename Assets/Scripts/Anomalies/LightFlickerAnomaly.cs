using UnityEngine;
using System.Collections;

public class LightFlickerAnomaly : BaseAnomaly
{
    [Header("Компоненты Света")]
    [SerializeField] private Light targetLight;

    [Header("Настройки Аудио")]
    [SerializeField] private SoundData electricFlickerSound;

    [Header("Настройки Мигания")]
    [SerializeField] private float minFlickerStep = 0.03f;
    [SerializeField] private float maxFlickerStep = 0.2f;

    private float _originalIntensity;
    private bool _originalEnabled;
    private AudioSource _spawnedAudioSource;
    private Coroutine _flickerCoroutine;

    private void Start()
    {
        if (targetLight != null)
        {
            _originalIntensity = targetLight.intensity;
            _originalEnabled = targetLight.enabled;
        }
    }

    public override void TriggerAnomaly()
    {
        base.TriggerAnomaly();
        _flickerCoroutine = StartCoroutine(FlickerRoutine());

        if (AudioManager.Instance != null && electricFlickerSound != null)
        {
            _spawnedAudioSource = AudioManager.Instance.PlayLoopingSound3D(electricFlickerSound, transform.position, 0.5f, 6f, 1f);
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (IsActive)
        {
            bool isLightOn = Random.Range(0f, 1f) > 0.4f;

            if (targetLight != null)
            {
                targetLight.enabled = isLightOn;
                targetLight.intensity = isLightOn ? Random.Range(_originalIntensity * 0.6f, _originalIntensity) : 0f;
            }

            yield return new WaitForSeconds(Random.Range(minFlickerStep, maxFlickerStep));
        }
    }

    public override void ResetAnomaly()
    {
        base.ResetAnomaly();

        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
        }

        if (targetLight != null)
        {
            targetLight.enabled = _originalEnabled;
            targetLight.intensity = _originalIntensity;
        }

        if (AudioManager.Instance != null && _spawnedAudioSource != null)
        {
            AudioManager.Instance.StopLoopingSound(_spawnedAudioSource);
            _spawnedAudioSource = null;
        }
    }
}