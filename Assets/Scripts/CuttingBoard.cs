using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    [Tooltip("Слот для овощей на доске")]
    [SerializeField] private Transform vegetableSlot;

    [Tooltip("Группа овощей на доске")]
    private List<GameObject> currentVegetables = new List<GameObject>();

    [Tooltip("Тип овощей на доске")]
    private VegetableType currentVegetableType;

    [Tooltip("Порезаны ли овощи")]
    private bool isVegetablesChopped;

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
    /// Положить группу овощей на доску
    /// </summary>
    public bool PlaceVegetables(List<GameObject> vegetables, VegetableType type)
    {
        if (currentVegetables.Count > 0)
        {
            Debug.LogWarning("[CuttingBoard] Board already has vegetables!");
            return false;
        }

        if (vegetableSlot == null)
        {
            Debug.LogWarning("[CuttingBoard] Vegetable slot not assigned!");
            return false;
        }

        currentVegetables = new List<GameObject>(vegetables);
        currentVegetableType = type;
        isVegetablesChopped = false;

        // Размещаем все овощи на доске
        foreach (var veg in currentVegetables)
        {
            if (veg != null)
            {
                veg.transform.SetParent(vegetableSlot);
                veg.transform.localPosition = Vector3.zero;
                veg.transform.localRotation = Quaternion.identity;
                veg.SetActive(true);
            }
        }

        Debug.Log($"[CuttingBoard] Placed {vegetables.Count} {type} vegetables on board");
        return true;
    }

    /// <summary>
    /// Забрать группу овощей с доски
    /// </summary>
    public List<GameObject> TakeVegetables()
    {
        if (currentVegetables.Count == 0)
        {
            Debug.LogWarning("[CuttingBoard] No vegetables on board!");
            return new List<GameObject>();
        }

        List<GameObject> veggies = new List<GameObject>(currentVegetables);
        currentVegetables.Clear();
        isVegetablesChopped = false;

        Debug.Log($"[CuttingBoard] Took {veggies.Count} vegetables from board");
        return veggies;
    }

    /// <summary>
    /// Порезать овощи (вызывается после успешной нарезки ножом)
    /// </summary>
    public void ChopVegetables(GameObject choppedPrefab)
    {
        if (currentVegetables.Count == 0)
        {
            Debug.LogWarning("[CuttingBoard] No vegetables to chop!");
            return;
        }

        if (isVegetablesChopped)
        {
            Debug.LogWarning("[CuttingBoard] Vegetables already chopped!");
            return;
        }

        // Уничтожаем старые овощи
        foreach (var veg in currentVegetables)
        {
            if (veg != null)
            {
                Destroy(veg);
            }
        }
        currentVegetables.Clear();

        // Создаем нарезанный овощ (один объект вместо группы)
        GameObject choppedVeg = Instantiate(choppedPrefab, vegetableSlot);
        choppedVeg.transform.localPosition = Vector3.zero;
        choppedVeg.transform.localRotation = Quaternion.identity;
        currentVegetables.Add(choppedVeg);
        isVegetablesChopped = true;

        Debug.Log($"[CuttingBoard] Vegetables chopped! Type: {currentVegetableType}");
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
    public bool HasVegetables() => currentVegetables.Count > 0;
    public bool IsVegetablesChopped() => isVegetablesChopped;
    public VegetableType GetVegetableType() => currentVegetableType;
    public Transform GetVegetableSlot() => vegetableSlot;
    public int GetVegetableCount() => currentVegetables.Count;
}
