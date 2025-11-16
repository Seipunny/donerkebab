using UnityEngine;

/// <summary>
/// Мусорка для утилизации пустых ящиков
/// </summary>
public class TrashBin : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [SerializeField] private Outline outlineComponent;
    [SerializeField] private float outlineWidthOnLook = 10f;
    [SerializeField] private float outlineWidthDefault = 0f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip disposeSound;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource audioSource;

    // Outline state
    private float currentOutlineWidth;
    private float targetOutlineWidth;

    private void Awake()
    {
        // Проверяем Outline
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
            {
                Debug.LogWarning($"[TrashBin] Outline component not found on {gameObject.name}");
            }
        }

        // Инициализируем outline
        currentOutlineWidth = outlineWidthDefault;
        targetOutlineWidth = outlineWidthDefault;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = outlineWidthDefault;
            outlineComponent.enabled = false;
        }

        // Создаем AudioSource если нужен звук
        if (disposeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
            audioSource.volume = volume;
        }
    }

    private void Update()
    {
        UpdateOutline();
    }

    private void UpdateOutline()
    {
        if (outlineComponent == null) return;

        currentOutlineWidth = Mathf.Lerp(
            currentOutlineWidth,
            targetOutlineWidth,
            Time.deltaTime * transitionSpeed
        );

        outlineComponent.OutlineWidth = currentOutlineWidth;

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

    /// <summary>
    /// Утилизировать пустой ящик
    /// </summary>
    public void DisposeBox(VegetableBox box)
    {
        if (box == null)
        {
            Debug.LogWarning("[TrashBin] No box to dispose!");
            return;
        }

        if (!box.IsEmpty())
        {
            Debug.LogWarning("[TrashBin] Cannot dispose box with vegetables inside!");
            return;
        }

        // Проигрываем звук
        if (audioSource != null && disposeSound != null)
        {
            audioSource.PlayOneShot(disposeSound);
        }

        // Уничтожаем ящик
        box.DisposeBox();

        Debug.Log($"[TrashBin] Box disposed successfully");
    }

    // IRaycastTarget implementation
    public void OnRaycastEnter()
    {
        targetOutlineWidth = outlineWidthOnLook;
    }

    public void OnRaycastStay()
    {
        targetOutlineWidth = outlineWidthOnLook;
    }

    public void OnRaycastExit()
    {
        targetOutlineWidth = outlineWidthDefault;
    }

    public void OnPickup(Transform handTransform)
    {
        // Мусорка не поднимается
    }

    public void OnDrop()
    {
        // Мусорка не поднимается
    }

    public bool CanBePickedUp()
    {
        return false; // Мусорку нельзя поднять
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        // Мусорка не размещается
    }

    public void HidePlacementPreview()
    {
        // Мусорка не размещается
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Мусорка не размещается
    }
}
