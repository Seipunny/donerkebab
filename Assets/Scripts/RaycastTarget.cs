using UnityEngine;

public class RaycastTarget : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [Tooltip("Outline компонент для управления")]
    [SerializeField] private Outline outlineComponent;

    [Tooltip("Ширина outline при взгляде")]
    [SerializeField] private float outlineWidthOnLook = 10f;

    [Tooltip("Ширина outline когда не смотрим")]
    [SerializeField] private float outlineWidthDefault = 0f;

    [Header("Animation Settings")]
    [Tooltip("Скорость изменения ширины outline")]
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Pickup Settings")]
    [Tooltip("Можно ли поднять этот объект")]
    [SerializeField] private bool canBePickedUp = true;

    [Tooltip("Скорость перемещения к руке")]
    [SerializeField] private float pickupSpeed = 10f;

    [Header("Placement Preview Settings")]
    [Tooltip("Цвет силуэта превью (полупрозрачный)")]
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.5f);

    private float targetWidth;
    private float currentWidth;

    // Pickup state
    private bool isPickedUp;
    private Transform targetHandTransform;
    private Vector3 pickupVelocity;
    private Rigidbody rb;
    private Collider[] colliders;
    private bool hadGravity;
    private bool wasKinematic;

    // Placement state
    private bool isPlacing;
    private Vector3 targetPlacePosition;
    private Quaternion targetPlaceRotation;
    private Vector3 placeVelocity;

    // Placement preview state
    private bool isShowingPreview;
    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private GameObject previewGhost;
    private Renderer[] originalRenderers;
    private Material[] ghostMaterials;

    private void Awake()
    {
        // Проверяем что Outline назначен
        if (outlineComponent == null)
        {
            // Пытаемся найти на том же объекте
            outlineComponent = GetComponent<Outline>();

            if (outlineComponent == null)
            {
                Debug.LogWarning($"[RaycastTarget] Outline component not found on {gameObject.name}");
            }
        }

        // Инициализируем начальные значения
        targetWidth = outlineWidthDefault;
        currentWidth = outlineWidthDefault;

        // Устанавливаем начальную ширину и отключаем компонент
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = outlineWidthDefault;
            outlineComponent.enabled = false; // Выключаем в начале
        }

        // Кешируем компоненты для pickup
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        originalRenderers = GetComponentsInChildren<Renderer>();
    }

    public void OnRaycastEnter()
    {
        // Устанавливаем целевую ширину на максимум
        targetWidth = outlineWidthOnLook;
    }

    public void OnRaycastStay()
    {
        // Продолжаем держать целевую ширину на максимуме
        targetWidth = outlineWidthOnLook;
    }

    public void OnRaycastExit()
    {
        // Возвращаем ширину к нулю
        targetWidth = outlineWidthDefault;
    }

    private void Update()
    {
        if (outlineComponent == null) return;

        // Плавно интерполируем текущую ширину к целевой
        currentWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * transitionSpeed);

        // Применяем ширину к Outline
        outlineComponent.OutlineWidth = currentWidth;

        // Оптимизация: отключаем Outline когда ширина близка к 0
        const float disableThreshold = 0.05f;
        if (currentWidth <= disableThreshold && outlineComponent.enabled)
        {
            outlineComponent.enabled = false;
        }
        else if (currentWidth > disableThreshold && !outlineComponent.enabled)
        {
            outlineComponent.enabled = true;
        }

        // Обрабатываем перемещение к руке
        if (isPickedUp && targetHandTransform != null && !isShowingPreview && !isPlacing)
        {
            ProcessPickupMovement();
        }

        // Обрабатываем анимацию размещения
        if (isPlacing)
        {
            ProcessPlacementMovement();
        }

        // Обновляем позицию превью
        if (isShowingPreview && previewGhost != null)
        {
            previewGhost.transform.position = previewPosition;
            previewGhost.transform.rotation = previewRotation;
        }
    }

    private void ProcessPickupMovement()
    {
        // Плавно перемещаем объект к позиции руки
        Vector3 targetPosition = targetHandTransform.position;
        Quaternion targetRotation = targetHandTransform.rotation;

        // Используем SmoothDamp для плавного перемещения
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref pickupVelocity,
            1f / pickupSpeed
        );

        // Плавно поворачиваем к целевой ориентации
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * pickupSpeed
        );
    }

    private void ProcessPlacementMovement()
    {
        // Плавно перемещаем объект к целевой позиции размещения
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPlacePosition,
            ref placeVelocity,
            1f / pickupSpeed // Используем ту же скорость что и для подбора
        );

        // Плавно поворачиваем к целевой ориентации
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetPlaceRotation,
            Time.deltaTime * pickupSpeed
        );

        // Проверяем достигли ли целевой позиции
        float distanceToTarget = Vector3.Distance(transform.position, targetPlacePosition);
        if (distanceToTarget < 0.01f)
        {
            // Размещение завершено
            transform.position = targetPlacePosition;
            transform.rotation = targetPlaceRotation;
            isPlacing = false;

            // Восстанавливаем физику
            if (rb != null)
            {
                rb.useGravity = hadGravity;
                rb.isKinematic = wasKinematic;
            }

            // Включаем коллайдеры
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }

            Debug.Log($"[RaycastTarget] Placement completed: {gameObject.name}");
        }
    }

    private void OnDisable()
    {
        // При отключении скрипта сбрасываем outline
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = outlineWidthDefault;
        }
        targetWidth = outlineWidthDefault;
        currentWidth = outlineWidthDefault;

        // Очищаем превью
        HidePlacementPreview();
    }

    private void OnDestroy()
    {
        // Очищаем превью при уничтожении
        DestroyPreviewGhost();
    }

    public void OnPickup(Transform handTransform)
    {
        if (!canBePickedUp)
        {
            Debug.LogWarning($"[RaycastTarget] Object {gameObject.name} cannot be picked up!");
            return;
        }

        isPickedUp = true;
        targetHandTransform = handTransform;
        pickupVelocity = Vector3.zero;

        // Отключаем физику
        if (rb != null)
        {
            hadGravity = rb.useGravity;
            wasKinematic = rb.isKinematic;

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Отключаем коллайдеры
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        Debug.Log($"[RaycastTarget] Picked up: {gameObject.name}");
    }

    public void OnDrop()
    {
        if (!isPickedUp) return;

        isPickedUp = false;
        targetHandTransform = null;
        pickupVelocity = Vector3.zero;

        // Восстанавливаем физику
        if (rb != null)
        {
            rb.useGravity = hadGravity;
            rb.isKinematic = wasKinematic;
        }

        // Включаем коллайдеры
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }

        Debug.Log($"[RaycastTarget] Dropped: {gameObject.name}");
    }

    public bool CanBePickedUp()
    {
        return canBePickedUp && !isPickedUp;
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        previewPosition = position;
        previewRotation = rotation;

        if (!isShowingPreview)
        {
            CreatePreviewGhost();
            isShowingPreview = true;
        }

        // Скрываем оригинальный объект
        SetRenderersEnabled(false);
    }

    public void HidePlacementPreview()
    {
        if (!isShowingPreview) return;

        DestroyPreviewGhost();
        isShowingPreview = false;

        // Показываем оригинальный объект
        SetRenderersEnabled(true);
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Скрываем превью
        HidePlacementPreview();

        // Запускаем анимацию размещения
        targetPlacePosition = position;
        targetPlaceRotation = rotation;
        placeVelocity = Vector3.zero;
        isPlacing = true;
        isPickedUp = false; // Больше не в руках

        Debug.Log($"[RaycastTarget] Starting placement animation to: {position}");
    }

    private void CreatePreviewGhost()
    {
        if (previewGhost != null)
        {
            DestroyPreviewGhost();
        }

        // Создаем копию объекта для превью
        previewGhost = new GameObject($"{gameObject.name}_Preview");
        previewGhost.transform.position = previewPosition;
        previewGhost.transform.rotation = previewRotation;
        previewGhost.transform.localScale = transform.lossyScale;

        // Копируем визуальные компоненты
        foreach (var renderer in originalRenderers)
        {
            if (renderer == null) continue;

            // Создаем копию меша
            MeshFilter originalMF = renderer.GetComponent<MeshFilter>();
            if (originalMF != null && originalMF.sharedMesh != null)
            {
                GameObject meshObj = new GameObject(renderer.name);
                meshObj.transform.SetParent(previewGhost.transform);
                meshObj.transform.localPosition = renderer.transform.localPosition;
                meshObj.transform.localRotation = renderer.transform.localRotation;
                meshObj.transform.localScale = renderer.transform.localScale;

                MeshFilter mf = meshObj.AddComponent<MeshFilter>();
                mf.sharedMesh = originalMF.sharedMesh;

                MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

                // Создаем прозрачный материал для силуэта
                Material ghostMat = new Material(Shader.Find("Standard"));
                ghostMat.color = previewColor;
                ghostMat.SetFloat("_Mode", 3); // Transparent mode
                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMat.renderQueue = 3000;

                mr.material = ghostMat;
            }
        }
    }

    private void DestroyPreviewGhost()
    {
        if (previewGhost != null)
        {
            Destroy(previewGhost);
            previewGhost = null;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (var renderer in originalRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    // Для отладки
    private void OnValidate()
    {
        if (outlineComponent != null && outlineComponent.gameObject != gameObject)
        {
            Debug.LogWarning($"[RaycastTarget] Outline component should be on the same GameObject as RaycastTarget. Current: {outlineComponent.gameObject.name}, Expected: {gameObject.name}");
        }
    }
}
