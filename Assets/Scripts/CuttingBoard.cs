using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Доска для нарезки овощей.
/// Имеет один слот для овоща. Игрок может положить овощ, порезать его ножом, и забрать нарезанный.
/// </summary>
public class CuttingBoard : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [SerializeField] private Outline outlineComponent;
    [SerializeField] private float outlineWidthOnLook = 10f;
    [SerializeField] private float outlineWidthDefault = 0f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Board Settings")]
    [Tooltip("Слот для овоща на доске")]
    [SerializeField] private Transform vegetableSlot;

    [Tooltip("Текущий овощ на доске")]
    private GameObject currentVegetable;

    [Tooltip("Тип овоща на доске")]
    private VegetableType currentVegetableType;

    [Tooltip("Порезан ли овощ")]
    private bool isVegetableChopped;

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
                Debug.LogWarning($"[CuttingBoard] Outline component not found on {gameObject.name}");
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
    }

    private void Update()
    {
        UpdateOutline();
    }

    private void UpdateOutline()
    {
        if (outlineComponent == null) return;

        currentOutlineWidth = Mathf.Lerp(currentOutlineWidth, targetOutlineWidth, Time.deltaTime * transitionSpeed);

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
    /// Положить овощ на доску
    /// </summary>
    public bool PlaceVegetable(GameObject vegetable, VegetableType type)
    {
        if (currentVegetable != null)
        {
            Debug.LogWarning("[CuttingBoard] Board already has a vegetable!");
            return false;
        }

        if (vegetableSlot == null)
        {
            Debug.LogWarning("[CuttingBoard] Vegetable slot not assigned!");
            return false;
        }

        currentVegetable = vegetable;
        currentVegetableType = type;
        isVegetableChopped = false;

        // Размещаем овощ на доске
        vegetable.transform.SetParent(vegetableSlot);
        vegetable.transform.localPosition = Vector3.zero;
        vegetable.transform.localRotation = Quaternion.identity;
        vegetable.SetActive(true);

        Debug.Log($"[CuttingBoard] Placed {type} vegetable on board");
        return true;
    }

    /// <summary>
    /// Забрать овощ с доски
    /// </summary>
    public GameObject TakeVegetable()
    {
        if (currentVegetable == null)
        {
            Debug.LogWarning("[CuttingBoard] No vegetable on board!");
            return null;
        }

        GameObject veg = currentVegetable;
        currentVegetable = null;
        isVegetableChopped = false;

        Debug.Log($"[CuttingBoard] Took vegetable from board");
        return veg;
    }

    /// <summary>
    /// Порезать овощ (вызывается после успешной нарезки ножом)
    /// </summary>
    public void ChopVegetable(GameObject choppedPrefab)
    {
        if (currentVegetable == null)
        {
            Debug.LogWarning("[CuttingBoard] No vegetable to chop!");
            return;
        }

        if (isVegetableChopped)
        {
            Debug.LogWarning("[CuttingBoard] Vegetable already chopped!");
            return;
        }

        // Уничтожаем старый овощ
        Destroy(currentVegetable);

        // Создаем нарезанный овощ
        currentVegetable = Instantiate(choppedPrefab, vegetableSlot);
        currentVegetable.transform.localPosition = Vector3.zero;
        currentVegetable.transform.localRotation = Quaternion.identity;
        isVegetableChopped = true;

        Debug.Log($"[CuttingBoard] Vegetable chopped! Type: {currentVegetableType}");
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

    public bool CanBePickedUp()
    {
        return false; // Доску нельзя поднять
    }

    public void OnPickup(Transform handTransform)
    {
        // Доску нельзя поднять
    }

    public void OnDrop()
    {
        // Доску нельзя поднять
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Доску нельзя переместить
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        // Доску нельзя переместить, превью не нужно
    }

    public void HidePlacementPreview()
    {
        // Доску нельзя переместить, превью не нужно
    }

    // Public getters
    public bool HasVegetable() => currentVegetable != null;
    public bool IsVegetableChopped() => isVegetableChopped;
    public VegetableType GetVegetableType() => currentVegetableType;
    public Transform GetVegetableSlot() => vegetableSlot;
}
