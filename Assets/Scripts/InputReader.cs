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

        // ПОЛУЧАЕМ ЛОКАЛЬНЫЕ ДЕЙСТВИЯ ИЗ PLAYER INPUT
        // Теперь SwitchCurrentActionMap будет реально работать и отключать ненужные кнопки!

        var move = _playerInput.actions.FindAction(_moveAction.action.name);
        move.performed += ctx => MoveValue = ctx.ReadValue<Vector2>();
        move.canceled += ctx => MoveValue = Vector2.zero;

        var look = _playerInput.actions.FindAction(_lookAction.action.name);
        look.performed += ctx => LookValue = ctx.ReadValue<Vector2>();
        look.canceled += ctx => LookValue = Vector2.zero;

        var jump = _playerInput.actions.FindAction(_jumpAction.action.name);
        jump.performed += ctx => { JumpPressed = true; OnJumpPerformed?.Invoke(); };
        jump.canceled += ctx => JumpPressed = false;

        var sprint = _playerInput.actions.FindAction(_sprintAction.action.name);
        sprint.performed += ctx => IsSprinting = true;
        sprint.canceled += ctx => IsSprinting = false;

        var pause = _playerInput.actions.FindAction(_pauseAction.action.name);
        pause.performed += ctx => OnPausePerformed?.Invoke();

        var interact = _playerInput.actions.FindAction(_interactAction.action.name);
        interact.performed += ctx => OnInteractPerformed?.Invoke();

        var unpause = _playerInput.actions.FindAction(_unpauseAction.action.name);
        unpause.performed += ctx => OnUnpausePerformed?.Invoke();

        var ritualClick = _playerInput.actions.FindAction(_ritualClickAction.action.name);
        ritualClick.performed += ctx => OnRitualClickPerformed?.Invoke();
        ritualClick.started += ctx => IsRitualClickHeld = true;
        ritualClick.canceled += ctx => IsRitualClickHeld = false;

        var ritualLook = _playerInput.actions.FindAction(_ritualLookAction.action.name);
        ritualLook.performed += ctx => RitualLookValue = ctx.ReadValue<Vector2>();
        ritualLook.canceled += ctx => RitualLookValue = Vector2.zero;

        var ritualPoint = _playerInput.actions.FindAction(_ritualPointAction.action.name);
        ritualPoint.performed += ctx => RitualPointValue = ctx.ReadValue<Vector2>();

        var ritualInteract = _playerInput.actions.FindAction(_ritualInteractAction.action.name);
        // ОШИБКА ДВОЙНОГО ВЫЗОВА ИСПРАВЛЕНА: Теперь здесь только выход из ритуала
        ritualInteract.performed += ctx => OnRitualInteractPerformed?.Invoke();
    }

    // ВНИМАНИЕ: Методы OnEnable и OnDisable полностью удалены!
    // PlayerInput сам автоматически включает нужную карту и выключает остальные.

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