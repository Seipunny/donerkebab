using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Независимая система рейкаста, которая работает отдельно от контроллера камеры.
/// Оптимизирована для устранения дерганий и минимального влияния на производительность.
/// </summary>
public class CameraRaycastInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Трансформ рук для переноса объектов")]
    [SerializeField] private Transform handTransform;

    [Header("Raycast Settings")]
    [Tooltip("Дальность луча")]
    [SerializeField] private float raycastDistance = 10f;

    [Tooltip("Слои для проверки")]
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Performance Settings")]
    [Tooltip("Интервал между рейкастами в секундах (меньше = чаще, но тяжелее)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float raycastInterval = 0.1f; // 10 раз в секунду

    [Tooltip("Использовать FixedUpdate вместо Update (более стабильно)")]
    [SerializeField] private bool useFixedUpdate = false;

    [Header("Smoothing Settings")]
    [Tooltip("Минимальное время удержания взгляда на объекте перед активацией")]
    [Range(0f, 0.5f)]
    [SerializeField] private float minHoldTime = 0f;

    [Tooltip("Время задержки перед деактивацией при потере взгляда")]
    [Range(0f, 0.5f)]
    [SerializeField] private float exitDelay = 0f;

    [Header("Pickup Settings")]
    [Tooltip("Время удержания ЛКМ для подбора объекта (секунды)")]
    [SerializeField] private float pickupHoldTime = 1f;

    [Header("Placement Settings")]
    [Tooltip("Максимальная дистанция размещения объекта")]
    [SerializeField] private float maxPlacementDistance = 5f;

    [Tooltip("Отступ от поверхности при размещении")]
    [SerializeField] private float placementOffset = 0.1f;

    // Private variables
    private IRaycastTarget currentTarget;
    private IRaycastTarget pendingTarget;
    private float timeSinceLastRaycast;
    private float timeOnPendingTarget;
    private float timeOffCurrentTarget;
    private bool isInitialized;

    // Pickup variables
    private float lmbHoldTimer;
    private bool isHoldingLMB;
    private IRaycastTarget pickedUpObject;

    // Placement variables
    private bool isInPlacementMode;
    private Vector3 previewPosition;
    private Quaternion previewRotation;

    private void Awake()
    {
        // Автоматически находим камеру если не назначена
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError("[CameraRaycastInteraction] Camera not found! Please assign a camera.");
            enabled = false;
            return;
        }

        isInitialized = true;
        timeSinceLastRaycast = 0f;
        timeOnPendingTarget = 0f;
        timeOffCurrentTarget = 0f;
    }

    private void Update()
    {
        if (!isInitialized || useFixedUpdate) return;
        ProcessRaycast();
        ProcessPickupInput();
        ProcessPlacementMode();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || !useFixedUpdate) return;
        ProcessRaycast();
        ProcessPickupInput();
        ProcessPlacementMode();
    }

    private void ProcessRaycast()
    {
        timeSinceLastRaycast += useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

        // Выполняем рейкаст только с определенным интервалом
        if (timeSinceLastRaycast >= raycastInterval)
        {
            timeSinceLastRaycast = 0f;
            PerformRaycast();
        }

        // Обновляем таймеры
        UpdateTimers();
    }

    private void PerformRaycast()
    {
        if (targetCamera == null) return;

        // Луч из центра экрана
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Выполняем raycast
        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            // Пытаемся получить IRaycastTarget
            IRaycastTarget target = hit.collider.GetComponent<IRaycastTarget>();

            if (target != null)
            {
                HandleTargetHit(target);
            }
            else
            {
                HandleNoTarget();
            }
        }
        else
        {
            HandleNoTarget();
        }
    }

    private void HandleTargetHit(IRaycastTarget target)
    {
        if (currentTarget == target)
        {
            // Продолжаем смотреть на тот же объект
            currentTarget.OnRaycastStay();
            timeOffCurrentTarget = 0f;
            pendingTarget = null;
            timeOnPendingTarget = 0f;
        }
        else if (pendingTarget == target)
        {
            // Продолжаем смотреть на pending объект
            timeOffCurrentTarget += raycastInterval;
        }
        else
        {
            // Новый объект обнаружен
            pendingTarget = target;
            timeOnPendingTarget = 0f;
        }
    }

    private void HandleNoTarget()
    {
        pendingTarget = null;
        timeOnPendingTarget = 0f;

        if (currentTarget != null)
        {
            timeOffCurrentTarget += raycastInterval;
        }
    }

    private void UpdateTimers()
    {
        // Если задержки равны 0, не нужно обрабатывать таймеры
        if (minHoldTime <= 0f && exitDelay <= 0f)
        {
            // Мгновенная активация/деактивация
            if (pendingTarget != null && currentTarget != pendingTarget)
            {
                currentTarget?.OnRaycastExit();
                currentTarget = pendingTarget;
                currentTarget.OnRaycastEnter();
                pendingTarget = null;
                timeOnPendingTarget = 0f;
                timeOffCurrentTarget = 0f;
            }
            return;
        }

        float deltaTime = useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

        // Проверяем активацию pending target
        if (pendingTarget != null && currentTarget != pendingTarget)
        {
            timeOnPendingTarget += deltaTime;

            if (timeOnPendingTarget >= minHoldTime)
            {
                // Деактивируем текущий
                if (currentTarget != null)
                {
                    currentTarget.OnRaycastExit();
                }

                // Активируем новый
                currentTarget = pendingTarget;
                currentTarget.OnRaycastEnter();

                // Сбрасываем таймеры
                pendingTarget = null;
                timeOnPendingTarget = 0f;
                timeOffCurrentTarget = 0f;
            }
        }

        // Проверяем деактивацию текущего target
        if (currentTarget != null && pendingTarget == null && timeOffCurrentTarget >= exitDelay)
        {
            currentTarget.OnRaycastExit();
            currentTarget = null;
            timeOffCurrentTarget = 0f;
        }
    }

    private void ProcessPickupInput()
    {
        // Если уже держим объект, пропускаем обработку
        if (pickedUpObject != null) return;

        // Проверяем ЛКМ
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (!isHoldingLMB)
            {
                isHoldingLMB = true;
                lmbHoldTimer = 0f;
            }

            // Если смотрим на объект, который можно поднять
            if (currentTarget != null && currentTarget.CanBePickedUp())
            {
                lmbHoldTimer += useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

                // Если держали достаточно долго - поднимаем
                if (lmbHoldTimer >= pickupHoldTime)
                {
                    PickupObject(currentTarget);
                    lmbHoldTimer = 0f;
                }
            }
            else
            {
                // Если не смотрим на объект, сбрасываем таймер
                lmbHoldTimer = 0f;
            }
        }
        else
        {
            // ЛКМ отпущена
            if (isHoldingLMB)
            {
                isHoldingLMB = false;
                lmbHoldTimer = 0f;
            }
        }
    }

    private void PickupObject(IRaycastTarget target)
    {
        if (handTransform == null)
        {
            Debug.LogWarning("[CameraRaycastInteraction] Hand transform not assigned! Cannot pickup object.");
            return;
        }

        pickedUpObject = target;
        target.OnPickup(handTransform);
        Debug.Log($"[CameraRaycastInteraction] Picked up object: {((MonoBehaviour)target).gameObject.name}");
    }

    private void ProcessPlacementMode()
    {
        // Работает только если держим объект
        if (pickedUpObject == null) return;

        // Проверяем ПКМ для входа в режим размещения
        bool rmbPressed = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (rmbPressed)
        {
            // Входим в режим размещения
            if (!isInPlacementMode)
            {
                EnterPlacementMode();
            }

            // Обновляем позицию превью
            UpdatePlacementPreview();

            // Проверяем ЛКМ для подтверждения размещения
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ConfirmPlacement();
            }
        }
        else
        {
            // Выходим из режима размещения
            if (isInPlacementMode)
            {
                ExitPlacementMode();
            }
        }
    }

    private void EnterPlacementMode()
    {
        isInPlacementMode = true;
        Debug.Log("[CameraRaycastInteraction] Entered placement mode");
    }

    private void ExitPlacementMode()
    {
        isInPlacementMode = false;
        if (pickedUpObject != null)
        {
            pickedUpObject.HidePlacementPreview();
        }
        Debug.Log("[CameraRaycastInteraction] Exited placement mode");
    }

    private void UpdatePlacementPreview()
    {
        if (targetCamera == null || pickedUpObject == null) return;

        // Луч из центра экрана
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Ограничиваем дистанцию размещения
        float placementDistance = Mathf.Min(raycastDistance, maxPlacementDistance);

        // Пытаемся найти поверхность
        if (Physics.Raycast(ray, out RaycastHit hit, placementDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            // Размещаем на поверхности с отступом
            previewPosition = hit.point + hit.normal * placementOffset;

            // Ориентируем по нормали поверхности
            previewRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            // Если не попали ни во что, размещаем на максимальной дистанции
            previewPosition = ray.origin + ray.direction * placementDistance;
            previewRotation = Quaternion.identity;
        }

        // Показываем превью
        pickedUpObject.ShowPlacementPreview(previewPosition, previewRotation);
    }

    private void ConfirmPlacement()
    {
        if (pickedUpObject == null) return;

        // Размещаем объект
        pickedUpObject.PlaceAt(previewPosition, previewRotation);

        Debug.Log($"[CameraRaycastInteraction] Placed object at {previewPosition}");

        // Очищаем состояние
        pickedUpObject = null;
        isInPlacementMode = false;
        lmbHoldTimer = 0f;
        isHoldingLMB = false;
    }

    private void OnDisable()
    {
        // Очищаем состояние при выключении
        if (currentTarget != null)
        {
            currentTarget.OnRaycastExit();
            currentTarget = null;
        }
        pendingTarget = null;
        timeOnPendingTarget = 0f;
        timeOffCurrentTarget = 0f;

        // Выходим из режима размещения
        if (isInPlacementMode)
        {
            ExitPlacementMode();
        }

        // Отпускаем объект если держим
        if (pickedUpObject != null)
        {
            pickedUpObject.OnDrop();
            pickedUpObject = null;
        }
    }

    private void OnDestroy()
    {
        // Очищаем при уничтожении
        if (currentTarget != null)
        {
            currentTarget.OnRaycastExit();
            currentTarget = null;
        }

        // Выходим из режима размещения
        if (isInPlacementMode)
        {
            ExitPlacementMode();
        }

        // Отпускаем объект если держим
        if (pickedUpObject != null)
        {
            pickedUpObject.OnDrop();
            pickedUpObject = null;
        }
    }

    // Для отладки в редакторе
    private void OnDrawGizmos()
    {
        if (targetCamera == null) return;

        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Gizmos.color = currentTarget != null ? Color.green : (pendingTarget != null ? Color.yellow : Color.red);
        Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);

        // Показываем точку попадания
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            Gizmos.DrawWireSphere(hit.point, 0.1f);
        }
    }

    // Публичные методы для управления из других скриптов
    public void SetRaycastEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    public bool IsLookingAtTarget()
    {
        return currentTarget != null;
    }

    public IRaycastTarget GetCurrentTarget()
    {
        return currentTarget;
    }

    public bool IsHoldingObject()
    {
        return pickedUpObject != null;
    }

    public IRaycastTarget GetPickedUpObject()
    {
        return pickedUpObject;
    }

    public void DropObject()
    {
        if (pickedUpObject != null)
        {
            pickedUpObject.OnDrop();
            pickedUpObject = null;
            lmbHoldTimer = 0f;
            isHoldingLMB = false;
        }
    }

    public float GetPickupProgress()
    {
        if (pickupHoldTime <= 0f) return 0f;
        return Mathf.Clamp01(lmbHoldTimer / pickupHoldTime);
    }

    public bool IsInPlacementMode()
    {
        return isInPlacementMode;
    }

    public Vector3 GetPreviewPosition()
    {
        return previewPosition;
    }

    public Quaternion GetPreviewRotation()
    {
        return previewRotation;
    }
}
