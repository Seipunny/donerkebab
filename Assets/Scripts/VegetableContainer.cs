using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Контейнер для овощей с индивидуальными слотами для каждого овоща.
/// Вместимость: 16 овощей (4 уровня по 4 овоща).
/// </summary>
public class VegetableContainer : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [SerializeField] private Outline outlineComponent;
    [SerializeField] private float outlineWidthOnLook = 10f;
    [SerializeField] private float outlineWidthDefault = 0f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Container Settings")]
    [Tooltip("Тип овощей в контейнере")]
    [SerializeField] private VegetableType vegetableType = VegetableType.Tomato;

    [Tooltip("Максимальная вместимость контейнера")]
    [SerializeField] private int maxCapacity = 16;

    [Tooltip("Количество овощей за один раз (в руки или из ящика)")]
    [SerializeField] private int transferAmount = 4;

    [Header("Vegetable Slots")]
    [Tooltip("Массив позиций для овощей (16 слотов)")]
    [SerializeField] private Transform[] vegetableSlots = new Transform[16];

    [Tooltip("Массив GameObject'ов овощей (заполняется автоматически или вручную)")]
    [SerializeField] private GameObject[] vegetables = new GameObject[16];

    [Header("UI")]
    [Tooltip("Прогресс бар наполнения (Filled Image)")]
    [SerializeField] private Image fillAmountProgressBar;

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
                Debug.LogWarning($"[VegetableContainer] Outline component not found on {gameObject.name}");
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

        // Инициализируем овощи
        InitializeVegetables();
        UpdateProgressBar();
    }

    private void InitializeVegetables()
    {
        // Деактивируем все овощи, которых нет в слотах
        for (int i = 0; i < vegetables.Length; i++)
        {
            if (vegetables[i] != null)
            {
                vegetables[i].SetActive(true);
            }
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
    /// Добавить овощи из ящика в контейнер
    /// </summary>
    public bool AddVegetablesFromBox(int amount, List<GameObject> vegetablesToAdd)
    {
        int currentCount = GetCurrentVegetableCount();

        Debug.Log($"[VegetableContainer] AddVegetablesFromBox called. Amount: {amount}, List count: {vegetablesToAdd.Count}, Current count: {currentCount}");

        if (currentCount >= maxCapacity)
        {
            Debug.Log("[VegetableContainer] Container is full!");
            return false;
        }

        if (vegetablesToAdd == null || vegetablesToAdd.Count == 0)
        {
            Debug.LogWarning("[VegetableContainer] No vegetables to add!");
            return false;
        }

        int amountToAdd = Mathf.Min(amount, maxCapacity - currentCount, vegetablesToAdd.Count);

        // Добавляем овощи в первые свободные слоты
        int addedCount = 0;
        for (int i = 0; i < vegetables.Length && addedCount < amountToAdd; i++)
        {
            if (vegetables[i] == null && addedCount < vegetablesToAdd.Count)
            {
                GameObject vegetableToAdd = vegetablesToAdd[addedCount];

                if (vegetableToAdd == null)
                {
                    Debug.LogWarning($"[VegetableContainer] Vegetable at index {addedCount} is null!");
                    addedCount++;
                    continue;
                }

                vegetables[i] = vegetableToAdd;
                vegetables[i].SetActive(true);

                // Перемещаем овощ в слот контейнера
                if (vegetableSlots[i] != null)
                {
                    vegetables[i].transform.SetParent(vegetableSlots[i]);
                    vegetables[i].transform.localPosition = Vector3.zero;
                    vegetables[i].transform.localRotation = Quaternion.identity;
                    vegetables[i].transform.localScale = Vector3.one;

                    Debug.Log($"[VegetableContainer] Added vegetable '{vegetables[i].name}' to slot {i}");
                }
                else
                {
                    Debug.LogWarning($"[VegetableContainer] Slot {i} is null! Vegetable added to array but not positioned.");
                }

                addedCount++;
            }
        }

        UpdateProgressBar();

        Debug.Log($"[VegetableContainer] Added {addedCount} vegetables. Current: {GetCurrentVegetableCount()}/{maxCapacity}");
        return addedCount > 0;
    }

    /// <summary>
    /// Забрать последние N овощей из контейнера
    /// </summary>
    public List<GameObject> TakeVegetables(int amount)
    {
        List<GameObject> takenVegetables = new List<GameObject>();

        int currentCount = GetCurrentVegetableCount();

        Debug.Log($"[VegetableContainer] TakeVegetables called. Requested: {amount}, Current count: {currentCount}");

        if (currentCount <= 0)
        {
            Debug.Log("[VegetableContainer] Container is empty!");
            return takenVegetables;
        }

        int amountToTake = Mathf.Min(amount, currentCount);

        // Берем последние N овощей (с конца массива)
        for (int i = vegetables.Length - 1; i >= 0 && takenVegetables.Count < amountToTake; i--)
        {
            if (vegetables[i] != null)
            {
                Debug.Log($"[VegetableContainer] Taking vegetable '{vegetables[i].name}' from slot {i}");
                takenVegetables.Add(vegetables[i]);
                vegetables[i] = null;
            }
        }

        UpdateProgressBar();

        Debug.Log($"[VegetableContainer] Took {takenVegetables.Count} vegetables. Remaining: {GetCurrentVegetableCount()}/{maxCapacity}");
        return takenVegetables;
    }

    /// <summary>
    /// Вернуть овощи обратно в контейнер на их места
    /// </summary>
    public bool ReturnVegetables(List<GameObject> vegetablesToReturn)
    {
        if (vegetablesToReturn == null || vegetablesToReturn.Count == 0)
            return false;

        int returnedCount = 0;

        // Находим пустые слоты и возвращаем овощи
        for (int i = vegetables.Length - 1; i >= 0 && returnedCount < vegetablesToReturn.Count; i--)
        {
            if (vegetables[i] == null)
            {
                vegetables[i] = vegetablesToReturn[returnedCount];
                vegetables[i].SetActive(true);

                // Возвращаем овощ в слот
                if (vegetableSlots[i] != null)
                {
                    vegetables[i].transform.SetParent(vegetableSlots[i]);
                    vegetables[i].transform.localPosition = Vector3.zero;
                    vegetables[i].transform.localRotation = Quaternion.identity;
                }

                returnedCount++;
            }
        }

        UpdateProgressBar();

        Debug.Log($"[VegetableContainer] Returned {returnedCount} vegetables. Current: {GetCurrentVegetableCount()}/{maxCapacity}");
        return returnedCount > 0;
    }

    private void UpdateProgressBar()
    {
        if (fillAmountProgressBar != null)
        {
            float fillPercentage = (float)GetCurrentVegetableCount() / maxCapacity;
            fillAmountProgressBar.fillAmount = fillPercentage;
        }
    }

    private int GetCurrentVegetableCount()
    {
        int count = 0;
        foreach (var veg in vegetables)
        {
            if (veg != null) count++;
        }
        return count;
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
        // Контейнеры не поднимаются
    }

    public void OnDrop()
    {
        // Контейнеры не поднимаются
    }

    public bool CanBePickedUp()
    {
        return false; // Контейнер нельзя поднять
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        // Контейнеры не размещаются
    }

    public void HidePlacementPreview()
    {
        // Контейнеры не размещаются
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Контейнеры не размещаются
    }

    // Public getters
    public int GetCurrentAmount() => GetCurrentVegetableCount();
    public int GetMaxCapacity() => maxCapacity;
    public bool IsEmpty() => GetCurrentVegetableCount() <= 0;
    public bool IsFull() => GetCurrentVegetableCount() >= maxCapacity;
    public int GetTransferAmount() => transferAmount;
    public VegetableType GetVegetableType() => vegetableType;
    public Transform[] GetVegetableSlots() => vegetableSlots;
}
