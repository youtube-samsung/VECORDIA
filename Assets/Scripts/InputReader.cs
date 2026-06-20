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

    private PlayerInput _playerInput;

    //  ˝¯ËÛÂÏ ÍÓÌÍÂÚÌ˚Â ˝ÍÁÂÏÔÎˇ˚ ˝Í¯ÂÌÓ‚ ÔÓ Ëı ÛÌËÍ‡Î¸Ì˚Ï ID
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction ritualLookAction;
    private InputAction ritualPointAction;

    // Õ≈œ–≈–€¬Õ€≈ «Õ¿◊≈Õ»ﬂ (ŒÔÓÒ ‚ Â‡Î¸ÌÓÏ ‚ÂÏÂÌË ˜ÂÂÁ Ò‚ÓÈÒÚ‚‡)
    // ¡ÓÎ¸¯Â ÌËÍ‡ÍËı Á‡‚ËÒ‡ÌËÈ Ë ÌÛÎÂÈ: ‰‡ÌÌ˚Â ·ÂÛÚÒˇ Ì‡ÔˇÏÛ˛ ËÁ ‡ÍÚË‚ÌÓÈ Í‡Ú˚ ÔÓ ÚÂ·Ó‚‡ÌË˛
    public Vector2 MoveValue => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 LookValue => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 RitualLookValue => ritualLookAction != null ? ritualLookAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 RitualPointValue => ritualPointAction != null ? ritualPointAction.ReadValue<Vector2>() : Vector2.zero;

    // ‘À¿√» —Œ—“ŒﬂÕ»…
    public bool JumpPressed { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsRitualClickHeld { get; private set; }

    // —Œ¡€“»ﬂ ƒÀﬂ ƒ»— –≈“Õ€’ Õ¿∆¿“»… ( ÌÓÔÓÍ)
    public System.Action OnJumpPerformed;
    public System.Action OnPausePerformed;
    public System.Action OnUnpausePerformed;
    public System.Action OnInteractPerformed;
    public System.Action OnRitualClickPerformed;
    public System.Action OnRitualInteractPerformed;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        // Õ¿’Œƒ»Ã —“–Œ√Œ —¬Œ» Õ¿“»¬Õ€≈ › ÿ≈Õ€ œŒ GUID, »√ÕŒ–»–”ﬂ —Œ¬œ¿ƒ≈Õ»ﬂ »Ã®Õ
        moveAction = _playerInput.actions.FindAction(_moveAction.action.id);
        lookAction = _playerInput.actions.FindAction(_lookAction.action.id);
        ritualLookAction = _playerInput.actions.FindAction(_ritualLookAction.action.id);
        ritualPointAction = _playerInput.actions.FindAction(_ritualPointAction.action.id);

        // œŒƒœ»— » Õ¿  ÕŒœ » (—Ó·˚ÚËÈÌ‡ˇ ÏÓ‰ÂÎ¸ ÚÛÚ ÓÔ‡‚‰‡Ì‡ Ì‡ 100%)
        var jump = _playerInput.actions.FindAction(_jumpAction.action.id);
        if (jump != null)
        {
            jump.performed += ctx => { JumpPressed = true; OnJumpPerformed?.Invoke(); };
            jump.canceled += ctx => JumpPressed = false;
        }

        var sprint = _playerInput.actions.FindAction(_sprintAction.action.id);
        if (sprint != null)
        {
            sprint.performed += ctx => IsSprinting = true;
            sprint.canceled += ctx => IsSprinting = false;
        }

        var pause = _playerInput.actions.FindAction(_pauseAction.action.id);
        if (pause != null) pause.performed += ctx => OnPausePerformed?.Invoke();

        var interact = _playerInput.actions.FindAction(_interactAction.action.id);
        if (interact != null) interact.performed += ctx => OnInteractPerformed?.Invoke();

        var unpause = _playerInput.actions.FindAction(_unpauseAction.action.id);
        if (unpause != null) unpause.performed += ctx => OnUnpausePerformed?.Invoke();

        var ritualClick = _playerInput.actions.FindAction(_ritualClickAction.action.id);
        if (ritualClick != null)
        {
            ritualClick.performed += ctx => OnRitualClickPerformed?.Invoke();
            ritualClick.started += ctx => IsRitualClickHeld = true;
            ritualClick.canceled += ctx => IsRitualClickHeld = false;
        }

        var ritualInteract = _playerInput.actions.FindAction(_ritualInteractAction.action.id);
        if (ritualInteract != null) ritualInteract.performed += ctx => OnRitualInteractPerformed?.Invoke();
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