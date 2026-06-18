using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _sprintAction;
    [SerializeField] private InputActionReference _pauseAction;
    [SerializeField] private InputActionReference _interactAction;

    [SerializeField] private InputActionReference _unpauseAction;

    [Header("Ritual Input")]
    [SerializeField] private InputActionReference _ritualClickAction;
    [SerializeField] private InputActionReference _ritualLookAction; 
    [SerializeField] private InputActionReference _ritualPointAction;
    [SerializeField] private InputActionReference _ritualInteractAction;


    public Vector2 MoveValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    public Vector2 RitualLookValue { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsSprinting { get; private set; }


    public Vector2 RitualPointValue { get; private set; }
    public bool IsRitualClickHeld { get; private set; }

    public System.Action OnJumpPerformed;
    public System.Action OnPausePerformed;
    public System.Action OnUnpausePerformed;
    public System.Action OnInteractPerformed;
    public System.Action OnRitualClickPerformed;
    public System.Action OnRitualInteractPerformed;


    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _moveAction.action.performed += ctx => MoveValue = ctx.ReadValue<Vector2>();
        _moveAction.action.canceled += ctx => MoveValue = Vector2.zero;
        _lookAction.action.performed += ctx => LookValue = ctx.ReadValue<Vector2>();
        _lookAction.action.canceled += ctx => LookValue = Vector2.zero;
        _jumpAction.action.performed += ctx => { JumpPressed = true; OnJumpPerformed?.Invoke(); };
        _jumpAction.action.canceled += ctx => JumpPressed = false;
        _sprintAction.action.performed += ctx => IsSprinting = true;
        _sprintAction.action.canceled += ctx => IsSprinting = false;
        _pauseAction.action.performed += ctx => OnPausePerformed?.Invoke();
        _interactAction.action.performed += ctx => OnInteractPerformed?.Invoke();

        _unpauseAction.action.performed += ctx => OnUnpausePerformed?.Invoke();
        _ritualClickAction.action.performed += ctx => OnRitualClickPerformed?.Invoke();

        _ritualClickAction.action.started += ctx => IsRitualClickHeld = true;
        _ritualClickAction.action.canceled += ctx => IsRitualClickHeld = false;

        _ritualLookAction.action.performed += ctx => RitualLookValue = ctx.ReadValue<Vector2>(); 
        _ritualLookAction.action.canceled += ctx => RitualLookValue = Vector2.zero;

        _ritualPointAction.action.performed += ctx => RitualPointValue = ctx.ReadValue<Vector2>();

        _ritualInteractAction.action.performed += ctx => OnInteractPerformed?.Invoke();
        _ritualInteractAction.action.performed += ctx => OnRitualInteractPerformed?.Invoke();

    }

    private void OnEnable()
    {
        _moveAction.action.Enable();
        _lookAction.action.Enable();
        _jumpAction.action.Enable();
        _sprintAction.action.Enable();
        _pauseAction.action.Enable();
        _unpauseAction.action.Enable();
        _interactAction.action.Enable();
        _ritualClickAction.action.Enable();
        _ritualLookAction.action.Enable();
        _ritualPointAction.action.Enable();
        _ritualInteractAction.action.Enable();

    }

    private void OnDisable()
    {
        _moveAction.action.Disable();
        _lookAction.action.Disable();
        _jumpAction.action.Disable();
        _sprintAction.action.Disable();
        _pauseAction.action.Disable();
        _unpauseAction.action.Disable();
        _interactAction.action.Disable();
        _ritualClickAction.action.Disable();
        _ritualLookAction.action.Disable();
        _ritualPointAction.action.Disable();
        _ritualInteractAction.action.Disable();
    }

    public void SwitchToGameplay()
    {
        _playerInput.SwitchCurrentActionMap("Gameplay");
        Debug.Log("Switched to Gameplay map");
    }

    public void SwitchToUI()
    {
        _playerInput.SwitchCurrentActionMap("UI");
        Debug.Log("Switched to UI map");
    }

    public void SwitchToRitual()
    {
        _playerInput.SwitchCurrentActionMap("Ritual");
        Debug.Log("Switched to Ritual map");
    }

    public void ConsumeJump()
    {
        JumpPressed = false;
    }
}
