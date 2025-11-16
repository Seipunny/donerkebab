using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент для порции овощей в руках игрока.
/// Хранит ссылки на GameObject'ы овощей и анимирует их движение к руке.
/// </summary>
public class VegetableHandPortion : MonoBehaviour
{
    [Header("Portion Settings")]
    [Tooltip("Тип овощей в порции")]
    [SerializeField] private VegetableType vegetableType = VegetableType.Tomato;

    [Header("Animation")]
    [Tooltip("Скорость анимации перемещения в руки")]
    [SerializeField] private float moveToHandSpeed = 5f;

    // Private state
    private List<GameObject> vegetables = new List<GameObject>();
    private List<Vector3> vegetableVelocities = new List<Vector3>();
    private bool isMovingToHand;
    private Transform targetHandTransform;

    public void Initialize(VegetableType type, List<GameObject> veggies)
    {
        vegetableType = type;
        vegetables = new List<GameObject>(veggies);

        Debug.Log($"[VegetableHandPortion] Initializing with {veggies.Count} {type} vegetables");

        // Инициализируем velocity для каждого овоща
        for (int i = 0; i < vegetables.Count; i++)
        {
            vegetableVelocities.Add(Vector3.zero);

            // Делаем овощи детьми этого объекта
            if (vegetables[i] != null)
            {
                Debug.Log($"[VegetableHandPortion] Setting up vegetable '{vegetables[i].name}'");
                vegetables[i].transform.SetParent(transform);
                vegetables[i].SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[VegetableHandPortion] Vegetable at index {i} is null!");
            }
        }

        Debug.Log($"[VegetableHandPortion] Initialized with {vegetables.Count} {vegetableType} vegetables");
    }

    public void StartMovementToHand(Transform handTransform)
    {
        if (handTransform == null)
        {
            Debug.LogWarning("[VegetableHandPortion] Hand transform is null!");
            return;
        }

        targetHandTransform = handTransform;
        isMovingToHand = true;

        Debug.Log("[VegetableHandPortion] Starting movement to hand");
    }

    private void Update()
    {
        if (isMovingToHand && targetHandTransform != null)
        {
            ProcessMovementToHand();
        }
    }

    private void ProcessMovementToHand()
    {
        bool allReached = true;

        // Анимируем каждый овощ отдельно
        for (int i = 0; i < vegetables.Count; i++)
        {
            if (vegetables[i] == null) continue;

            // Получаем текущую velocity
            Vector3 currentVelocity = vegetableVelocities[i];

            // Плавно перемещаемся к руке
            vegetables[i].transform.position = Vector3.SmoothDamp(
                vegetables[i].transform.position,
                targetHandTransform.position,
                ref currentVelocity,
                1f / moveToHandSpeed
            );

            // Сохраняем обновленную velocity
            vegetableVelocities[i] = currentVelocity;

            vegetables[i].transform.rotation = Quaternion.Slerp(
                vegetables[i].transform.rotation,
                targetHandTransform.rotation,
                Time.deltaTime * moveToHandSpeed
            );

            // Проверяем достигли ли цели
            float distanceToTarget = Vector3.Distance(vegetables[i].transform.position, targetHandTransform.position);
            if (distanceToTarget >= 0.05f)
            {
                allReached = false;
            }
        }

        // Если все овощи достигли руки
        if (allReached)
        {
            // Привязываем к руке
            transform.SetParent(targetHandTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Сбрасываем локальные позиции овощей
            foreach (var veg in vegetables)
            {
                if (veg != null)
                {
                    veg.transform.localPosition = Vector3.zero;
                    veg.transform.localRotation = Quaternion.identity;
                }
            }

            isMovingToHand = false;
            Debug.Log("[VegetableHandPortion] All vegetables reached hand");
        }
    }

    // Public getters
    public VegetableType GetVegetableType() => vegetableType;
    public int GetAmount() => vegetables.Count;
    public bool IsMoving() => isMovingToHand;
    public List<GameObject> GetVegetables() => vegetables;
}
