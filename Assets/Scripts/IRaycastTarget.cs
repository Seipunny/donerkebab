using UnityEngine;

public interface IRaycastTarget
{
    void OnRaycastEnter();
    void OnRaycastStay();
    void OnRaycastExit();

    /// <summary>
    /// Вызывается когда объект берется в руки
    /// </summary>
    void OnPickup(Transform handTransform);

    /// <summary>
    /// Вызывается когда объект отпускается
    /// </summary>
    void OnDrop();

    /// <summary>
    /// Может ли объект быть поднят
    /// </summary>
    bool CanBePickedUp();

    /// <summary>
    /// Показать превью размещения в указанной позиции и ротации
    /// </summary>
    void ShowPlacementPreview(Vector3 position, Quaternion rotation);

    /// <summary>
    /// Скрыть превью размещения
    /// </summary>
    void HidePlacementPreview();

    /// <summary>
    /// Разместить объект в указанной позиции (завершить размещение)
    /// </summary>
    void PlaceAt(Vector3 position, Quaternion rotation);
}
