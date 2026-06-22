using UnityEngine;

public class AmbientAudioEmitter : MonoBehaviour
{
    [Header("Настройки кассеты")]
    [SerializeField] private SoundData loopSoundData;

    [Header("Настройки 3D Звука")]
    [Range(0f, 1f)]
    [Tooltip("0 = Полное 2D (в голове), 1 = Полное 3D (позиционируется в ушах).")]
    [SerializeField] private float spatialBlend = 1f;

    [Tooltip("Радиус (в метрах), внутри которого звук всегда орет на максимуме и не становится громче.")]
    [SerializeField] private float minDistance = 1f;

    [Tooltip("Дистанция (в метрах), дальше которой звук полностью исчезает.")]
    [SerializeField] private float maxDistance = 8f;

    private AudioSource _spawnedSource;

    private void Start()
    {
        if (loopSoundData != null && AudioManager.Instance != null)
        {
            _spawnedSource = AudioManager.Instance.PlayLoopingSound3D(loopSoundData, transform.position, minDistance, maxDistance, spatialBlend);
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null && _spawnedSource != null)
        {
            AudioManager.Instance.StopLoopingSound(_spawnedSource);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}