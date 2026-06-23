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


    private Transform _originalSaladParent;
    private Vector3 _originalSaladLocalPos;
    private Quaternion _originalSaladLocalRot;
    private Vector3 _originalSaladLocalScale; 

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

        if (fullSaladObject != null)
        {
            _originalSaladParent = fullSaladObject.transform.parent;
            _originalSaladLocalPos = fullSaladObject.transform.localPosition;
            _originalSaladLocalRot = fullSaladObject.transform.localRotation;
            _originalSaladLocalScale = fullSaladObject.transform.localScale; 
        }

        if (fridgeActivator != null) fridgeActivator.enabled = false;
    }


    public void PickUpSalad()
    {
        if (_isHoldingSalad || fullSaladObject == null || _playerCamera == null) return;

        _isHoldingSalad = true;

        if (AudioManager.Instance != null && putInFridgeSound != null)
        {

            AudioManager.Instance.PlaySound3D(pickUpSound, fullSaladObject.transform.position);
        }

        if (saladTableActivator != null) saladTableActivator.enabled = false;

        fullSaladObject.transform.SetParent(_playerCamera, false);

        fullSaladObject.transform.localPosition = holdingOffset;
        fullSaladObject.transform.localRotation = Quaternion.Euler(holdingRotation);
        fullSaladObject.transform.localScale = _originalSaladLocalScale; 

        if (fridgeActivator != null) fridgeActivator.enabled = true;

        Debug.Log("Салат взят на [E], привязан к камере без изменения локального скейла.");
    }

    public void PlaceInFridge()
    {
        if (!_isHoldingSalad || fullSaladObject == null || fridgeActivator == null) return;

        _isHoldingSalad = false;

        fullSaladObject.SetActive(false);


        fullSaladObject.transform.SetParent(_originalSaladParent, false);

        if (fridgeActivator != null) fridgeActivator.enabled = false;

        if (AudioManager.Instance != null && putInFridgeSound != null)
        {
            AudioManager.Instance.PlaySound3D(putInFridgeSound, fridgeActivator.transform.position);
        }

        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.RegisterRitualComplete();
        }
    }


    private void ResetSaladController()
    {
        _isHoldingSalad = false;

        if (fullSaladObject != null)
        {

            fullSaladObject.transform.SetParent(_originalSaladParent, false);


            fullSaladObject.transform.localPosition = _originalSaladLocalPos;
            fullSaladObject.transform.localRotation = _originalSaladLocalRot;
            fullSaladObject.transform.localScale = _originalSaladLocalScale; 

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