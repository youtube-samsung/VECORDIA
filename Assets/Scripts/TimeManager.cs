using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }


    [Tooltip("Время петли")]
    public float totalLoopDuration = 420f;

    public float ElapsedTime { get; private set; }


    public float TimeRatio => Mathf.Clamp01(ElapsedTime / totalLoopDuration);

    private bool _isRunning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartTimer()
    {
        ElapsedTime = 0f;
        _isRunning = true;
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    private void Update()
    {
        if (!_isRunning) return;
        ElapsedTime += Time.deltaTime;
    }
}