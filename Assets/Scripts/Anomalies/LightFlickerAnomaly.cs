using UnityEngine;

// Наследуемся от твоего базового класса аномалий
public class LightFlickerAnomaly : BaseAnomaly
{
    [Header("Компоненты")]
    [SerializeField] private Renderer lampRenderer;
    [SerializeField] private Light targetLight;

    [Header("Настройки цвета (HDR)")]
    [ColorUsage(true, true)]
    [SerializeField] private Color emissionColor = Color.white;

    [Header("Настройки яркости")]
    [SerializeField] private float maxIntensity = 3f;
    [SerializeField] private float minIntensity = 0f;

    [Header("Настройки таймингов")]
    [SerializeField] private float flickerSpeed = 0.1f;

    private Material _lampMaterial;
    private float _nextFlickerTime;
    private bool _isOn = true;
    private bool _isAnomalyActive = false; // Флаг: активен ли сбой прямо сейчас

    private void Start()
    {
        if (lampRenderer == null) lampRenderer = GetComponent<Renderer>();

        if (lampRenderer != null)
        {
            _lampMaterial = lampRenderer.material;
            _lampMaterial.EnableKeyword("_EMISSION");
        }

        // Изначально принудительно сбрасываем в нормальное состояние
        ResetAnomaly();
    }

    private void Update()
    {
        // Если аномалия НЕ активна — код мигания просто не выполняется!
        if (!_isAnomalyActive) return;

        if (Time.time >= _nextFlickerTime)
        {
            _isOn = !_isOn;
            float currentIntensity = _isOn ? maxIntensity : minIntensity;

            if (_lampMaterial != null)
            {
                Color finalEmissionColor = emissionColor * currentIntensity;
                _lampMaterial.SetColor("_EmissionColor", finalEmissionColor);
            }

            if (targetLight != null)
            {
                targetLight.intensity = currentIntensity;
                targetLight.enabled = _isOn;
            }

            _nextFlickerTime = Time.time + Random.Range(flickerSpeed * 0.3f, flickerSpeed * 1.5f);
        }
    }

    // ВЫПОЛНЕНИЕ АБСТРАКТНОГО КЛАССА: Включаем мигание
    public override void TriggerAnomaly()
    {
        base.TriggerAnomaly(); // Если в родителе есть базовая логика
        _isAnomalyActive = true;
        _nextFlickerTime = Time.time; // Начинаем моргать мгновенно
    }

    // ВЫПОЛНЕНИЕ АБСТРАКТНОГО КЛАССА: Возвращаем свет в норму
    public override void ResetAnomaly()
    {
        base.ResetAnomaly(); // Если в родителе есть базовая логика
        _isAnomalyActive = false;

        // Возвращаем лампочке стабильный дефолтный свет
        if (targetLight != null)
        {
            targetLight.enabled = true;
            targetLight.intensity = maxIntensity;
        }

        if (_lampMaterial != null)
        {
            _lampMaterial.SetColor("_EmissionColor", emissionColor * maxIntensity);
        }
    }

    private void OnDestroy()
    {
        if (_lampMaterial != null) Destroy(_lampMaterial);
    }
}