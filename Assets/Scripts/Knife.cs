using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Нож для нарезки овощей на доске.
/// Игрок должен зажать ЛКМ на ноже в течение 5 секунд, чтобы порезать овощ.
/// </summary>
public class Knife : MonoBehaviour, IRaycastTarget
{
    [Header("Outline Settings")]
    [SerializeField] private Outline outlineComponent;
    [SerializeField] private float outlineWidthOnLook = 10f;
    [SerializeField] private float outlineWidthDefault = 0f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Knife Settings")]
    [Tooltip("Связанная доска для нарезки")]
    [SerializeField] private CuttingBoard cuttingBoard;

    [Tooltip("Время зажатия ЛКМ для нарезки (секунды)")]
    [SerializeField] private float choppingTime = 5f;

    [Header("Chopped Prefabs")]
    [Tooltip("Префаб нарезанных помидоров")]
    [SerializeField] private GameObject choppedTomatoPrefab;

    [Tooltip("Префаб нарезанной капусты")]
    [SerializeField] private GameObject choppedCabbagePrefab;

    [Tooltip("Префаб нарезанных огурцов")]
    [SerializeField] private GameObject choppedCucumberPrefab;

    [Header("UI")]
    [Tooltip("Прогресс бар нарезки")]
    [SerializeField] private Image choppingProgressBar;

    // Outline state
    private float currentOutlineWidth;
    private float targetOutlineWidth;

    // Chopping state
    private float choppingProgress;
    private bool isChopping;

    private void Awake()
    {
        // Проверяем Outline
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
            {
                Debug.LogWarning($"[Knife] Outline component not found on {gameObject.name}");
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

        // Инициализируем UI
        if (choppingProgressBar != null)
        {
            choppingProgressBar.fillAmount = 0f;
            choppingProgressBar.gameObject.SetActive(false);
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
    /// Начать процесс нарезки
    /// </summary>
    public void StartChopping()
    {
        if (cuttingBoard == null)
        {
            Debug.LogWarning("[Knife] Cutting board not assigned!");
            return;
        }

        if (!cuttingBoard.HasVegetables())
        {
            Debug.LogWarning("[Knife] No vegetables on cutting board!");
            return;
        }

        if (cuttingBoard.IsVegetablesChopped())
        {
            Debug.LogWarning("[Knife] Vegetables already chopped!");
            return;
        }

        isChopping = true;
        choppingProgress = 0f;

        if (choppingProgressBar != null)
        {
            choppingProgressBar.gameObject.SetActive(true);
            choppingProgressBar.fillAmount = 0f;
        }

        Debug.Log("[Knife] Started chopping");
    }

    /// <summary>
    /// Обновить прогресс нарезки
    /// </summary>
    public void UpdateChopping(float deltaTime)
    {
        if (!isChopping) return;

        choppingProgress += deltaTime;

        if (choppingProgressBar != null)
        {
            choppingProgressBar.fillAmount = choppingProgress / choppingTime;
        }

        // Если нарезка завершена
        if (choppingProgress >= choppingTime)
        {
            CompleteChopping();
        }
    }

    /// <summary>
    /// Завершить нарезку
    /// </summary>
    private void CompleteChopping()
    {
        if (cuttingBoard == null || !cuttingBoard.HasVegetables())
        {
            CancelChopping();
            return;
        }

        // Определяем нужный префаб нарезанного овоща
        GameObject choppedPrefab = null;
        VegetableType vegType = cuttingBoard.GetVegetableType();

        switch (vegType)
        {
            case VegetableType.Tomato:
                choppedPrefab = choppedTomatoPrefab;
                break;
            case VegetableType.Cabbage:
                choppedPrefab = choppedCabbagePrefab;
                break;
            case VegetableType.Cucumber:
                choppedPrefab = choppedCucumberPrefab;
                break;
        }

        if (choppedPrefab == null)
        {
            Debug.LogError($"[Knife] Chopped prefab for {vegType} not assigned!");
            CancelChopping();
            return;
        }

        // Режем овощи
        cuttingBoard.ChopVegetables(choppedPrefab);

        isChopping = false;
        choppingProgress = 0f;

        if (choppingProgressBar != null)
        {
            choppingProgressBar.fillAmount = 0f;
            choppingProgressBar.gameObject.SetActive(false);
        }

        Debug.Log($"[Knife] Chopping complete! {vegType} is now chopped");
    }

    /// <summary>
    /// Отменить нарезку
    /// </summary>
    public void CancelChopping()
    {
        isChopping = false;
        choppingProgress = 0f;

        if (choppingProgressBar != null)
        {
            choppingProgressBar.fillAmount = 0f;
            choppingProgressBar.gameObject.SetActive(false);
        }

        Debug.Log("[Knife] Chopping cancelled");
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
        return false; // Нож нельзя поднять
    }

    public void OnPickup(Transform handTransform)
    {
        // Нож нельзя поднять
    }

    public void OnDrop()
    {
        // Нож нельзя поднять
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        // Нож нельзя переместить
    }

    public void ShowPlacementPreview(Vector3 position, Quaternion rotation)
    {
        // Нож нельзя переместить, превью не нужно
    }

    public void HidePlacementPreview()
    {
        // Нож нельзя переместить, превью не нужно
    }

    // Public getters
    public bool IsChopping() => isChopping;
    public float GetChoppingProgress() => choppingProgress / choppingTime;
}
