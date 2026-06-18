using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float stickToGroundForce = 1f;

    [Header("Camera")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;
    [SerializeField] private bool smoothMouse = true;
    [SerializeField] private float smoothTime = 5f;

    [Header("Head Bob")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float headBobSpeed = 1f;
    [SerializeField] private float headBobAmount = 0.05f;
    [SerializeField] private float runstepLenghten = 0.5f;

    [Header("FOV Kick")]
    [SerializeField] private bool useFovKick = true;
    [SerializeField] private float fovKickAmount = 10f;
    [SerializeField] private float fovKickTime = 0.1f;

   
    private CharacterController controller;
    private Camera cam;

   
    private Vector3 moveDirection;
    private float cameraXRotation;
    private bool isJumping;
    private bool wasGrounded;
    private float originalFOV;
    private float currentFOV;
    private float headBobTimer;
    private Vector3 originalCameraPosition;

    private float CurrentMoveSpeed => inputReader.IsSprinting ? runSpeed : walkSpeed;
    private bool IsMoving => inputReader.MoveValue.magnitude > 0.1f && controller.isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        cam = playerCamera != null ? playerCamera : Camera.main;
        originalCameraPosition = cam.transform.localPosition;
        originalFOV = cam.fieldOfView;
        currentFOV = originalFOV;

        if (inputReader == null)
            Debug.LogError("InputReader не назначен в инспекторе!");

        //Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!GetComponent<CharacterController>().enabled) return;
        if (Time.timeScale == 0f)
        {
            return; 
        }
        HandleLook();
        HandleFOVKick();
        HandleMovement();
        HandleHeadBob();
    }

    private void FixedUpdate()
    {
        
    }

    private void HandleLook()
    {
      
        float yRot = inputReader.LookValue.x * mouseSensitivity;
        transform.Rotate(Vector3.up, yRot);

      
        float xRot = inputReader.LookValue.y * mouseSensitivity;
        cameraXRotation -= xRot;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -maxLookAngle, maxLookAngle);

        if (smoothMouse)
        {
            Quaternion targetRot = Quaternion.Euler(cameraXRotation, 0, 0);
            cam.transform.localRotation = Quaternion.Slerp(cam.transform.localRotation, targetRot, smoothTime * Time.deltaTime);
        }
        else
        {
            cam.transform.localRotation = Quaternion.Euler(cameraXRotation, 0, 0);
        }

        // Cursor lock
        //if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
        //{
        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;
        //}
        //else if (Mouse.current?.leftButton.wasPressedThisFrame ?? false)
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //}
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;


        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 desiredMove = (forward * inputReader.MoveValue.y + right * inputReader.MoveValue.x).normalized;


        float currentSpeed = CurrentMoveSpeed;
        moveDirection.x = desiredMove.x * currentSpeed;
        moveDirection.z = desiredMove.z * currentSpeed;


        if (isGrounded)
        {
            moveDirection.y = -stickToGroundForce;

            if (inputReader.JumpPressed && !isJumping)
            {
                moveDirection.y = jumpSpeed;
                isJumping = true;
                inputReader.ConsumeJump();
            }
        }
        else
        {
            moveDirection.y += Physics.gravity.y * gravityMultiplier * Time.fixedDeltaTime;
        }


        controller.Move(moveDirection * Time.fixedDeltaTime);


        if (!wasGrounded && isGrounded)
        {
            isJumping = false;
        }

        wasGrounded = isGrounded;
    }

    private void HandleHeadBob()
    {
        if (!useHeadBob) return;

        if (IsMoving && controller.isGrounded)
        {
            float speedMultiplier = inputReader.IsSprinting ? runstepLenghten : 1f;
            headBobTimer += Time.fixedDeltaTime * headBobSpeed * speedMultiplier;

            float bobX = Mathf.Sin(headBobTimer) * headBobAmount;
            float bobY = Mathf.Sin(headBobTimer * 2f) * (headBobAmount * 0.5f);

            cam.transform.localPosition = originalCameraPosition + new Vector3(bobX, bobY, 0);
        }
        else
        {
            headBobTimer = 0;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, originalCameraPosition, Time.fixedDeltaTime * 10f);
        }
    }

    private void HandleFOVKick()
    {
        if (!useFovKick) return;

        float targetFOV = inputReader.IsSprinting && IsMoving ? originalFOV + fovKickAmount : originalFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime / fovKickTime);
        cam.fieldOfView = currentFOV;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body != null && !body.isKinematic && controller.velocity.magnitude > 1f)
        {
            body.AddForceAtPosition(controller.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }

    //«¬” » ( потом добавить) 
    //
    // [Header("Audio")]
    // [SerializeField] private AudioClip[] footstepSounds;
    // [SerializeField] private AudioClip jumpSound;
    // [SerializeField] private AudioClip landSound;
    // [SerializeField] private float stepInterval = 0.5f;
    //
    // private AudioSource audioSource;
    // private float stepTimer;
    //
    // private void Start() {
    //     audioSource = GetComponent<AudioSource>();
    // }
    //
    // private void HandleFootsteps() {
    //     if (IsMoving && controller.isGrounded && !isJumping) {
    //         stepTimer -= Time.fixedDeltaTime;
    //         if (stepTimer <= 0) {
    //             PlayFootstep();
    //             float speedMultiplier = inputReader.IsSprinting ? 0.7f : 1f;
    //             stepTimer = stepInterval * speedMultiplier;
    //         }
    //     } else {
    //         stepTimer = 0;
    //     }
    // }
    //
    // private void PlayFootstep() {
    //     if (footstepSounds.Length > 0) {
    //         AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
    //         audioSource.PlayOneShot(clip);
    //     }
    // }
    //
    // private void PlayJumpSound() {
    //     if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
    // }
    //
    // private void PlayLandSound() {
    //     if (landSound != null) audioSource.PlayOneShot(landSound);
    // }
    //

    // - PlayJumpSound() в HandleMovement() при прыжке
    // - PlayLandSound() в HandleMovement() при приземлении
    // - HandleFootsteps() в FixedUpdate()
}