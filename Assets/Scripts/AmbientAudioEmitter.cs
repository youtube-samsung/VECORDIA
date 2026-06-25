using UnityEngine;
using System.Collections;

public class AmbientAudioEmitter : MonoBehaviour
{

    public enum EmitterMode
    {
        ContinuousLoop, 
        RandomPeriodic 
    }

    [Header("Основные настройки")]
    [SerializeField] private EmitterMode emitterMode = EmitterMode.ContinuousLoop;
    [SerializeField] private SoundData loopSoundData;

    [Header("Настройки Пространства (2D / 3D)")]
    [Range(0f, 1f)]
    [Tooltip("0 = Полное 2D (звук играет прямо в ушах/в голове), 1 = Полное 3D (позиционируется в пространстве от объекта).")]
    [SerializeField] private float spatialBlend = 1f;

    [Tooltip("Радиус (в метрах), внутри которого 3D-звук всегда орет на максимуме.")]
    [SerializeField] private float minDistance = 1f;

    [Tooltip("Дистанция (в метрах), дальше которой 3D-звук полностью исчезает.")]
    [SerializeField] private float maxDistance = 8f;

    [Header("Настройки периодичности (Только для режима RandomPeriodic)")]
    [Tooltip("Минимальная пауза между случайными звуками")]
    [SerializeField] private float minTimeBetweenSounds = 15f;
    [Tooltip("Максимальная пауза между случайными звуками")]
    [SerializeField] private float maxTimeBetweenSounds = 40f;

    private AudioSource _spawnedSource;
    private Coroutine _randomSoundCoroutine;

    private void Start()
    {
        if (loopSoundData == null || AudioManager.Instance == null) return;


        if (emitterMode == EmitterMode.ContinuousLoop)
        {

            _spawnedSource = AudioManager.Instance.PlayLoopingSound3D(loopSoundData, transform.position, minDistance, maxDistance, spatialBlend);
        }

        else if (emitterMode == EmitterMode.RandomPeriodic)
        {
            _randomSoundCoroutine = StartCoroutine(RandomSoundRoutine());
        }
    }

    private IEnumerator RandomSoundRoutine()
    {
        while (true)
        {

            float waitTime = Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds);
            yield return new WaitForSeconds(waitTime);


            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0 || Time.timeScale == 0f)
            {
                continue;
            }

            if (loopSoundData != null && AudioManager.Instance != null)
            {

                if (spatialBlend <= 0f)
                {
                    AudioManager.Instance.PlaySound2D(loopSoundData);
                }

                else
                {
                    AudioManager.Instance.PlaySound3D(loopSoundData, transform.position);
                }
            }
        }
    }

    private void OnDestroy()
    {

        if (AudioManager.Instance != null && _spawnedSource != null)
        {
            AudioManager.Instance.StopLoopingSound(_spawnedSource);
        }

        if (_randomSoundCoroutine != null)
        {
            StopCoroutine(_randomSoundCoroutine);
        }
    }

    private void OnDrawGizmosSelected()
    {

        if (spatialBlend > 0f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}