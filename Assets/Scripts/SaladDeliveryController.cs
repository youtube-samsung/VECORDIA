using UnityEngine;

public class SaladDeliveryController : MonoBehaviour
{
    [Header("Объекты салата")]
    [Tooltip("Объект готового салата, который будет перемещаться")]
    public GameObject fullSaladObject;

    [Header("Активаторы (RitualActivator)")]
    [Tooltip("Активатор, который висит на салате на столе")]
    public RitualActivator saladTableActivator;
    [Tooltip("Активатор, который висит на полке холодильника")]
    public RitualActivator fridgeActivator;

    [Header("Настройки удержания перед камерой")]
    public Vector3 holdingOffset = new Vector3(0.3f, -0.4f, 0.6f);
    public Vector3 holdingRotation = new Vector3(0f, 45f, 0f);

    [Header("Звуки")]
    [Tooltip("Звук, когда берем тарелку со стола")]
    public SoundData pickUpSound;
    [Tooltip("Звук, когда ставим тарелку в холодильник")]
    public SoundData putInFridgeSound;

    // Ссылки для возврата салата на исходную позицию при сбросе лупа
    private Transform _originalSaladParent;
    private Vector3 _originalSaladLocalPos;
    private Quaternion _originalSaladLocalRot;

    private Transform _playerCamera;
    private bool _isHoldingSalad = false;

    public bool IsCarryingSalad => _isHoldingSalad;

    private void OnEnable()
    {
        GameLoopManager.OnLoopReset += ResetSaladController;
    }

    private void OnDisable()
    {
        GameLoopManager.OnLoopReset -= ResetSaladController;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _playerCamera = Camera.main.transform;
        }

        // Запоминаем стартовую позицию салата на столе для сброса
        if (fullSaladObject != null)
        {
            _originalSaladParent = fullSaladObject.transform.parent;
            _originalSaladLocalPos = fullSaladObject.transform.localPosition;
            _originalSaladLocalRot = fullSaladObject.transform.localRotation;
        }

        // Холодильник НА СЦЕНЕ есть, но его скрипт подсказки изначально выключен
        if (fridgeActivator != null) fridgeActivator.enabled = false;
    }

    // Вызывается через UnityEvent из активатора салата на столе [E]
    public void PickUpSalad()
    {
        if (_isHoldingSalad || fullSaladObject == null || _playerCamera == null) return;

        _isHoldingSalad = true;

        // Звук поднятия тарелки
        if (AudioManager.Instance != null && pickUpSound != null)
        {
            AudioManager.Instance.PlaySound3D(pickUpSound, fullSaladObject.transform.position);
        }

        // Отключаем ТОЛЬКО скрипт активатора на столе, чтобы не горел текст [E]
        if (saladTableActivator != null) saladTableActivator.enabled = false;

        // Жестко цепляем салат к камере игрока
        fullSaladObject.transform.SetParent(_playerCamera);
        fullSaladObject.transform.localPosition = holdingOffset;
        fullSaladObject.transform.localRotation = Quaternion.Euler(holdingRotation);

        // Включаем скрипт подсказки на холодильнике
        if (fridgeActivator != null) fridgeActivator.enabled = true;

        Debug.Log("Салат взят на [E], привязан к камере. Активатор холодильника включен.");
    }

    // Вызывается через UnityEvent из активатора внутри холодильника [E]
    public void PlaceInFridge()
    {
        if (!_isHoldingSalad || fullSaladObject == null || fridgeActivator == null) return;

        _isHoldingSalad = false;

        // ВМЕСТО ПЕРЕМЕЩЕНИЯ: Просто выключаем салат, он исчезает из рук игрока
        fullSaladObject.SetActive(false);
        fullSaladObject.transform.SetParent(_originalSaladParent); // Возвращаем родителя на базу, чтобы не мусорить в камере

        // Отключаем подсказку холодильника
        fridgeActivator.enabled = false;

        // Звук установки
        if (AudioManager.Instance != null && putInFridgeSound != null)
        {
            AudioManager.Instance.PlaySound3D(putInFridgeSound, fridgeActivator.transform.position);
        }

        // Закрываем ритуал в менеджере петель
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.RegisterRitualComplete();
        }

        Debug.Log("Салат успешно убран (отключен) по кнопке [E]. Петля ритуала закрыта.");
    }

    // Сброс при смерти / новом лупе
    private void ResetSaladController()
    {
        _isHoldingSalad = false;

        if (fullSaladObject != null)
        {
            fullSaladObject.transform.SetParent(_originalSaladParent);
            fullSaladObject.transform.localPosition = _originalSaladLocalPos;
            fullSaladObject.transform.localRotation = _originalSaladLocalRot;

            // Салат скрываем в начале лупа
            fullSaladObject.SetActive(false);
        }

        if (saladTableActivator != null)
        {
            saladTableActivator.enabled = true;
            saladTableActivator.RitualFinished();
        }

        if (fridgeActivator != null)
        {
            fridgeActivator.enabled = false;
            fridgeActivator.RitualFinished();
        }
    }
}