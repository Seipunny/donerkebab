using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Переносимый ящик с овощами. Наследует RaycastTarget и добавляет логику хранения овощей.
/// </summary>
[RequireComponent(typeof(RaycastTarget))]
public class VegetableBox : MonoBehaviour
{
    [Header("Box Settings")]
    [Tooltip("Тип овощей в ящике")]
    [SerializeField] private VegetableType vegetableType = VegetableType.Tomato;

    [Tooltip("Массив овощей в ящике (16 овощей)")]
    [SerializeField] private GameObject[] vegetables = new GameObject[16];

    private RaycastTarget raycastTarget;

    private void Awake()
    {
        raycastTarget = GetComponent<RaycastTarget>();
    }

    /// <summary>
    /// Переместить все овощи из ящика в контейнер
    /// </summary>
    public bool TransferVegetablesToContainer(VegetableContainer container)
    {
        Debug.Log($"[VegetableBox] TransferVegetablesToContainer called. Current count: {GetVegetableCount()}");

        if (IsEmpty())
        {
            Debug.Log("[VegetableBox] Box is already empty!");
            return false;
        }

        if (container.IsFull())
        {
            Debug.Log("[VegetableBox] Container is full!");
            return false;
        }

        // Проверяем совпадение типов овощей
        if (container.GetVegetableType() != vegetableType)
        {
            Debug.LogWarning($"[VegetableBox] Vegetable type mismatch! Box: {vegetableType}, Container: {container.GetVegetableType()}");
            return false;
        }

        // Собираем все овощи из ящика
        List<GameObject> vegetablesToTransfer = new List<GameObject>();
        for (int i = 0; i < vegetables.Length; i++)
        {
            if (vegetables[i] != null)
            {
                Debug.Log($"[VegetableBox] Collecting vegetable '{vegetables[i].name}' from slot {i}");
                vegetablesToTransfer.Add(vegetables[i]);
                vegetables[i] = null;
            }
        }

        Debug.Log($"[VegetableBox] Collected {vegetablesToTransfer.Count} vegetables to transfer");

        // Пытаемся добавить все овощи в контейнер
        bool success = container.AddVegetablesFromBox(vegetablesToTransfer.Count, vegetablesToTransfer);

        if (success)
        {
            Debug.Log($"[VegetableBox] Transferred {vegetablesToTransfer.Count} {vegetableType} vegetables to container. Box is now empty.");
            return true;
        }
        else
        {
            Debug.LogError("[VegetableBox] Failed to transfer vegetables to container!");
            return false;
        }
    }

    /// <summary>
    /// Уничтожить пустой ящик
    /// </summary>
    public void DisposeBox()
    {
        if (!IsEmpty())
        {
            Debug.LogWarning("[VegetableBox] Cannot dispose box with vegetables!");
            return;
        }

        Debug.Log($"[VegetableBox] Disposing empty box: {gameObject.name}");
        Destroy(gameObject);
    }

    // Public getters
    public bool IsEmpty()
    {
        foreach (var veg in vegetables)
        {
            if (veg != null) return false;
        }
        return true;
    }

    public int GetVegetableCount()
    {
        int count = 0;
        foreach (var veg in vegetables)
        {
            if (veg != null) count++;
        }
        return count;
    }

    public VegetableType GetVegetableType() => vegetableType;
}
