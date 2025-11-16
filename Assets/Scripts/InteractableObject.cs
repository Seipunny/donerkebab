using UnityEngine;

/// <summary>
/// Компонент для интерактивных объектов (двери, окна), которые открываются/закрываются
/// при взаимодействии. Использует процедурную анимацию с затуханием.
/// </summary>
public class InteractableObject : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [Tooltip("Outline компонент для подсветки")]
    [SerializeField] private Outline outlineComponent;

    [Tooltip("Ширина outline при наведении")]
    [SerializeField] private float outlineWidthOnLook = 10f;

    [Tooltip("Ширина outline по умолчанию")]
    [SerializeField] private float outlineWidthDefault = 0f;

    [Tooltip("Скорость изменения outline")]
    [SerializeField] private float outlineTransitionSpeed = 10f;

    [Header("Interaction Settings")]
    [Tooltip("Градус поворота по Y когда закрыто")]
    [SerializeField] private float closedAngleY = 0f;

    [Tooltip("Градус поворота по Y когда открыто")]
    [SerializeField] private float openAngleY = 90f;

    [Tooltip("Скорость открытия/закрытия (чем больше, тем быстрее)")]
    [SerializeField] private float rotationSpeed = 5f;

    [Tooltip("Затухание (damping) - чем меньше, тем плавнее замедление")]
    [Range(0.01f, 1f)]
    [SerializeField] private float dampingFactor = 0.1f;

    [Tooltip("Начальное состояние (открыто/закрыто)")]
    [SerializeField] private bool startOpen = false;

    [Header("Audio (Optional)")]
    [Tooltip("Звук открытия")]
    [SerializeField] private AudioClip openSound;

    [Tooltip("Звук закрытия")]
    [SerializeField] private AudioClip closeSound;

    [Tooltip("Громкость звуков")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    // Private state
    private bool isOpen;
    private float currentAngleY;
    private float targetAngleY;
    private float angularVelocity;
    private AudioSource audioSource;

    // Outline state
    private float currentOutlineWidth;
    private float targetOutlineWidth;
    private bool isLookingAt;

    private void Awake()
    {
        // Проверяем Outline
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
            {
                Debug.LogWarning($"[InteractableObject] Outline component not found on {gameObject.name}");
            }
        }

        // Инициализируем состояние
        isOpen = startOpen;
        currentAngleY = isOpen ? openAngleY : closedAngleY;
        targetAngleY = currentAngleY;

        // Устанавливаем начальную ротацию
        Vector3 currentRotation = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentRotation.x, currentAngleY, currentRotation.z);

        // Инициализируем outline
        currentOutlineWidth = outlineWidthDefault;
        targetOutlineWidth = outlineWidthDefault;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = outlineWidthDefault;
            outlineComponent.enabled = false; // Выключаем в начале
        }

        // Создаем AudioSource если нужен звук
        if (openSound != null || closeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
            audioSource.volume = volume;
        }
    }

    private void Update()
    {
        // Обновляем outline
        UpdateOutline();

        // Обновляем ротацию с затуханием
        UpdateRotation();
    }

    private void UpdateOutline()
    {
        if (outlineComponent == null) return;

        // Плавно интерполируем ширину outline
        currentOutlineWidth = Mathf.Lerp(
            currentOutlineWidth,
            targetOutlineWidth,
            Time.deltaTime * outlineTransitionSpeed
        );

        outlineComponent.OutlineWidth = currentOutlineWidth;

        // Оптимизация: отключаем Outline когда ширина близка к 0
        const float disableThreshold = 0.05f;
        if (currentOutlineWidth <= disableThreshold && outlineComponent.enabled)
        {
            outlineComponent.enabled = false;
        }
        else if (currentOutlineWidth > disableThreshold && !outlineComponent.enabled)
        {
            outlineComponent.enabled = true;
        }
    }

    private void UpdateRotation()
    {
        // Используем SmoothDampAngle для плавного затухания
        currentAngleY = Mathf.SmoothDampAngle(
            currentAngleY,
            targetAngleY,
            ref angularVelocity,
            dampingFactor,
            rotationSpeed * 100f // Максимальная скорость
        );

        // Применяем ротацию
        Vector3 currentRotation = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentRotation.x, currentAngleY, currentRotation.z);
    }

    /// <summary>
    /// Переключить состояние открыто/закрыто
    /// </summary>
    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /// <summary>
    /// Открыть объект
    /// </summary>
    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        targetAngleY = openAngleY;

        // Проигрываем звук
        PlaySound(openSound);

        Debug.Log($"[InteractableObject] Opening {gameObject.name}");
    }

    /// <summary>
    /// Закрыть объект
    /// </summary>
    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        targetAngleY = closedAngleY;

        // Проигрываем звук
        PlaySound(closeSound);

        Debug.Log($"[InteractableObject] Closing {gameObject.name}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Реализация IRaycastTarget
    public void OnRaycastEnter()
    {
        targetOutlineWidth = outlineWidthOnLook;
        isLookingAt = true;
    }

    public void OnRaycastStay()
    {
        targetOutlineWidth = outlineWidthOnLook;
        isLookingAt = true;
    }

    public void OnRaycastExit()
    {
        targetOutlineWidth = outlineWidthDefault;
        isLookingAt = false;
    }

    public void OnPickup(Transform handTransform)
    {
        // Интерактивные объекты не поднимаются
    }

    public void OnDrop()
    {
        // Интерактивные объекты не поднимаются
    }

    public bool CanBePickedUp()
    {
        // Интерактивные объекты не поднимаются
        return false;
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        // Интерактивные объекты не размещаются
    }

    public void HidePlacementPreview()
    {
        // Интерактивные объекты не размещаются
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Интерактивные объекты не размещаются
    }

    // Публичные методы
    public bool IsOpen()
    {
        return isOpen;
    }

    public bool IsLookingAt()
    {
        return isLookingAt;
    }

    public float GetOpenProgress()
    {
        if (Mathf.Approximately(openAngleY, closedAngleY))
            return isOpen ? 1f : 0f;

        float normalizedAngle = Mathf.InverseLerp(closedAngleY, openAngleY, currentAngleY);
        return Mathf.Clamp01(normalizedAngle);
    }

    private void OnDisable()
    {
        // Сбрасываем outline
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = outlineWidthDefault;
        }
        targetOutlineWidth = outlineWidthDefault;
        currentOutlineWidth = outlineWidthDefault;
    }

    private void OnValidate()
    {
        if (outlineComponent != null && outlineComponent.gameObject != gameObject)
        {
            Debug.LogWarning($"[InteractableObject] Outline должен быть на том же GameObject. Current: {outlineComponent.gameObject.name}, Expected: {gameObject.name}");
        }
    }
}
