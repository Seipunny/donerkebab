using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Tooltip("GroundCheck Transform от FPS контроллера для размещения объектов")]
    [SerializeField] private Transform groundCheckTransform;

    [Header("Raycast Settings")]
    [Tooltip("Дальность луча")]
    [SerializeField] private float raycastDistance = 10f;

    [Tooltip("Слои для проверки")]
    [SerializeField] private LayerMask interactionLayers = ~0;


    [Header("UI References")]
    [Tooltip("UI Image для прогресс бара подбора (filled radial)")]
    [SerializeField] private Image pickupProgressBar;

    [Header("Pickup Settings")]
    [Tooltip("Время удержания ЛКМ для подбора объекта (секунды)")]
    [SerializeField] private float pickupHoldTime = 1f;

    // Private variables
    private IRaycastTarget currentTarget;
    private bool isInitialized;

    // Pickup variables
    private float lmbHoldTimer;
    private bool isHoldingLMB;
    private IRaycastTarget pickedUpObject;

    // Vegetable portion in hand
    private List<GameObject> vegetablesInHand = new List<GameObject>();
    private VegetableType vegetablesInHandType;
    private bool isMovingVegetablesToHand;
    private List<Vector3> vegetableVelocities = new List<Vector3>();

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

        // Инициализируем UI
        if (pickupProgressBar != null)
        {
            pickupProgressBar.fillAmount = 0f;
            pickupProgressBar.gameObject.SetActive(false);
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;
        ProcessRaycast();
        ProcessPickupInput();
        ProcessPlacementInput();
        UpdatePickupUI();
        UpdateVegetablesMovement();
    }

    private void UpdateVegetablesMovement()
    {
        if (!isMovingVegetablesToHand || handTransform == null) return;

        bool allReached = true;

        for (int i = 0; i < vegetablesInHand.Count; i++)
        {
            if (vegetablesInHand[i] == null) continue;

            Vector3 currentVelocity = vegetableVelocities[i];

            vegetablesInHand[i].transform.position = Vector3.SmoothDamp(
                vegetablesInHand[i].transform.position,
                handTransform.position,
                ref currentVelocity,
                0.2f
            );

            vegetableVelocities[i] = currentVelocity;

            vegetablesInHand[i].transform.rotation = Quaternion.Slerp(
                vegetablesInHand[i].transform.rotation,
                handTransform.rotation,
                Time.deltaTime * 5f
            );

            float distanceToTarget = Vector3.Distance(vegetablesInHand[i].transform.position, handTransform.position);
            if (distanceToTarget >= 0.05f)
            {
                allReached = false;
            }
        }

        if (allReached)
        {
            foreach (var veg in vegetablesInHand)
            {
                if (veg != null)
                {
                    veg.transform.SetParent(handTransform);
                    veg.transform.localPosition = Vector3.zero;
                    veg.transform.localRotation = Quaternion.identity;
                }
            }

            isMovingVegetablesToHand = false;
            Debug.Log("[CameraRaycastInteraction] All vegetables reached hand");
        }
    }

    private void ProcessRaycast()
    {
        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (targetCamera == null) return;

        // Луч из центра экрана
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Выполняем raycast
        IRaycastTarget newTarget = null;
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            newTarget = hit.collider.GetComponent<IRaycastTarget>();
        }

        // Обрабатываем изменение цели
        if (newTarget != currentTarget)
        {
            // Деактивируем старую цель
            if (currentTarget != null)
            {
                currentTarget.OnRaycastExit();
            }

            // Активируем новую цель
            if (newTarget != null)
            {
                newTarget.OnRaycastEnter();
            }

            currentTarget = newTarget;
        }
        else if (currentTarget != null)
        {
            // Продолжаем смотреть на ту же цель
            currentTarget.OnRaycastStay();
        }
    }

    private void ProcessPickupInput()
    {
        // Проверяем ЛКМ клик для любых неподнимаемых объектов (работает всегда, даже если держим предмет)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentTarget != null)
            {
                // Проверяем все типы интерактивных объектов
                bool isInteractable = !currentTarget.CanBePickedUp();

                if (isInteractable)
                {
                    Debug.Log($"[CameraRaycastInteraction] Interacting with: {((MonoBehaviour)currentTarget).gameObject.name}");
                    InteractWithObject(currentTarget);
                    return;
                }
            }
        }

        // Если уже держим объект, пропускаем обработку подбора
        if (pickedUpObject != null) return;

        // Проверяем ЛКМ удержание для подбора
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
                lmbHoldTimer += Time.deltaTime;

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

    private void InteractWithObject(IRaycastTarget target)
    {
        // Проверяем если это интерактивный объект (дверь/окно)
        if (target is InteractableObject interactable)
        {
            interactable.Toggle();
            Debug.Log($"[CameraRaycastInteraction] Interacted with {((MonoBehaviour)target).gameObject.name}");
            return;
        }

        // Проверяем если это контейнер для овощей
        if (target is VegetableContainer container)
        {
            HandleContainerInteraction(container);
            return;
        }

        // Проверяем если это мусорка
        if (target is TrashBin trashBin)
        {
            HandleTrashBinInteraction(trashBin);
            return;
        }
    }

    private void HandleContainerInteraction(VegetableContainer container)
    {
        Debug.Log($"[CameraRaycastInteraction] HandleContainerInteraction called. Holding object: {pickedUpObject != null}, Holding vegetables: {vegetablesInHand.Count}");

        // Если держим порцию овощей - возвращаем их в контейнер
        if (vegetablesInHand.Count > 0)
        {
            // Проверяем совпадение типов
            if (vegetablesInHandType == container.GetVegetableType())
            {
                ReturnVegetablesToContainer(container);
            }
            else
            {
                Debug.LogWarning($"[CameraRaycastInteraction] Type mismatch! Holding {vegetablesInHandType}, Container is {container.GetVegetableType()}");
            }
            return;
        }

        // Если держим ящик с овощами - переносим овощи в контейнер
        if (pickedUpObject != null)
        {
            MonoBehaviour pickedMono = pickedUpObject as MonoBehaviour;
            if (pickedMono != null)
            {
                VegetableBox box = pickedMono.GetComponent<VegetableBox>();

                if (box != null)
                {
                    Debug.Log($"[CameraRaycastInteraction] Found VegetableBox. Is empty: {box.IsEmpty()}, Vegetable count: {box.GetVegetableCount()}");

                    if (!box.IsEmpty())
                    {
                        bool success = box.TransferVegetablesToContainer(container);
                        if (success)
                        {
                            Debug.Log("[CameraRaycastInteraction] Vegetables transferred to container!");
                        }
                        else
                        {
                            Debug.LogWarning("[CameraRaycastInteraction] Failed to transfer vegetables!");
                        }
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("[CameraRaycastInteraction] Box is empty!");
                    }
                }
                else
                {
                    Debug.LogWarning("[CameraRaycastInteraction] Picked object is not a VegetableBox!");
                }
            }
        }
        // Если руки свободны - забрать овощи из контейнера
        else if (pickedUpObject == null && vegetablesInHand.Count == 0)
        {
            if (!container.IsEmpty())
            {
                TakeVegetablesFromContainer(container);
            }
            else
            {
                Debug.Log("[CameraRaycastInteraction] Container is empty!");
            }
        }
    }

    private void ReturnVegetablesToContainer(VegetableContainer container)
    {
        if (vegetablesInHand.Count == 0) return;

        bool success = container.ReturnVegetables(vegetablesInHand);

        if (success)
        {
            Debug.Log($"[CameraRaycastInteraction] Returned {vegetablesInHand.Count} vegetables to container");

            // Очищаем руки
            vegetablesInHand.Clear();
            vegetableVelocities.Clear();
        }
        else
        {
            Debug.LogWarning("[CameraRaycastInteraction] Failed to return vegetables to container!");
        }
    }

    private void TakeVegetablesFromContainer(VegetableContainer container)
    {
        if (handTransform == null)
        {
            Debug.LogWarning("[CameraRaycastInteraction] Hand transform not assigned!");
            return;
        }

        // Берем овощи из контейнера (последние 4)
        List<GameObject> takenVegetables = container.TakeVegetables(container.GetTransferAmount());

        if (takenVegetables.Count > 0)
        {
            vegetablesInHand = takenVegetables;
            vegetablesInHandType = container.GetVegetableType();
            vegetableVelocities.Clear();

            // Инициализируем velocity для каждого овоща
            for (int i = 0; i < vegetablesInHand.Count; i++)
            {
                vegetableVelocities.Add(Vector3.zero);
                if (vegetablesInHand[i] != null)
                {
                    vegetablesInHand[i].SetActive(true);
                }
            }

            isMovingVegetablesToHand = true;

            Debug.Log($"[CameraRaycastInteraction] Took {takenVegetables.Count} {container.GetVegetableType()} vegetables from container!");
        }
        else
        {
            Debug.LogWarning("[CameraRaycastInteraction] Failed to take vegetables from container!");
        }
    }

    private void HandleTrashBinInteraction(TrashBin trashBin)
    {
        Debug.Log($"[CameraRaycastInteraction] HandleTrashBinInteraction called. Holding object: {pickedUpObject != null}");

        // Если держим пустой ящик - выбросить его
        if (pickedUpObject != null)
        {
            MonoBehaviour pickedMono = pickedUpObject as MonoBehaviour;
            if (pickedMono != null)
            {
                VegetableBox box = pickedMono.GetComponent<VegetableBox>();

                if (box != null)
                {
                    if (box.IsEmpty())
                    {
                        trashBin.DisposeBox(box);
                        pickedUpObject = null;
                        Debug.Log("[CameraRaycastInteraction] Empty box disposed in trash!");
                    }
                    else
                    {
                        Debug.LogWarning($"[CameraRaycastInteraction] Cannot dispose box with {box.GetVegetableCount()} {box.GetVegetableType()} vegetables inside! Empty it first.");
                    }
                }
                else
                {
                    Debug.LogWarning("[CameraRaycastInteraction] Picked object is not a VegetableBox!");
                }
            }
        }
        else
        {
            Debug.Log("[CameraRaycastInteraction] No object in hands");
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

    private void ProcessPlacementInput()
    {
        // Проверяем клавишу G для размещения/дропа
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            // Если держим объект - размещаем его
            if (pickedUpObject != null)
            {
                PlaceObjectAtGround();
            }
            // Если держим порцию овощей - выбрасываем их
            else if (vegetablesInHand.Count > 0)
            {
                DropVegetables();
            }
        }
    }

    private void DropVegetables()
    {
        if (vegetablesInHand.Count == 0) return;

        Debug.Log($"[CameraRaycastInteraction] Dropping {vegetablesInHand.Count} {vegetablesInHandType} vegetables");

        // Уничтожаем овощи
        foreach (var veg in vegetablesInHand)
        {
            if (veg != null)
            {
                Destroy(veg);
            }
        }

        vegetablesInHand.Clear();
        vegetableVelocities.Clear();
    }

    private void PlaceObjectAtGround()
    {
        if (pickedUpObject == null) return;

        if (groundCheckTransform == null)
        {
            Debug.LogWarning("[CameraRaycastInteraction] GroundCheck transform not assigned! Cannot place object.");
            return;
        }

        // Размещаем объект впереди GroundCheck с оффсетом
        float forwardOffset = 0.7f; // Смещение вперед
        float yOffset = -0.03f;      // Смещение по Y

        Vector3 placePosition = groundCheckTransform.position +
                                groundCheckTransform.forward * forwardOffset +
                                Vector3.up * yOffset;

        // Используем ротацию GroundCheck (родительский угол)
        Quaternion placeRotation = groundCheckTransform.rotation;

        pickedUpObject.PlaceAt(placePosition, placeRotation);

        Debug.Log($"[CameraRaycastInteraction] Placed object at ground: {placePosition}");

        // Очищаем состояние
        pickedUpObject = null;
        lmbHoldTimer = 0f;
        isHoldingLMB = false;
    }

    private void UpdatePickupUI()
    {
        if (pickupProgressBar == null) return;

        // Показываем прогресс бар только когда удерживаем ЛКМ на поднимаемом объекте
        bool shouldShowProgress = isHoldingLMB &&
                                  currentTarget != null &&
                                  currentTarget.CanBePickedUp() &&
                                  pickedUpObject == null;

        if (shouldShowProgress)
        {
            if (!pickupProgressBar.gameObject.activeSelf)
            {
                pickupProgressBar.gameObject.SetActive(true);
            }

            // Обновляем fillAmount (0 to 1)
            float progress = Mathf.Clamp01(lmbHoldTimer / pickupHoldTime);
            pickupProgressBar.fillAmount = progress;
        }
        else
        {
            if (pickupProgressBar.gameObject.activeSelf)
            {
                pickupProgressBar.gameObject.SetActive(false);
                pickupProgressBar.fillAmount = 0f;
            }
        }
    }

    private void OnDisable()
    {
        // Очищаем состояние при выключении
        if (currentTarget != null)
        {
            currentTarget.OnRaycastExit();
            currentTarget = null;
        }

        // Отпускаем объект если держим
        if (pickedUpObject != null)
        {
            pickedUpObject.OnDrop();
            pickedUpObject = null;
        }

        // Удаляем овощи из рук
        foreach (var veg in vegetablesInHand)
        {
            if (veg != null) Destroy(veg);
        }
        vegetablesInHand.Clear();
        vegetableVelocities.Clear();

        // Скрываем UI
        if (pickupProgressBar != null && pickupProgressBar.gameObject.activeSelf)
        {
            pickupProgressBar.gameObject.SetActive(false);
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

        // Отпускаем объект если держим
        if (pickedUpObject != null)
        {
            pickedUpObject.OnDrop();
            pickedUpObject = null;
        }

        // Удаляем овощи из рук
        foreach (var veg in vegetablesInHand)
        {
            if (veg != null) Destroy(veg);
        }
        vegetablesInHand.Clear();
        vegetableVelocities.Clear();
    }

    // Для отладки в редакторе
    private void OnDrawGizmos()
    {
        if (targetCamera == null) return;

        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Gizmos.color = currentTarget != null ? Color.green : Color.red;
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
}
