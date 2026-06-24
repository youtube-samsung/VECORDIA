using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnomaliesDirector : MonoBehaviour
{
    public static AnomaliesDirector Instance { get; private set; }

    [Header("Динамические Графики Тревожности (Ось X: 0 - 100)")]

    [Tooltip("Ось X: Тревожность (0-100). Ось Y: Интервал проверок в секундах.")]
    [SerializeField] private AnimationCurve checkIntervalCurve = AnimationCurve.Linear(0f, 35f, 100f, 6f);

    [Tooltip("Ось X: Тревожность (0-100). Ось Y: Шанс срабатывания аномалии в процентах.")]
    [SerializeField] private AnimationCurve triggerChanceCurve = AnimationCurve.Linear(0f, 10f, 100f, 85f);

    private List<BaseAnomaly> _allSceneAnomalies = new List<BaseAnomaly>();
    private Coroutine _directorLoop;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {

        GameLoopManager.OnLoopReset += ResetAllDirectorAnomalies;
    }

    private void OnDisable()
    {
        GameLoopManager.OnLoopReset -= ResetAllDirectorAnomalies;
        if (_directorLoop != null) StopCoroutine(_directorLoop);
    }

    private void Start()
    {

        RefreshAnomaliesList();


        _directorLoop = StartCoroutine(DirectorBrainLoopRoutine());
    }

    public void RefreshAnomaliesList()
    {
        _allSceneAnomalies.Clear();
        _allSceneAnomalies.AddRange(Object.FindObjectsByType<BaseAnomaly>(FindObjectsSortMode.None));
        Debug.Log($"[AnomaliesDirector]: {_allSceneAnomalies.Count}");
    }

    private IEnumerator DirectorBrainLoopRoutine()
    {
        while (true)
        {

            float currentAnxiety = AnxietyManager.Instance != null ? AnxietyManager.Instance.CurrentTotalAnxiety : 0f;


            float dynamicWaitTime = checkIntervalCurve.Evaluate(currentAnxiety);
            yield return new WaitForSeconds(dynamicWaitTime);


            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0 || Time.timeScale == 0f)
                continue;


            float dynamicChance = triggerChanceCurve.Evaluate(currentAnxiety);


            if (Random.Range(0f, 100f) > dynamicChance)
                continue;


            List<BaseAnomaly> validAnomalies = new List<BaseAnomaly>();
            int totalWeight = 0;

            foreach (var anomaly in _allSceneAnomalies)
            {
                if (anomaly != null && !anomaly.IsActive)
                {
                    validAnomalies.Add(anomaly);
                    totalWeight += anomaly.selectionWeight;
                }
            }


            if (validAnomalies.Count == 0) continue;


            BaseAnomaly chosenAnomaly = ChooseAnomalyByWeight(validAnomalies, totalWeight);

            if (chosenAnomaly != null)
            {

                StartCoroutine(ExecuteAnomalyRoutine(chosenAnomaly));
            }
        }
    }

    private BaseAnomaly ChooseAnomalyByWeight(List<BaseAnomaly> anomalies, int totalWeight)
    {
        int randomRoll = Random.Range(0, totalWeight);
        int currentWeightSum = 0;

        foreach (var anomaly in anomalies)
        {
            currentWeightSum += anomaly.selectionWeight;
            if (randomRoll < currentWeightSum)
            {
                return anomaly;
            }
        }
        return anomalies[0];
    }

    private IEnumerator ExecuteAnomalyRoutine(BaseAnomaly anomaly)
    {
        Debug.Log($"[AnomaliesDirector]  активируется: <<{anomaly.anomalyName}>>");

        anomaly.TriggerAnomaly();


        yield return new WaitForSeconds(anomaly.duration);

        if (anomaly != null && anomaly.IsActive)
        {
            anomaly.ResetAnomaly();
        }
    }

    private void ResetAllDirectorAnomalies()
    {
        foreach (var anomaly in _allSceneAnomalies)
        {
            if (anomaly != null) anomaly.ResetAnomaly();
        }
        Debug.Log("[AnomaliesDirector] обнулено.");
    }
}