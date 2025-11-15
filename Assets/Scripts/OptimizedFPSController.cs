using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Оптимизированный FPS контроллер с правильным разделением логики.
/// Совместим с внешними системами (рейкасты, UI и т.д.)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OptimizedFPSController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraParent;
    [SerializeField] private Transform groundCheck;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float jumpForce = 3f;
    [SerializeField] private float gravity = 9.81f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 50f;
    [SerializeField] private float lookSmoothing = 100f;
    [SerializeField] private bool invertY = false;

    [Header("Crouch & Slide")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchCameraHeight = 0.5f;
    [SerializeField] private float standingCameraHeight = 0.9f;
    [SerializeField] private float slideSpeed = 8f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideFovBoost = 5f;
    [SerializeField] private float slideTiltAngle = 5f;

    [Header("Camera Effects")]
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float sprintFov = 70f;
    [SerializeField] private float fovTransitionSpeed = 5f;
    [SerializeField] private float headBobSpeed = 10f;
    [SerializeField] private float headBobAmount = 0.05f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Advanced Settings")]
    [SerializeField] private bool enableCoyoteTime = true;
    [SerializeField] private float coyoteTimeDuration = 0.2f;
    [SerializeField] private QueryTriggerInteraction groundCheckMode = QueryTriggerInteraction.Ignore;
    [SerializeField] private QueryTriggerInteraction ceilingCheckMode = QueryTriggerInteraction.Ignore;

    [Header("Feature Toggles")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canSlide = true;

    #endregion

    #region Private Variables

    // Components
    private CharacterController controller;
    private Camera mainCamera;
    private PlayerInputActions inputActions;

    // Movement
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isGrounded;
    private float coyoteTimer;

    // Camera rotation
    private float targetRotationX;
    private float targetRotationY;
    private float currentRotationX;
    private float currentRotationY;
    private float rotationXVelocity;
    private float rotationYVelocity;

    // State
    private bool isSprinting;
    private bool isCrouching;
    private bool isSliding;
    private Vector3 slideDirection;
    private float slideTimer;
    private float postSlideCrouchTimer;

    // Camera effects
    private float targetFov;
    private float currentFov;
    private float fovVelocity;
    private float headBobTimer;
    private float targetCameraHeight;
    private float currentCameraHeight;
    private float currentHeadBob;
    private float targetTiltAngle;
    private float currentTiltAngle;
    private float tiltVelocity;

    // Control flags
    private bool controlsEnabled = true;
    private bool lookEnabled = true;
    private bool moveEnabled = true;

    // Cached values
    private float originalControllerHeight;
    private Vector3 originalCameraLocalPos;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeInputSystem();
        InitializeValues();
        LockCursor();
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
        }
        else
        {
            Debug.LogWarning("[OptimizedFPSController] inputActions is null in OnEnable! Trying to reinitialize...");
            InitializeInputSystem();
            inputActions?.Enable();
        }
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        // Разделяем логику на четкие этапы для предсказуемости
        UpdateGroundCheck();
        UpdateInput();
        UpdateCameraRotation();
        UpdateMovement();
        UpdateCameraEffects();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            mainCamera = cameraTransform.GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("[OptimizedFPSController] Camera not found!");
        }
    }

    private void InitializeInputSystem()
    {
        inputActions = new PlayerInputActions();

        // Debug: проверяем что Input System создан
        if (inputActions == null)
        {
            Debug.LogError("[OptimizedFPSController] Failed to create PlayerInputActions!");
        }
    }

    private void InitializeValues()
    {
        // Сохраняем оригинальные значения
        originalControllerHeight = controller.height;
        standingHeight = originalControllerHeight;

        if (cameraParent != null)
        {
            originalCameraLocalPos = cameraParent.localPosition;
            standingCameraHeight = originalCameraLocalPos.y;
            targetCameraHeight = standingCameraHeight;
            currentCameraHeight = standingCameraHeight;
        }

        // Инициализируем камеру
        if (mainCamera != null)
        {
            currentFov = normalFov;
            targetFov = normalFov;
            mainCamera.fieldOfView = currentFov;
        }

        // Инициализируем вращение из текущей позиции
        targetRotationX = transform.eulerAngles.y;
        currentRotationX = targetRotationX;

        if (cameraTransform != null)
        {
            targetRotationY = cameraTransform.localEulerAngles.x;
            currentRotationY = targetRotationY;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region Ground Check

    private void UpdateGroundCheck()
    {
        if (groundCheck == null) return;

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckDistance,
            groundMask,
            groundCheckMode
        );

        // Обновляем coyote time
        if (isGrounded)
        {
            coyoteTimer = enableCoyoteTime ? coyoteTimeDuration : 0f;

            // Сбрасываем вертикальную скорость если на земле
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else if (enableCoyoteTime)
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region Input Processing

    private void UpdateInput()
    {
        if (!controlsEnabled || inputActions == null) return;

        // Читаем движение
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Проверяем спринт
        bool sprintInput = canSprint && inputActions.Player.Sprint.IsPressed();
        isSprinting = sprintInput && moveInput.y > 0.1f && isGrounded && !isCrouching && !isSliding;

        // Обрабатываем приседание и слайд
        HandleCrouchAndSlide();

        // Прыжок
        if (canJump && inputActions.Player.Jump.triggered && CanJump())
        {
            velocity.y = jumpForce;
        }
    }

    private bool CanJump()
    {
        return (isGrounded || coyoteTimer > 0f) && !isSliding;
    }

    private void HandleCrouchAndSlide()
    {
        bool crouchInput = inputActions.Player.Crouch.IsPressed();
        bool crouchPressed = inputActions.Player.Crouch.triggered;

        // Инициируем слайд
        if (canSlide && crouchPressed && isSprinting && isGrounded && !isSliding)
        {
            StartSlide();
            return;
        }

        // Обновляем слайд
        if (isSliding)
        {
            UpdateSlide();
            return;
        }

        // Проверяем потолок
        bool hasCeiling = CheckCeiling();

        // Post-slide crouch timer
        if (postSlideCrouchTimer > 0)
        {
            postSlideCrouchTimer -= Time.deltaTime;
            isCrouching = canCrouch && !hasCeiling;
        }
        else
        {
            isCrouching = canCrouch && (crouchInput || hasCeiling);
        }

        // Обновляем высоту контроллера
        UpdateControllerHeight();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        // Направление слайда
        if (moveInput.magnitude > 0.1f)
        {
            slideDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        }
        else
        {
            slideDirection = transform.forward;
        }
    }

    private void UpdateSlide()
    {
        slideTimer -= Time.deltaTime;

        if (slideTimer <= 0f || !isGrounded)
        {
            isSliding = false;
            postSlideCrouchTimer = 0.3f;
            return;
        }

        // Плавное замедление слайда
        float slideProgress = slideTimer / slideDuration;
        float currentSlideSpeed = slideSpeed * Mathf.Lerp(0.7f, 1f, slideProgress);

        controller.Move(slideDirection * currentSlideSpeed * Time.deltaTime);
    }

    private bool CheckCeiling()
    {
        Vector3 point1 = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
        Vector3 point2 = point1 + Vector3.up * (controller.height * 0.6f);
        float capsuleRadius = controller.radius * 0.95f;
        float castDistance = isSliding ? standingHeight + 0.2f : standingHeight - crouchHeight + 0.2f;

        return Physics.CapsuleCast(
            point1,
            point2,
            capsuleRadius,
            Vector3.up,
            castDistance,
            groundMask,
            ceilingCheckMode
        );
    }

    private void UpdateControllerHeight()
    {
        float targetHeight = (isCrouching || isSliding) ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
    }

    #endregion

    #region Camera Rotation

    private void UpdateCameraRotation()
    {
        if (!controlsEnabled || !lookEnabled || inputActions == null) return;

        // Читаем mouse input
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        if (invertY) mouseY = -mouseY;

        // Обновляем target rotation
        targetRotationX += mouseX;
        targetRotationY -= mouseY;
        targetRotationY = Mathf.Clamp(targetRotationY, -90f, 90f);

        // Плавное сглаживание
        currentRotationX = Mathf.SmoothDamp(
            currentRotationX,
            targetRotationX,
            ref rotationXVelocity,
            1f / lookSmoothing
        );

        currentRotationY = Mathf.SmoothDamp(
            currentRotationY,
            targetRotationY,
            ref rotationYVelocity,
            1f / lookSmoothing
        );

        // Применяем вращение
        transform.rotation = Quaternion.Euler(0f, currentRotationX, 0f);

        if (cameraTransform != null)
        {
            // Применяем tilt для слайда
            targetTiltAngle = isSliding ? slideTiltAngle : 0f;
            currentTiltAngle = Mathf.SmoothDamp(currentTiltAngle, targetTiltAngle, ref tiltVelocity, 0.2f);

            cameraTransform.localRotation = Quaternion.Euler(currentRotationY - currentTiltAngle, 0f, 0f);
        }
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        if (!controlsEnabled || !moveEnabled || isSliding)
        {
            ApplyGravity();
            return;
        }

        // Определяем скорость
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        // Вычисляем направление движения
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Горизонтальное движение
        Vector3 horizontalMove = moveDirection * currentSpeed;

        // Применяем гравитацию
        ApplyGravity();

        // Объединяем горизонтальное и вертикальное движение
        Vector3 finalMove = new Vector3(horizontalMove.x, velocity.y, horizontalMove.z);
        controller.Move(finalMove * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
    }

    #endregion

    #region Camera Effects

    private void UpdateCameraEffects()
    {
        UpdateFOV();
        UpdateHeadBob();
        UpdateCameraPosition();
    }

    private void UpdateFOV()
    {
        if (mainCamera == null) return;

        // Определяем target FOV
        if (isSliding)
        {
            float slideProgress = slideTimer / slideDuration;
            targetFov = sprintFov + (slideFovBoost * (1f - slideProgress));
        }
        else if (isSprinting)
        {
            targetFov = sprintFov;
        }
        else
        {
            targetFov = normalFov;
        }

        // Плавный переход
        currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, 1f / fovTransitionSpeed);
        mainCamera.fieldOfView = currentFov;
    }

    private void UpdateHeadBob()
    {
        if (cameraParent == null) return;

        // Определяем target высоту камеры
        targetCameraHeight = (isCrouching || isSliding) ? crouchCameraHeight : standingCameraHeight;

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.1f && isGrounded && !isSliding && !isCrouching;

        if (isMoving)
        {
            // Обновляем таймер head bob
            float bobSpeed = headBobSpeed * (isSprinting ? sprintBobMultiplier : 1f);
            headBobTimer += Time.deltaTime * bobSpeed;

            // Вычисляем head bob offset
            currentHeadBob = Mathf.Lerp(
                currentHeadBob,
                Mathf.Sin(headBobTimer) * headBobAmount,
                Time.deltaTime * headBobSpeed
            );
        }
        else
        {
            // Сбрасываем head bob
            headBobTimer = 0f;
            currentHeadBob = Mathf.Lerp(currentHeadBob, 0f, Time.deltaTime * headBobSpeed);
        }
    }

    private void UpdateCameraPosition()
    {
        if (cameraParent == null) return;

        // Плавно обновляем высоту камеры
        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 10f);

        // Применяем позицию с head bob
        cameraParent.localPosition = new Vector3(
            originalCameraLocalPos.x,
            currentCameraHeight + currentHeadBob,
            originalCameraLocalPos.z
        );
    }

    #endregion

    #region Public API

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        lookEnabled = enabled;
        moveEnabled = enabled;
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
    }

    public void SetMoveEnabled(bool enabled)
    {
        moveEnabled = enabled;
    }

    public void SetCursorVisible(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public bool IsGrounded => isGrounded;

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }

    #endregion
}
